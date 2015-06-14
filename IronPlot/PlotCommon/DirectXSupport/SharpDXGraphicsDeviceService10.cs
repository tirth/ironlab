// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Threading;
using SharpDX.Direct2D1;
using SharpDX.Direct3D10;
using SharpDX.Direct3D9;
using SharpDX.DXGI;
using Device1 = SharpDX.Direct3D10.Device1;
using Factory = SharpDX.DXGI.Factory;
using FeatureLevel = SharpDX.Direct3D10.FeatureLevel;
using Format = SharpDX.Direct3D9.Format;
using Resource = SharpDX.DXGI.Resource;
using Surface = SharpDX.DXGI.Surface;
using Usage = SharpDX.Direct3D9.Usage;
using Viewport = SharpDX.Direct3D10.Viewport;

namespace IronPlot
{
    public class SharpDxGraphicsDeviceService10
    {
        // Singleton device service instance.
        static SharpDxGraphicsDeviceService10 _singletonInstance;
        // Keep track of how many controls are sharing the singletonInstance.
        static int _referenceCount;

        internal DirectImageTracker Tracker = new DirectImageTracker();

        // When the (possibly) shared instance is resized.
        public event EventHandler DeviceResized;

        // A texture not shareable between D3D9 and D3D10, but which does support multi-sample 
        // anti-aliasing etc
        Texture2D _texture;
        // The shareable texture:
        Texture2D _shareableTexture;
        
        // For linking back to WPF, we need the device and the D3D9 shareable texture:
        readonly SharpDxGraphicsDeviceService9 _sharpDxGraphicsDeviceService9;
        Texture _shareableTexture9;

        Factory _factoryDxgi;
        SharpDX.Direct2D1.Factory _factory2D;
        RenderTarget _renderTarget;
        
        int _width;
        int _height;

        public Texture2D Texture => _shareableTexture;
        public Texture Texture9 => _shareableTexture9;

        public RenderTarget RenderTarget => _renderTarget;

        public Device1 Device => _device;

        public int Width => _width;

        public int Height => _height;

        //SharpDX.Direct3D10.Device device;
        readonly Device1 _device;

        /// <summary>
        /// Gets a reference to the singleton instance.
        /// </summary>
        public static SharpDxGraphicsDeviceService10 AddRef()
        {
            // Increment the "how many controls sharing the device" reference count.
            if (Interlocked.Increment(ref _referenceCount) == 1)
            {
                // If this is the first control to start using the
                // device, we must create the singleton instance.
                _singletonInstance = new SharpDxGraphicsDeviceService10();
            }
            return _singletonInstance;
        }

        /// <summary>
        /// Create a new GraphicsDeviceService and return a reference to it.
        /// </summary>
        public static SharpDxGraphicsDeviceService10 RefToNew()
        {
            return new SharpDxGraphicsDeviceService10();
        }

        /// <summary>
        /// Constructor is private, because this is a singleton class:
        /// client controls should use the public AddRef method instead.
        /// </summary>
        SharpDxGraphicsDeviceService10()
        {
            // We need a a D3D9 device.
            _sharpDxGraphicsDeviceService9 = SharpDxGraphicsDeviceService9.RefToNew(0, 0);
            
            _factoryDxgi = new Factory();
            _factory2D = new SharpDX.Direct2D1.Factory();
            // Try to create a hardware device first and fall back to a
            // software (WARP doesn't let us share resources)
            var device1 = TryCreateDevice1(DriverType.Hardware);
            if (device1 == null)
            {
                device1 = TryCreateDevice1(DriverType.Software);
                if (device1 == null)
                {
                    throw new Exception("Unable to create a DirectX 10 device.");
                }
            }
            // Ratserizer not needed for Direct2D (retain for if mixing D2D and D3D).
            //RasterizerStateDescription rastDesc = new RasterizerStateDescription();
            //rastDesc.CullMode = CullMode.Back;
            //rastDesc.FillMode = FillMode.Solid;
            //rastDesc.IsMultisampleEnabled = false;
            //rastDesc.IsAntialiasedLineEnabled = false;
            //device1.Rasterizer.State = new RasterizerState(device1, rastDesc);
            _device = device1;
        }

        private Device1 TryCreateDevice1(DriverType type)
        {
            // We'll try to create the device that supports any of these feature levels
            FeatureLevel[] levels =
            {
                FeatureLevel.Level_10_1,
                FeatureLevel.Level_10_0,
                FeatureLevel.Level_9_3,
                FeatureLevel.Level_9_2,
                FeatureLevel.Level_9_1
            };

            foreach (var level in levels)
            {
                try
                {
                    //var device = new SharpDX.Direct3D10.Device1(factoryDXGI.GetAdapter(0), DeviceCreationFlags.BgraSupport, level);
                    var device = new Device1(type, DeviceCreationFlags.BgraSupport, level);
                    return device;
                }
                catch (ArgumentException) // E_INVALIDARG
                {
                }
                catch (OutOfMemoryException) // E_OUTOFMEMORY
                {
                }
                catch (Exception) // SharpDX.Direct3D10.Direct3D10Exception D3DERR_INVALIDCALL or E_FAIL
                {
                }
            }
            return null; // We failed to create a device at any required feature level
        }

