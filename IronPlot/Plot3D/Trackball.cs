// Copyright (c) 2010 Joe Moorhouse (additions only)

//---------------------------------------------------------------------------
//
// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Limited Permissive License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/limitedpermissivelicense.mspx
// All other rights reserved.
//
// This file is part of the 3D Tools for Windows Presentation Foundation
// project.  For more information, see:
// 
// http://CodePlex.com/Wiki/View.aspx?ProjectName=3DTools
//
// The following article discusses the mechanics behind this
// trackball implementation: http://viewport3d.com/trackball.htm
//
// Reading the article is not required to use this sample code,
// but skimming it might be useful.
//
//---------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;

namespace IronPlot.Plotting3D
{
    public delegate void TrackballEventHandler(object sender, EventArgs e);
    /// <summary>
    ///     Trackball is a utility class which observes the mouse events
    ///     on a specified FrameworkElement and produces a Transform3D
    ///     with the resultant rotation and Scale.
    /// 
    ///     Example Usage:
    /// 
    ///         Trackball trackball = new Trackball();
    ///         trackball.EventSource = myElement;
    ///         myViewport3D.Camera.Transform = trackball.Transform;
    /// 
    ///     Because Viewport3Ds only raise events when the mouse is over the
    ///     rendered 3D geometry (as opposed to not when the mouse is within
    ///     the layout bounds) you usually want to use another element as 
    ///     your EventSource.  For example, a transparent border placed on
    ///     top of your Viewport3D works well:
    ///     
    ///         <Grid>
    ///           <ColumnDefinition />
    ///           <RowDefinition />
    ///           <Viewport3D Name="myViewport" ClipToBounds="True" Grid.Row="0" Grid.Column="0" />
    ///           <Border Name="myElement" Background="Transparent" Grid.Row="0" Grid.Column="0" />
    ///         </Grid>
    ///     
    ///     NOTE: The Transform property may be shared by multiple Cameras
    ///           if you want to have auxilary views following the trackball.
    /// 
    ///           It can also be useful to share the Transform property with
    ///           models in the scene that you want to move with the camera.
    ///           (For example, the Trackport3D's headlight is implemented
    ///           this way.)
    /// 
    ///           You may also use a Transform3DGroup to combine the
    ///           Transform property with additional Transforms.
    /// </summary> 
    public class Trackball : DependencyObject
    {
        protected FrameworkElement _eventSource;
        protected Point PreviousPosition2D;
        protected Vector3D PreviousPosition3D = new Vector3D(0, 0, 1);

        protected Transform3DGroup _transform;
        protected ScaleTransform3D _scale = new ScaleTransform3D();
        protected AxisAngleRotation3D _rotation = new AxisAngleRotation3D();
        protected TranslateTransform3D _translate = new TranslateTransform3D();

        protected bool MouseLeftDown, MouseRightDown;
        
        Quaternion _delta;
        double scale;
        Point _translation;

        public event TrackballEventHandler OnTrackBallMoved;
        public event TrackballEventHandler OnTrackBallZoom;
        public event TrackballEventHandler OnTrackBallTranslate;

        protected virtual void RaiseTrackballMovedEvent(EventArgs e)
        {
            if (OnTrackBallMoved != null)
                OnTrackBallMoved(this, e);
        }

        protected virtual void RaiseZoomEvent(EventArgs e)
        {
            if (OnTrackBallZoom != null)
                OnTrackBallZoom(this, e);
        }

        protected virtual void RaiseTranslateEvent(EventArgs e)
        {
            if (OnTrackBallTranslate != null)
                OnTrackBallTranslate(this, e);
        }

        public Quaternion Delta => _delta;

        public double Scale => scale;

        public Point Translation => _translation;

        public Trackball()
        {
            _transform = new Transform3DGroup();
            _transform.Children.Add(_scale);
            _transform.Children.Add(new RotateTransform3D(_rotation));
        }

        /// <summary>
        ///     A transform to move the camera or scene to the trackball's
        ///     current orientation and Scale.
        /// </summary>
        public Transform3DGroup Transform => _transform;

        /// <summary>
        ///     A transform to move the camera or scene to the trackball's
        ///     current orientation and Scale.
        /// </summary>
        public AxisAngleRotation3D Rotation => _rotation;

        #region Event Handling

        /// <summary>
        /// The FrameworkElement we listen to for mouse events.
        /// </summary>
        public FrameworkElement EventSource
        {
            get { return _eventSource; }

            set
            {
                if (_eventSource != null)
                {
                    _eventSource.MouseDown -= OnMouseDown;
                    _eventSource.MouseUp -= OnMouseUp;
                    _eventSource.MouseMove -= OnMouseMove;
                    _eventSource.MouseWheel -= OnMouseWheel;
                }

                _eventSource = value;

                _eventSource.MouseDown += OnMouseDown;
                _eventSource.MouseUp += OnMouseUp;
                _eventSource.MouseMove += OnMouseMove;
                _eventSource.MouseWheel += OnMouseWheel;
            }
        }

