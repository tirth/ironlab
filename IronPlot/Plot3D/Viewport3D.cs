// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Media3D;
using IronPlot.ManagedD3D;
using SharpDX;

namespace IronPlot.Plotting3D
{
    //[ContentProperty("ModelsProperty")]
    public class Viewport3D : PlotPanelBase
    {     
        internal Trackball Trackball;
        private Axes3D _axes;
        private Viewport3DControl _viewport3DControl;
        private ViewportImage _viewport3DImage; 

        public Axes3D Axes => _axes;

        #region DependencyProperties
        
        public static readonly DependencyProperty ModelsProperty =
            DependencyProperty.Register("Models",
            typeof(Model3DCollection), typeof(Viewport3D),
            new PropertyMetadata(null));
        
        public static readonly DependencyProperty GraphToWorldProperty =
            DependencyProperty.Register("GraphToWorld",
            typeof(MatrixTransform3D), typeof(Viewport3D),
            new PropertyMetadata((MatrixTransform3D)Transform3D.Identity, OnGraphToWorldChanged));

        public static readonly DependencyProperty GraphMinProperty =
            DependencyProperty.Register("GraphMin",
            typeof(Point3D), typeof(Viewport3D),
            new PropertyMetadata(new Point3D(-10, -10, -10), OnUpdateGraphMinMax));

        public static readonly DependencyProperty GraphMaxProperty =
            DependencyProperty.Register("GraphMax",
            typeof(Point3D), typeof(Viewport3D),
            new PropertyMetadata(new Point3D(10, 10, 10), OnUpdateGraphMinMax));

        public static readonly DependencyProperty ProjectionTypeProperty =
            DependencyProperty.Register("ProjectionType",
            typeof(ProjectionType), typeof(Viewport3D),
            new FrameworkPropertyMetadata(ProjectionType.Perspective));

        public MatrixTransform3D GraphToWorld
        {
            get { return (MatrixTransform3D)GetValue(GraphToWorldProperty); }
            set { SetValue(GraphToWorldProperty, value); }
        }

        public Model3DCollection Models
        {
            get { return (Model3DCollection)GetValue(ModelsProperty); }
            set { SetValue(ModelsProperty, value); }
        }

        public Point3D GraphMin
        {
            get { return (Point3D)GetValue(GraphMinProperty); }
            set { SetValue(GraphMinProperty, value); }
        }

        public Point3D GraphMax
        {
            get { return (Point3D)GetValue(GraphMaxProperty); }
            set { SetValue(GraphMaxProperty, value); }
        }

        public ProjectionType ProjectionType
        {
            set { SetValue(ProjectionTypeProperty, value); }
            get { return (ProjectionType)GetValue(ProjectionTypeProperty); }
        }

        protected Point3D worldMin;
        public Point3D WorldMin
        {
            get
            {
                try
                {
                    worldMin = GraphToWorld.Transform(GraphMin);
                }
                catch
                {
                    worldMin = new Point3D(0, 0, 0);
                }
                return worldMin;
            }
            set
            {
                worldMin = value;
                UpdateGraphToWorld();
            }
        }

        protected Point3D worldMax;
        public Point3D WorldMax
        {
            get
            {
                try
                {
                    worldMax = GraphToWorld.Transform(GraphMax);
                }
                catch
                {
                    worldMax = new Point3D(0, 0, 0);
                }
                return worldMax;
            }
            set
            {
                worldMax = value;
                UpdateGraphToWorld();
            }
        }

        protected enum UpdateType { UpdateWorldMin, UpdateWorldMax, AlreadyUpdated };

        protected static void OnGraphToWorldChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
        }