        public void ResizeDevice(int width, int height)
        {
            lock (this)
            {
                if (width < 0)
                {
                    throw new ArgumentOutOfRangeException("width", "Value must be positive.");
                }
                if (height < 0)
                {
                    throw new ArgumentOutOfRangeException("height", "Value must be positive.");
                }
                if ((width <= _width) && (height <= _height))
                {
                    return;
                }

                DirectXHelpers.SafeDispose(ref _texture);
                var texture = CreateTexture(Math.Max(width, _width), Math.Max(height, _height), true);
                _texture = texture;

                DirectXHelpers.SafeDispose(ref _shareableTexture);
                var shareableTexture = CreateTexture(Math.Max(width, _width), Math.Max(height, _height), false);
                _shareableTexture = shareableTexture;

                CreateD3D9TextureFromD3D10Texture(shareableTexture);

                _width = texture.Description.Width;
                _height = texture.Description.Height;

                using (var surface = texture.AsSurface())
                {
                    CreateRenderTarget(surface);
                }

                if (DeviceResized != null)
                    DeviceResized(this, EventArgs.Empty);
            }
        }

        public void SetViewport(Viewport viewport)
        {
            _device.Rasterizer.SetViewports(viewport);
        }

        private Texture2D CreateTexture(int width, int height, bool multiSampling)
        {
            var description = new Texture2DDescription();
            description.ArraySize = 1;
            description.BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource;
            description.CpuAccessFlags = CpuAccessFlags.None;
            description.Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm;
            description.MipLevels = 1;
 
            // Multi-sample anti-aliasing
            int count, quality;
            if (multiSampling) 
            {
                count = 8;
                quality = _device.CheckMultisampleQualityLevels(description.Format, count);
                if (quality == 0)
                {
                    count = 4;
                    quality = _device.CheckMultisampleQualityLevels(description.Format, count);
                }
                if (quality == 0) count = 1;
            }
            else count = 1;
            if (count == 1) quality = 1;
            var sampleDesc = new SampleDescription(count, 0);
            description.SampleDescription = sampleDesc;

            description.Usage = ResourceUsage.Default;
            description.OptionFlags = ResourceOptionFlags.Shared;
            description.Height = height;
            description.Width = width;

            return new Texture2D(_device, description);
        }

        /// <summary>
        /// Copy texture to shareable texture. 
        /// </summary>
        internal void CopyTextureAcross()
        {
            _device.ResolveSubresource(_texture, 0, _shareableTexture, 0, SharpDX.DXGI.Format.B8G8R8A8_UNorm);
        }

        private void CreateRenderTarget(Surface surface)
        {
            // Create a D2D render target which can draw into our offscreen D3D surface. 
            // D2D uses device independant units, like WPF, at 96/inch.
            var properties = new RenderTargetProperties();
            properties.DpiX = 96;
            properties.DpiY = 96;
            properties.MinLevel = SharpDX.Direct2D1.FeatureLevel.Level_DEFAULT;
            properties.PixelFormat = new PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Premultiplied);
            properties.Usage = RenderTargetUsage.None;

            if (_renderTarget != null)
            {
                _renderTarget.Dispose();
            }

            _renderTarget = new RenderTarget(_factory2D, surface, properties);
        }

        #region D3D9Sharing

        public void CreateD3D9TextureFromD3D10Texture(Texture2D Texture)
        {
            DirectXHelpers.SafeDispose(ref _shareableTexture9);

            if (IsShareable(Texture))
            {
                var format = TranslateFormat(Texture);
                if (format == Format.Unknown)
                    throw new ArgumentException("Texture format is not compatible with OpenSharedResource");

                var handle = GetSharedHandle(Texture);
                if (handle == IntPtr.Zero)
                    throw new ArgumentNullException("Handle");

                _shareableTexture9 = new Texture(_sharpDxGraphicsDeviceService9.GraphicsDevice, Texture.Description.Width, Texture.Description.Height, 1, Usage.RenderTarget, format, Pool.Default, ref handle);
            }
            else
                throw new ArgumentException("Texture must be created with ResourceOptionFlags.Shared");
        }

        IntPtr GetSharedHandle(Texture2D Texture)
        {
            var resource = Texture.QueryInterface<Resource>();
            var result = resource.SharedHandle;
            resource.Dispose();
            return result;
        }

        Format TranslateFormat(Texture2D Texture)
        {
            switch (Texture.Description.Format)
            {
                case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
                    return Format.A2B10G10R10;

                case SharpDX.DXGI.Format.R16G16B16A16_Float:
                    return Format.A16B16G16R16F;

                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                    return Format.A8R8G8B8;

                default:
                    return Format.Unknown;
            }
        }

        bool IsShareable(Texture2D Texture)
        {
            return (Texture.Description.OptionFlags & ResourceOptionFlags.Shared) != 0;
        }
        #endregion

        /// <summary>
        /// Releases a reference to the singleton instance.
        /// </summary>
        public void Release(bool disposing)
        {
            // Decrement the "how many controls sharing the device" reference count.
            if (Interlocked.Decrement(ref _referenceCount) == 0)
            {
                // If this is the last control to finish using the
                // device, we should dispose the singleton instance.
                DirectXHelpers.SafeDispose(ref _renderTarget);
                DirectXHelpers.SafeDispose(ref _texture);
                DirectXHelpers.SafeDispose(ref _shareableTexture);
                DirectXHelpers.SafeDispose(ref _factoryDxgi);
                DirectXHelpers.SafeDispose(ref _factory2D);
                DirectXHelpers.SafeDispose(ref _shareableTexture9);
            }
        }

    }
}