        protected virtual void OnMouseDown(object sender, MouseEventArgs e)
        {
            Mouse.Capture(EventSource, CaptureMode.Element);
            PreviousPosition2D = e.GetPosition(EventSource);
            PreviousPosition3D = ProjectToTrackball(
                EventSource.ActualWidth,
                EventSource.ActualHeight,
                PreviousPosition2D);
        }

        protected virtual void OnMouseUp(object sender, MouseEventArgs e)
        {
            Mouse.Capture(EventSource, CaptureMode.None);
        }

        protected virtual void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double delta = -e.Delta / 120;
            scale = Math.Pow(1.1, delta);
            Zoom(scale);
        }

        protected virtual void OnMouseMove(object sender, MouseEventArgs e)
        {
            var currentPosition = e.GetPosition(EventSource);
            var ctrlOrShift = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftShift);

            // Prefer tracking to zooming if both buttons are pressed.
            if (e.LeftButton == MouseButtonState.Pressed && !ctrlOrShift)
            {
                if (MouseLeftDown)
                {
                    Track(currentPosition);
                }
                else
                {
                    MouseLeftDown = true;
                    Mouse.Capture(EventSource, CaptureMode.Element);
                    PreviousPosition2D = e.GetPosition(EventSource);
                    PreviousPosition3D = ProjectToTrackball(
                        EventSource.ActualWidth,
                        EventSource.ActualHeight,
                        PreviousPosition2D);
                }
            }
            else if (e.RightButton == MouseButtonState.Pressed || (e.LeftButton == MouseButtonState.Pressed && ctrlOrShift))
            {
                if (MouseRightDown)
                {
                    //if (Zoom(currentPosition)) e.Handled = true;
                    Translate(currentPosition);
                }
                else
                {
                    MouseRightDown = true;
                    Mouse.Capture(EventSource, CaptureMode.Element);
                    PreviousPosition2D = e.GetPosition(EventSource);
                    PreviousPosition3D = ProjectToTrackball(
                        EventSource.ActualWidth,
                        EventSource.ActualHeight,
                        PreviousPosition2D);
                }
            }
            else
            {
                MouseLeftDown = false;
                MouseRightDown = false;
            }

            PreviousPosition2D = currentPosition;
        }

        #endregion Event Handling

        protected void Track(Point currentPosition)
        {
            var currentPosition3D = ProjectToTrackball(
                EventSource.ActualWidth, EventSource.ActualHeight, currentPosition);

            var axis = Vector3D.CrossProduct(PreviousPosition3D, currentPosition3D);
            var angle = Vector3D.AngleBetween(PreviousPosition3D, currentPosition3D);
            if (angle == 0.0) return;
            _delta = new Quaternion(axis, -angle);

            // Get the current orientantion from the RotateTransform3D
            var r = _rotation;
            var q = new Quaternion(_rotation.Axis, _rotation.Angle);

            // Compose the delta with the previous orientation
            q *= _delta;

            // Write the new orientation back to the Rotation3D
            _rotation.Axis = q.Axis;
            _rotation.Angle = q.Angle;

            PreviousPosition3D = currentPosition3D;

            RaiseTrackballMovedEvent(EventArgs.Empty);
        }

        protected Vector3D ProjectToTrackball(double width, double height, Point point)
        {
            var x = point.X / (width / 2);    // Scale so bounds map to [0,0] - [2,2]
            var y = point.Y / (height / 2);

            x = x - 1;                           // Translate 0,0 to the center
            y = 1 - y;                           // Flip so +Y is up instead of down

            var z2 = 1 - x * x - y * y;       // z^2 = 1 - x^2 - y^2
            var z = z2 > 0 ? Math.Sqrt(z2) : 0;
            return new Vector3D(x, y, z);
        }

        protected bool Zoom(Point currentPosition)
        {
            var yDelta = currentPosition.Y - PreviousPosition2D.Y;
            scale = Math.Exp(yDelta / 100);    // e^(yDelta/100) is fairly arbitrary.
            return Zoom(scale);
        }

        protected bool Zoom(double factor)
        {
            _scale.ScaleX *= Scale;
            _scale.ScaleY *= Scale;
            _scale.ScaleZ *= Scale;

            RaiseZoomEvent(EventArgs.Empty);

            return (factor != 1.0);
        }

        protected void Translate(Point currentPosition)
        {
            _translation = new Point((currentPosition.X - PreviousPosition2D.X) / EventSource.ActualWidth, 
                (currentPosition.Y - PreviousPosition2D.Y) / EventSource.ActualHeight);
            RaiseTranslateEvent(EventArgs.Empty);
        }
    }
}