        protected static void OnUpdateGraphMinMax(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Viewport3D)obj).UpdateGraphToWorld(); 
        }

        protected void UpdateGraphToWorld()
        {
            var graphMin = (Point3D)GetValue(GraphMinProperty);
            var graphMax = (Point3D)GetValue(GraphMaxProperty);
            var scaleX = (worldMax.X - worldMin.X) / (graphMax.X - graphMin.X);
            var scaleY = (worldMax.Y - worldMin.Y) / (graphMax.Y - graphMin.Y);
            var scaleZ = (worldMax.Z - worldMin.Z) / (graphMax.Z - graphMin.Z);
            var offX = -graphMin.X * scaleX + worldMin.X;
            var offY = -graphMin.Y * scaleY + worldMin.Y;
            var offZ = -graphMin.Z * scaleZ + worldMin.Z;

            var transform = new Matrix3D(scaleX, 0, 0, 0, 0, scaleY, 0, 0,
                0, 0, scaleZ, 0, offX, offY, offZ, 1);

            var matrixTransform = new MatrixTransform3D();
            matrixTransform.Matrix = transform;
            SetValue(GraphToWorldProperty, matrixTransform);
        }

        #endregion

        public Viewport3D()
        {
            Initialize();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            MeasureAnnotations(availableSize);
            // Return the region available for plotting and set legendRegion:
            var available = PlaceAnnotations(availableSize);
            _viewport3DControl.Measure(new Size(available.Width, available.Height));
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var final = PlaceAnnotations(finalSize);
            AxesRegion = final;
            _axes.UpdateLabels();
            ArrangeAnnotations(finalSize);
            _viewport3DControl.Arrange(final);
            return finalSize;
        }
        
        public void Initialize()
        {
            Background = null;
            _viewport3DControl = new Viewport3DControl();
            _viewport3DControl.SetValue(ZIndexProperty, 100);
            Children.Add(_viewport3DControl);

            _viewport3DImage = _viewport3DControl.Viewport3DImage;
            if (_viewport3DImage == null)
            {
                _axes = new Axes3D();
                SetValue(ModelsProperty, new Model3DCollection(null));
                return;
            }
            _viewport3DImage.ViewPort3D = this;

            SetValue(ModelsProperty, _viewport3DImage.Models);
            // Set the owner of the Model3DCollection to be the D3DImageViewport
            // This ensures that the Model3D objects are rendered by the D3DImageViewport
            _viewport3DImage.SetLayer2D(_viewport3DImage.Canvas, GraphToWorld);

            _viewport3DImage.Models.Changed += Models_Changed;

            Trackball = new Trackball();
            Trackball.EventSource = _viewport3DControl; //viewport3DImage.Canvas;
            Trackball.OnTrackBallMoved += trackball_TrackBallMoved;
            Trackball.OnTrackBallZoom += trackball_OnTrackBallZoom;
            Trackball.OnTrackBallTranslate += trackball_OnTrackBallTranslate;

            _axes = new Axes3D();
            _viewport3DImage.Models.Add(_axes);

            _viewport3DImage.CameraPosition = new Vector3(-3f, -3f, 2f);
            _viewport3DImage.CameraTarget = new Vector3(0f, 0f, 0f);
            //
            var bindingGraphMin = new Binding("GraphMin");
            bindingGraphMin.Source = this;
            bindingGraphMin.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(_axes, Axes3D.GraphMinProperty, bindingGraphMin);
            var bindingGraphMax = new Binding("GraphMax");
            bindingGraphMax.Source = this;
            bindingGraphMax.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(_axes, Axes3D.GraphMaxProperty, bindingGraphMax);
            var bindingGraphToWorld = new Binding("GraphToWorld");
            bindingGraphToWorld.Source = this;
            bindingGraphToWorld.Mode = BindingMode.OneWay;
            BindingOperations.SetBinding(_viewport3DImage, ViewportImage.ModelToWorldProperty, bindingGraphToWorld);
            ////
            GraphMax = new Point3D(1, 1, 1);
            GraphMin = new Point3D(-1, -1, -1);
            WorldMin = new Point3D(-1, -1, -1);
            WorldMax = new Point3D(1, 1, 1);
            _axes.UpdateOpenSides(FindPhi());

            IsVisibleChanged += Viewport3D_IsVisibleChanged;
        }

        void Viewport3D_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _viewport3DImage.Visible = (bool)e.NewValue;
        }

        /// <summary>
        /// Handle request to re-render the scene from the 3D models.
        /// Tell the ViewPort3D to re-render.
        /// </summary>
        protected void OnRequestRender(Object sender, EventArgs e)
        {
            _viewport3DImage.RequestRender();
        }

        /// <summary>
        /// Fired when model added or removed
        /// </summary>
        protected void Models_Changed(Object sender, ItemEventArgs e)
        {
            // Find new bounds that gives the world max and min
            var bounds = new Cuboid(new Point3D(0, 0, 0), new Point3D(0, 0, 0));
            foreach (var model in (Model3DCollection)sender)
            {
                bounds = Cuboid.Union(model.Bounds, bounds);
            }
            if (!bounds.IsPhysical)
            {
                bounds = new Cuboid(new Point3D(-1, -1, -1), new Point3D(1, 1, 1));
            }
            GraphMin = bounds.Minimum;
            GraphMax = bounds.Maximum;
        }
        
        protected void Base_OnDraw(Object sender, EventArgs e)
        {
            _axes.UpdateLabels();
        }

        protected void trackball_TrackBallMoved(Object sender, EventArgs e)
        {
            var delta = ((Trackball)sender).Delta;
            var deltaAxis = new Vector3();
            deltaAxis.X = (float)delta.Axis.X; 
            deltaAxis.Y = (float)delta.Axis.Y;
            deltaAxis.Z = (float)delta.Axis.Z;
            deltaAxis.Normalize();
            var cameraLookDirection = _viewport3DImage.CameraTarget - _viewport3DImage.CameraPosition;
            var cameraUpDirection = _viewport3DImage.CameraUpVector;
            cameraLookDirection.Normalize();
            // Subtract any component of cameraUpDirection along cameraLookDirection
            cameraUpDirection = cameraUpDirection - Vector3.Multiply(cameraLookDirection, Vector3.Dot(cameraUpDirection, cameraLookDirection));
            cameraUpDirection.Normalize();
            var cameraX = Vector3.Cross(cameraLookDirection, cameraUpDirection);
            // Get axis of rotation in the camera coordinates
            var deltaAxisWorld = Vector3.Multiply(cameraX, deltaAxis.X) +
                Vector3.Multiply(cameraUpDirection, deltaAxis.Y) +
                Vector3.Multiply(cameraLookDirection, -deltaAxis.Z);
            var cameraTransform = Matrix.RotationAxis(deltaAxisWorld, (float)(delta.Angle * Math.PI / 180.0));
            _viewport3DImage.CameraTarget = Vector3.Transform(_viewport3DImage.CameraTarget, cameraTransform).ToVector3();
            _viewport3DImage.CameraPosition = Vector3.Transform(_viewport3DImage.CameraPosition, cameraTransform).ToVector3();
            _viewport3DImage.CameraUpVector = Vector3.Transform(_viewport3DImage.CameraUpVector, cameraTransform).ToVector3();
            var newPhi = FindPhi();
            if (newPhi != _lastPhi)
            {
                _lastPhi = newPhi;
                _axes.UpdateOpenSides(newPhi);
            }
        }

        protected void trackball_OnTrackBallZoom(Object sender, EventArgs e)
        {
            var scale = ((Trackball)sender).Scale;
            switch (ProjectionType)
            {
                case ProjectionType.Perspective:
                    _viewport3DImage.CameraPosition = Vector3.Multiply(_viewport3DImage.CameraPosition, (float)scale);
                    break;
                case ProjectionType.Orthogonal:
                    _viewport3DImage.Scale = Convert.ToSingle(_viewport3DImage.Scale / scale);
                    _viewport3DImage.RequestRender();
                    break;
            }
        }

        protected void trackball_OnTrackBallTranslate(Object sender, EventArgs e)
        {
            var translation = ((Trackball)sender).Translation;
            var cameraLookDirection = _viewport3DImage.CameraTarget - _viewport3DImage.CameraPosition;
            var distance = cameraLookDirection.Length();
            var cameraUpDirection = _viewport3DImage.CameraUpVector;
            cameraLookDirection.Normalize();
            // Subtract any component of cameraUpDirection along cameraLookDirection
            cameraUpDirection = cameraUpDirection - Vector3.Multiply(cameraLookDirection, Vector3.Dot(cameraUpDirection, cameraLookDirection));
            cameraUpDirection.Normalize();
            var cameraX = Vector3.Cross(cameraLookDirection, cameraUpDirection);
            var scalingFactor = _viewport3DImage.TanSemiFov * distance * 2;
            var pan = -cameraX * (float)translation.X * scalingFactor + cameraUpDirection * (float)translation.Y * scalingFactor;
            _viewport3DImage.CameraPosition = _viewport3DImage.CameraPosition + pan;
            _viewport3DImage.CameraTarget = _viewport3DImage.CameraTarget + pan;
        }

        private double _lastPhi = -10;
        /// <summary>
        /// Calculate azimuthal angle
        /// </summary>
        /// <returns></returns>
        protected double FindPhi()
        {
            var vector = _viewport3DImage.CameraPosition - _viewport3DImage.CameraTarget;
            return Math.Atan2(vector.Y, vector.X);
        }

        /// <summary>
        /// Set the resolution of the 3D components.
        /// This is used for printing and copying to clipboard etc.
        /// </summary>
        /// <param name="dpi">Resolution in dpi</param>
        internal void SetResolution(int dpi)
        {
            var width = (int)(_viewport3DControl.ActualWidth * dpi / 96.0);
            var height = (int)(_viewport3DControl.ActualHeight * dpi / 96.0);
            Models.SetModelResolution(dpi);
            _viewport3DImage.SetImageSize((int)_viewport3DControl.ActualWidth, (int)_viewport3DControl.ActualHeight, dpi);
            _viewport3DImage.RenderScene();
        }
    }
}
