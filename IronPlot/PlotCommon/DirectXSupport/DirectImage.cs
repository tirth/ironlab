// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using SharpDX.Direct2D1;
using SharpDX.Direct3D9;
using ResultCode = SharpDX.Direct3D9.ResultCode;

namespace IronPlot
{
    public enum SurfaceType { DirectX9, Direct2D };

    public enum ResizeResetResult { Ok, D3D9DeviceLost, D3D9ResetFailed };
    
    /// <summary>
    /// Class uses SharpDX to render onto an ImageBrush using D3DImage,
    /// and optionally onto a Canvas that can be overlaid for 2D vector annotation.
    /// Base contains just basic rendering capability.
    /// Rendering is either Direct3D9 for 3D or Direct2D for 2D, controlled by surfaceType field.
    /// </summary>
    public class DirectImage : DependencyObject
    {
        public static readonly DependencyProperty VisibleProperty =
            DependencyProperty.Register("Visible",
            typeof(bool), typeof(DirectImage),
            new FrameworkPropertyMetadata(true, OnVisiblePropertyChanged));

        internal bool Visible
        {
            set { SetValue(VisibleProperty, value); }
            get { return (bool)GetValue(VisibleProperty); }
        }

        public event EventHandler RenderRequested;

        protected SurfaceType SurfaceType;
        //Texture SharedTexture;

        // All  GraphicsDevices, managed by this helper service.
        protected SharpDxGraphicsDeviceService9 GraphicsDeviceService9;
        // And this one for Direct3D10 (needed for Direct2D)
        protected SharpDxGraphicsDeviceService10 GraphicsDeviceService10;
        //DeviceEx graphicsDeviceTemp;
        // The D3DImage...
        internal D3DImage D3DImage;
        // and derived ImageBrush
        protected ImageBrush imageBrush;
        // Canvas for optional overlaying of vector graphics
        Canvas _canvas;

        bool _afterResizeReset;

        private Surface _backBufferSurface;

        // The width and height of the backBuffer or Textures
        protected int bufferWidth;
        public int BufferWidth => bufferWidth;
        protected int bufferHeight;
        public int BufferHeight => bufferWidth;

        // The width and height of the Viewport
        protected int viewportWidth;
        public int ViewportWidth => viewportWidth;
        protected int viewportHeight;
        public int ViewportHeight => viewportHeight;

        // The width and height of the image onto which the viewport is mapped
        internal int ImageWidth, ImageHeight;
        double _pixelsPerDiPixel = 1; 
        // and the dpi
        // On render, viewportWidth = pixelsPerDIPixel * imageWidth

        public DirectImage()
        {
            bufferWidth = 10; bufferHeight = 10;
            ImageWidth = 10; ImageHeight = 10;
            viewportWidth = bufferWidth; viewportHeight = bufferHeight;
            RecreateD3DImage(false);
        }

        public void RecreateD3DImage(bool setBackBuffer)
        {
            if (D3DImage != null) D3DImage.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
            D3DImage = new D3DImage();
            imageBrush = new ImageBrush(D3DImage);
            D3DImage.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;
            if (setBackBuffer) SetBackBuffer();
        }

        protected virtual void OnVisiblePropertyChanged(bool isVisible)
        {
        }

