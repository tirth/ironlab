// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using SharpDX;
using SharpDX.Direct3D9;

namespace IronPlot.Plotting3D
{
    /// <summary>
    /// Supplies 3D graphics device to draw into and Canvas (and associated transform methods) for
    /// vector overlay.
    /// </summary>
    /// <remarks>
    /// Unlikely that there will be an implementer of IViewportImage that is not a ViewportImage;
    /// mainly used to keep track of the methods that the children Model3D objects need.
    /// </remarks>
    public interface IViewportImage
    {
        I2DLayer Layer2D { get; }
        Device GraphicsDevice { get; }
        MatrixTransform3D ModelToWorld { get; set; }
    }
    
    /// <summary>
    /// Class uses SharpDX to render onto an ImageBrush using D3DImage,
    /// and optionally onto a Canvas that can be overlaid for 2D vector annotation.
    /// ViewportImage renders a collection of Model3D objects.
    /// </summary>
    public class ViewportImage : DirectImage, IViewportImage
    {
        #region Fields

        Matrix _world;
        Matrix _view;
        Matrix _projection;
        Matrix _cameraTransform;
        float _fov, _tanSemiFov;

        I2DLayer _layer2D;

        public Matrix World => _world;

        public Matrix View => _view;

        public Matrix Projection => _projection;

        public float Scale { get; internal set; }

        public float Fov
        {
            get { return _fov; }

            set
            {
                _fov = value;
                _tanSemiFov = (float)Math.Tan((double)_fov / 2);
            }
        }

         public float TanSemiFov => _tanSemiFov;

        public I2DLayer Layer2D => _layer2D;

        public void SetLayer2D(Canvas canvas, MatrixTransform3D modelToWorld) 
        {
            _layer2D = new SharpDxLayer2D(canvas, this, modelToWorld);
        }

        internal Viewport3D ViewPort3D { get; set; }

        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty ModelToWorldProperty =
            DependencyProperty.Register("ModelToWorld",
            typeof(MatrixTransform3D), typeof(ViewportImage),
            new PropertyMetadata((MatrixTransform3D)Transform3D.Identity, OnModelToWorldChanged));

        public static readonly DependencyProperty ModelsProperty =
            DependencyProperty.Register("Models",
            typeof(Model3DCollection), typeof(ViewportImage),
            new PropertyMetadata(null));

        public Model3DCollection Models
        {
            get { return (Model3DCollection)GetValue(ModelsProperty); }
            set { SetValue(ModelsProperty, value); }
        }

        public static readonly DependencyProperty CameraPositionProperty =
            DependencyProperty.Register("CameraPosition",
            typeof(Vector3), typeof(ViewportImage),
            new FrameworkPropertyMetadata(new Vector3(10f, 0, 0),
            OnCameraChanged));

        public static readonly DependencyProperty CameraTargetProperty =
            DependencyProperty.Register("CameraTarget",
            typeof(Vector3), typeof(ViewportImage),
            new FrameworkPropertyMetadata(Vector3.Zero,
            OnCameraChanged));

        public static readonly DependencyProperty CameraUpVectorProperty =
            DependencyProperty.Register("CameraUpVector",
            typeof(Vector3), typeof(ViewportImage),
            new FrameworkPropertyMetadata(new Vector3(0, 0, 1),
            OnCameraChanged));

         
        public MatrixTransform3D ModelToWorld
        {
            get { return (MatrixTransform3D)GetValue(ModelToWorldProperty); }
            set { SetValue(ModelToWorldProperty, value); }
        }

        public Vector3 CameraPosition
        {
            set { SetValue(CameraPositionProperty, value); }
            get { return (Vector3)GetValue(CameraPositionProperty); }
        }

        public Vector3 CameraTarget
        {
            set { SetValue(CameraTargetProperty, value); }
            get { return (Vector3)GetValue(CameraTargetProperty); }
        }

        public Vector3 CameraUpVector
        {
            set { SetValue(CameraUpVectorProperty, value); }
            get { return (Vector3)GetValue(CameraUpVectorProperty); }
        }

        protected static void OnCameraChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var viewportImage = ((ViewportImage)obj);
            viewportImage.RequestRender();
            viewportImage._view = Matrix.LookAtRH(viewportImage.CameraPosition, viewportImage.CameraTarget, viewportImage.CameraUpVector);
        }

        protected override void OnVisiblePropertyChanged(bool isVisible)
        {
            if (isVisible) foreach (var model in Models) model.RecursiveRecreateDisposables();
            else foreach (var model in Models) model.RecursiveDisposeDisposables();
        }

        protected static void OnModelToWorldChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((ViewportImage)obj).RequestRender();
        }

        #endregion

        public ViewportImage()
        {
            _layer2D = null;
            CreateDevice(SurfaceType.DirectX9);
            Fov = 0.75f;
            Scale = 1;
        }

        protected override void Initialize()
        {
            SetValue(ModelsProperty, new Model3DCollection(this));
            _cameraTransform = Matrix.Identity;
            _world = Matrix.Identity;
        }

        protected override void Draw()
        {
            // Ensure transforms are updated and clear; otherwise leave to Model3D tree
            GraphicsDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, new Color4(1.0f, 1.0f, 1.0f, 1.0f), 1.0f, 0);
            GraphicsDevice.BeginScene();
            var aspect = GraphicsDevice.Viewport.Width / (float)GraphicsDevice.Viewport.Height;

            switch (ViewPort3D.ProjectionType)
            {
                case ProjectionType.Perspective:
                    _projection = Matrix.PerspectiveFovRH(_fov, aspect, 0.01f, 10000f);
                    break;
                case ProjectionType.Orthogonal:
                    var width = (float)Models.Select(m => m.Bounds.Maximum.X).Max() * aspect;
                    var height = (float)Models.Select(m => m.Bounds.Maximum.Y).Max();
                    _projection = Matrix.OrthoRH(width / Scale, height / Scale, 1, 100);
                    break;
            }
            
            _view = Matrix.LookAtRH(CameraPosition, CameraTarget, CameraUpVector);
            _world = Matrix.Identity;
            // ENDTODO
            GraphicsDevice.SetTransform(TransformState.Projection, ref _projection);
            GraphicsDevice.SetTransform(TransformState.View, ref _view);
            GraphicsDevice.SetTransform(TransformState.World, ref _world);
            
            foreach (var model in Models)
            {
                model.Draw();
            }
            GraphicsDevice.EndScene();
            GraphicsDevice.Present();
        }
    }

    public enum ProjectionType
    {
        Perspective,
        Orthogonal
    }
}