        protected static void OnVisiblePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)(e.NewValue)) ((DirectImage)obj).OnVisiblePropertyChanged(true); else ((DirectImage)obj).OnVisiblePropertyChanged(false);
        }

        // Create Device(s) and start rendering.
        protected virtual void CreateDevice(SurfaceType surfaceType)
        {
            SurfaceType = surfaceType;
            if (surfaceType == SurfaceType.Direct2D)
            {
                // Use shared devices resources.
                GraphicsDeviceService10 = SharpDxGraphicsDeviceService10.AddRef();
                GraphicsDeviceService10.DeviceResized += graphicsDeviceService10_DeviceResized;
                GraphicsDeviceService10.ResizeDevice(bufferWidth, bufferHeight);
                bufferWidth = GraphicsDeviceService10.Width;
                bufferHeight = GraphicsDeviceService10.Height;
            }
            else if (surfaceType == SurfaceType.DirectX9)
            {
                // Use shared devices resources.
                GraphicsDeviceService9 = SharpDxGraphicsDeviceService9.AddRef(bufferWidth, bufferHeight);
                GraphicsDeviceService9.DeviceReset += graphicsDeviceService9_DeviceReset;
                _afterResizeReset = true;
            }
            Initialize();
            if (D3DImage.IsFrontBufferAvailable)
            {
                SetBackBuffer();
            }
            //CompositionTarget.Rendering += OnRendering;
        }

        internal void SetBackBuffer()
        {
            if (SurfaceType == SurfaceType.DirectX9)
            {
                D3DImage.Lock();
                using (_backBufferSurface = GraphicsDevice.GetBackBuffer(0, 0))
                {
                    D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, _backBufferSurface.NativePointer);
                }
                D3DImage.Unlock();
            }
            else if (SurfaceType == SurfaceType.Direct2D) SetBackBuffer(GraphicsDeviceService10.Texture9); 
        }

        public void SetBackBuffer(Texture texture)
        {
            if (texture == null) ReleaseBackBuffer();
            else
            {
                using (var surface = texture.GetSurfaceLevel(0))
                {
                    D3DImage.Lock();
                    D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
                    D3DImage.Unlock();
                }
            }
        }

        protected void ReleaseBackBuffer()
        {
            D3DImage.Lock();
            D3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
            D3DImage.Unlock();
        }

        /// <summary>
        /// Gets a GraphicsDevice that can be used to draw into the 3D space.
        /// </summary>
        public Device GraphicsDevice => GraphicsDeviceService9.GraphicsDevice;

        /// <summary>
        /// Gets a 2D RenderTarget.
        /// </summary>
        public RenderTarget RenderTarget => GraphicsDeviceService10.RenderTarget;

        /// <summary>
        /// Gets a GraphicsDeviceService.
        /// </summary>
        public SharpDxGraphicsDeviceService9 GraphicsDeviceService => GraphicsDeviceService9;

        public int Width => viewportWidth;

        public int Height => viewportHeight;

        /// <summary>
        /// Gets or sets the Canvas for vector overlays
        /// </summary>
        public Canvas Canvas
        {
            get { return _canvas; }
            set { _canvas = value; }
        }

        /// <summary>
        /// Gets an ImageBrush of the Viewport 
        /// </summary>
        public ImageBrush ImageBrush => imageBrush;

        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // If the front buffer becomes unavailable, the D3DImage is discarded. This is for robustness since in
            // some situations the front buffer seems never to become available again.
            if (D3DImage.IsFrontBufferAvailable)
            {
                ReleaseBackBuffer();
                SetBackBuffer();
                RenderScene();
                if (SurfaceType == SurfaceType.DirectX9) ResetDevice();
            }
            else
            {
                imageBrush = null;
                D3DImage.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
                D3DImage = null;
                if (SurfaceType == SurfaceType.DirectX9 && GraphicsDeviceService9.UseDeviceEx == false)
                {
                    if (GraphicsDeviceService9.GraphicsDevice.TestCooperativeLevel() == ResultCode.DeviceNotReset)
                    {
                        ResetDevice();
                    }
                    ReleaseBackBuffer();
                }
            }
        }

        /// <summary>
        /// Disposes the control.
        /// </summary>
        protected void Dispose(bool disposing)
        {
            if (GraphicsDeviceService9 != null)
            {
                GraphicsDeviceService9.Release(disposing);
                GraphicsDeviceService9 = null;
            }
            if (GraphicsDeviceService10 != null)
            {
                GraphicsDeviceService10.Release(disposing);
                GraphicsDeviceService10 = null;
            }
        }

        object _locker = new object();

        public void RequestRender()
        {
            if (RenderRequested != null) RenderRequested(this, EventArgs.Empty);
        }

        public void RequestRender(object sender, EventArgs e)
        {
            if (RenderRequested != null) RenderRequested(sender, e);
        }

        public void RenderScene()
        {
            if (D3DImage == null) return;
            D3DImage.Lock();
            if (BeginDraw())
            {
                // Draw is overriden in derived classes
                Draw();
                EndDraw();
            }
            if (D3DImage.IsFrontBufferAvailable)
            {
                D3DImage.AddDirtyRect(new Int32Rect(0, 0, viewportWidth, viewportHeight));
                D3DImage.Unlock();
            }
        }

        /// <summary>
        /// Attempts to begin drawing the control. Returns false if this was not possible.
        /// </summary>
        private bool BeginDraw()
        {
            // Make sure the graphics device is big enough, and is not lost.
            if (HandleDeviceResetAndSizeChanged() != ResizeResetResult.Ok) return false;

            // Multiple DirectImage instances can share the same device. The viewport is adjusted
            // for when a smaller DirectImage is rendering.
            if (SurfaceType == SurfaceType.DirectX9)
            {
                var viewport = new Viewport();
                viewport.X = 0;
                viewport.Y = 0;
                viewport.Width = viewportWidth;
                viewport.Height = viewportHeight;
                viewport.MinZ = 0;
                viewport.MaxZ = 1;
                GraphicsDevice.Viewport = viewport;
            }

            if (SurfaceType == SurfaceType.Direct2D)
            {
                var viewport2D = new SharpDX.Direct3D10.Viewport();
                viewport2D.Height = viewportHeight;
                viewport2D.MaxDepth = 1;
                viewport2D.MinDepth = 0;
                viewport2D.TopLeftX = 0;
                viewport2D.TopLeftY = 0;
                viewport2D.Width = viewportWidth;
                GraphicsDeviceService10.SetViewport(viewport2D);
            }
            return true;
        }

        /// <summary>
        /// Ends drawing the control. This is called after derived classes
        /// have finished their Draw method.
        /// </summary>
        private void EndDraw()
        {
            if (SurfaceType == SurfaceType.DirectX9) 
            {
                try
                {
                    GraphicsDeviceService9.GraphicsDevice.Present();
                }
                catch
                { 
                    // Present might throw if the device became lost while we were
                    // drawing. The lost device will be handled by the next BeginDraw,
                    // so we just swallow the exception.
                }
            }
        }

        /// <summary>
        /// Helper used by BeginDraw. This checks the graphics device status,
        /// making sure it is big enough for drawing the current control, and
        /// that the device is not lost.
        /// </summary>
        private ResizeResetResult HandleDeviceResetAndSizeChanged()
        {
            viewportWidth = (int)(Math.Max(ImageWidth * 1, 1) * _pixelsPerDiPixel);
            viewportHeight = (int)(Math.Max(ImageHeight * 1, 1) * _pixelsPerDiPixel);
            if (SurfaceType == SurfaceType.DirectX9)
            {
                if (GraphicsDevice.TestCooperativeLevel() == ResultCode.DeviceLost) return ResizeResetResult.D3D9DeviceLost;
                var reset = false;
                try
                {
                    reset = GraphicsDeviceService9.ResetIfNecessary();
                    bufferWidth = GraphicsDeviceService9.PresentParameters.BackBufferWidth;
                    bufferHeight = GraphicsDeviceService9.PresentParameters.BackBufferHeight;
                }
                catch
                {
                    return ResizeResetResult.D3D9ResetFailed;
                }
                if (_afterResizeReset || reset)
                {
                    AfterResizeReset();
                    _afterResizeReset = false;
                }
            }
            else if (SurfaceType == SurfaceType.Direct2D)
            {
                if ((viewportWidth > bufferWidth) || (viewportHeight > bufferHeight))
                {
                    _requestedWidth = (int)(viewportWidth * 1.1);
                    _requestedHeight = (int)(viewportHeight * 1.1);
                    GraphicsDeviceService10.ResizeDevice(_requestedWidth, _requestedHeight);
                    AfterResizeReset();
                }
                else if (_afterResizeReset) AfterResizeReset();
            }
            return ResizeResetResult.Ok;
        }

        int _requestedWidth, _requestedHeight;

        protected virtual void ResetDevice()
        {
            //if (viewportWidth > bufferWidth) bufferWidth = (int)(viewportWidth * 1.1);
            //if (viewportHeight > bufferHeight) bufferHeight = (int)(viewportHeight * 1.1);
            //graphicsDeviceService9.RatchetResetDevice(bufferWidth,
            //              bufferHeight);
            //graphicsDeviceService9.ResetIfNecessary();
        }

        protected void graphicsDeviceService9_DeviceReset(object sender, EventArgs e)
        {
            _afterResizeReset = true;
        }

        void graphicsDeviceService10_DeviceResized(object sender, EventArgs e)
        {
            _afterResizeReset = true;
        }

        private void AfterResizeReset()
        {
            if (SurfaceType == SurfaceType.DirectX9)
            {
                bufferWidth = GraphicsDeviceService9.PresentParameters.BackBufferWidth;
                bufferHeight = GraphicsDeviceService9.PresentParameters.BackBufferHeight;
                SetBackBuffer();
                imageBrush.Viewbox = new Rect(0, 0, viewportWidth / (double)bufferWidth, viewportHeight / (double)bufferHeight);
            }
            else
            {
                bufferWidth = GraphicsDeviceService10.Width;
                bufferHeight = GraphicsDeviceService10.Height;
                SetBackBuffer(GraphicsDeviceService10.Texture9);
                UpdateImageBrush();
            }
        }

        public void OnSizeChanged(Object sender, SizeChangedEventArgs e)
        {
            SetImageSize((int)e.NewSize.Width, (int)e.NewSize.Height, 96);
        }

        internal void SetImageSize(int width, int height, int dpi)
        {
            ImageWidth = width;
            ImageHeight = height;
            _pixelsPerDiPixel = dpi / 96.0;
            UpdateImageBrush();
        }

        private void UpdateImageBrush()
        {
            imageBrush.Viewbox = new Rect(0, 0, ImageWidth * _pixelsPerDiPixel / bufferWidth, ImageHeight * _pixelsPerDiPixel / bufferHeight);
            imageBrush.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
            imageBrush.TileMode = TileMode.None;
            imageBrush.Stretch = Stretch.Fill;
            imageBrush.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
        }

        protected virtual void Initialize()
        {
            // Do nothing in base 
        }
    
        /// <summary>
        /// Derived classes override this to draw themselves using the GraphicsDevice.
        /// </summary>
        protected virtual void Draw()
        {
            // Do nothing in base
        }

        internal void RegisterWithService()
        {
            if (SurfaceType == SurfaceType.Direct2D)
                GraphicsDeviceService10.Tracker.Register(this);
            else 
                GraphicsDeviceService9.Tracker.Register(this);
        }

        internal void UnregisterWithService()
        {
            if (SurfaceType == SurfaceType.Direct2D)
                GraphicsDeviceService10.Tracker.Unregister(this);
            else 
                GraphicsDeviceService9.Tracker.Unregister(this);
        }

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();
    }
}
