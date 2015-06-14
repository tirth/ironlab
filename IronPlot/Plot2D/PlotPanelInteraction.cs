// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace IronPlot
{
    public partial class PlotPanel : PlotPanelBase
    {
        // Mouse interaction
        private Point _mouseDragStartPoint;
        List<Axis2D> _axesBeingDragged;
        List<Range> _axesDragStartRanges;

        // Selection Window
        private bool _selectionStarted;
        private bool _dragging;
        private Point _selectionStart;
        protected Rectangle Selection;

        protected Rect CachedRegion;

        private Point _currentPosition;
        readonly DispatcherTimer _mousePositionTimer = new DispatcherTimer();

        protected void AddInteractionEvents()
        {
            Canvas.MouseLeftButtonUp += LeftClickEnd;
            Canvas.MouseLeftButtonDown += LeftClickStart;
            Canvas.MouseRightButtonUp += canvas_RightClickEnd;
            Canvas.MouseRightButtonDown += canvas_RightClickStart;
            Canvas.MouseMove += element_MouseMove;
            Canvas.MouseWheel += element_MouseWheel;
            var allAxes = Axes.XAxes.Concat(Axes.YAxes);

            _mousePositionTimer.Interval = TimeSpan.FromSeconds(0.05);
            _mousePositionTimer.Tick += mousePositionTimer_Tick;
        }

        internal void AddAxisInteractionEvents(IEnumerable<Axis2D> axes)
        {
            if (this is ColourBarPanel) return;
            foreach (var axis in axes) axis.MouseLeftButtonDown += LeftClickStart;
            foreach (var axis in axes) axis.MouseMove += element_MouseMove;
            foreach (var axis in axes) axis.MouseLeftButtonUp += LeftClickEnd;
            foreach (var axis in axes) axis.MouseWheel += element_MouseWheel;
        }

        internal void RemoveAxisInteractionEvents(IEnumerable<Axis2D> axes)
        {
            if (this is ColourBarPanel) return;
            foreach (var axis in axes) axis.MouseLeftButtonDown -= LeftClickStart;
            foreach (var axis in axes) axis.MouseMove -= element_MouseMove;
            foreach (var axis in axes) axis.MouseLeftButtonUp -= LeftClickEnd;
            foreach (var axis in axes) axis.MouseWheel -= element_MouseWheel;
        }

        protected void AddSelectionRectangle()
        {
            Selection = new Rectangle
            { Visibility = Visibility.Visible, ClipToBounds = true, Width = 0, Height = 0, 
                VerticalAlignment = VerticalAlignment.Top };
            var selectionBrush = new SolidColorBrush { Color = Brushes.Aquamarine.Color, Opacity = 0.5 };
            Selection.Fill = selectionBrush;
            Selection.StrokeDashOffset = 5; Selection.StrokeThickness = 0.99;
            var selectionBrush2 = new SolidColorBrush { Color = Color.FromArgb(255, 0, 0, 0) };
            Selection.Stroke = selectionBrush2;
            Selection.HorizontalAlignment = HorizontalAlignment.Left;
            var strokeDashArray1 = new DoubleCollection(2);
            strokeDashArray1.Add(3); strokeDashArray1.Add(3);
            Selection.StrokeDashArray = strokeDashArray1;
            Canvas.Children.Add(Selection);
            Selection.SetValue(ZIndexProperty, 1000);
        }

        protected void LeftClickStart(object sender, MouseButtonEventArgs e)
        {
            foreach (var item in plotItems)
            {
                if (item is Plot2DCurve && !Double.IsNaN((item as Plot2DCurve).AnnotationPosition.X))
                {
                    (item as Plot2DCurve).AnnotationPosition = new Point(Double.NaN, Double.NaN);
                }
            }
            
            var ctrlOrShift = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftShift);
            if (ctrlOrShift)
            {
                canvas_RightClickStart(sender, e);
                return;
            }

            // either whole canvas or single axis
            var isSingleAxis = (sender is Axis2D);
            if (e.ClickCount > 1)
            {
                _dragging = false;
                Cursor = Cursors.Arrow;
                List<Axis2D> allAxes;
                if (isSingleAxis) allAxes = new List<Axis2D> { sender as Axis2D };
                else allAxes = Axes.XAxes.Concat(Axes.YAxes).ToList();
                foreach (var axis in allAxes)
                {
                    var axisRange = GetRangeFromChildren(axis);
                    if (axisRange.Length != 0) axis.SetValue(Axis2D.RangeProperty, axisRange);
                }
            }
            else
            {
                if (isSingleAxis) _axesBeingDragged = new List<Axis2D> { sender as Axis2D };
                else _axesBeingDragged = Axes.XAxes.Concat(Axes.YAxes).ToList();
                StartDrag(e);
            }
            Canvas.CaptureMouse();
        }

        protected void StartDrag(MouseButtonEventArgs e)
        {
            _mouseDragStartPoint = e.GetPosition(this);
            _axesDragStartRanges = new List<Range>();
            foreach (var axis in _axesBeingDragged)
            {
                _axesDragStartRanges.Add(new Range(axis.Min, axis.Max));
            }
            Cursor = Cursors.ScrollAll;
            _dragging = true;
        }

        protected void MoveDrag(MouseEventArgs e)
        {
            // Get the new mouse position. 
            var mouseDragCurrentPoint = e.GetPosition(this);

            var delta = new Point(
                (mouseDragCurrentPoint.X - _mouseDragStartPoint.X),
                (mouseDragCurrentPoint.Y - _mouseDragStartPoint.Y));

            var index = 0;
            foreach (var axis in _axesBeingDragged)
            {
                double offset;
                if (axis is XAxis)
                    offset = -delta.X / axis.Scale;
                else offset = delta.Y / axis.Scale;
                axis.SetValue(Axis2D.RangeProperty, new Range(
                    axis.CanvasTransform(axis.GraphTransform(_axesDragStartRanges[index].Min) + offset),
                    axis.CanvasTransform(axis.GraphTransform(_axesDragStartRanges[index].Max) + offset)));
                index++;
            }
        }

        protected void EndDrag(object sender)
        {
            var element = (UIElement)sender;
            if (element.IsMouseCaptured)
            {
                Cursor = Cursors.Arrow;
                _dragging = false;
                MarginChangeTimer.Interval = TimeSpan.FromSeconds(0.2);
                MarginChangeTimer.Start();
            }
            element.ReleaseMouseCapture();
        }

        protected void canvas_RightClickStart(object sender, MouseButtonEventArgs e)
        {
            _selectionStarted = true;
            _selectionStart = e.GetPosition(Canvas);
            Selection.Width = 0;
            Selection.Height = 0;
            Canvas.CaptureMouse();
        }

        protected void LeftClickEnd(object sender, MouseButtonEventArgs e)
        {
            if (_selectionStarted)
            {
                canvas_RightClickEnd(sender, e);
                return;
            }

            EndDrag(sender);
        }

        protected void canvas_RightClickEnd(object sender, MouseButtonEventArgs e)
        {
            if (Canvas.IsMouseCaptured)
            {
                if (_selectionStarted)
                {
                    Selection.Width = 0;
                    Selection.Height = 0;
                    _selectionStarted = false;
                    var selectionEnd = e.GetPosition(Canvas);
                    if ((Math.Abs(_selectionStart.X - selectionEnd.X) <= 1) ||
                       (Math.Abs(_selectionStart.Y - selectionEnd.Y) <= 1))
                    {
                        return;
                    }
                    foreach (var axis in Axes.XAxes)
                    {
                        axis.SetValue(Axis2D.RangeProperty, new Range(
                            Math.Min(axis.CanvasToGraph(_selectionStart.X), axis.CanvasToGraph(selectionEnd.X)),
                            Math.Max(axis.CanvasToGraph(_selectionStart.X), axis.CanvasToGraph(selectionEnd.X))));
                    }
                    foreach (var axis in Axes.YAxes)
                    {
                        axis.SetValue(Axis2D.RangeProperty, new Range(
                            Math.Min(axis.CanvasToGraph(_selectionStart.Y), axis.CanvasToGraph(selectionEnd.Y)),
                            Math.Max(axis.CanvasToGraph(_selectionStart.Y), axis.CanvasToGraph(selectionEnd.Y))));
                    }
                    _selectionStarted = false;
                }
            }
            Canvas.ReleaseMouseCapture();
            e.Handled = true;
        }

        protected void element_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var isSingleAxis = (sender is Axis2D);
            double delta = e.Delta / 120;
            var canvasPosition = e.GetPosition(Canvas);
            var factor = Math.Pow(1.4, delta);
            //
            List<Axis2D> zoomAxes;
            if (isSingleAxis) zoomAxes = new List<Axis2D> { sender as Axis2D };
            else zoomAxes = Axes.XAxes.Concat(Axes.YAxes).ToList();
            foreach (var axis in zoomAxes)
            {
                double axisMid;
                if (axis is XAxis) axisMid = axis.GraphTransform(axis.CanvasToGraph(canvasPosition.X));
                else axisMid = axis.GraphTransform(axis.CanvasToGraph(canvasPosition.Y));
                var axisMin = axis.GraphTransform(axis.Min);
                var axisMax = axis.GraphTransform(axis.Max);
                axis.SetValue(Axis2D.RangeProperty, new Range(axis.CanvasTransform(axisMid + (axisMin - axisMid) / factor), axis.CanvasTransform(axisMid + (axisMax - axisMid) / factor)));
            }
        }

        protected void element_MouseMove(object sender, MouseEventArgs e)
        {
            _currentPosition = e.GetPosition(Canvas);
            if (Canvas.IsMouseCaptured)
            {
                if (_dragging)
                {
                    MoveDrag(e);
                }
                if (_selectionStarted)
                {
                    var rect = new Rect(_selectionStart, _currentPosition);
                    Selection.RenderTransform = new TranslateTransform(rect.X, rect.Y);
                    Selection.Width = rect.Width;
                    Selection.Height = rect.Height;
                }
            }
            _mousePositionTimer.Stop(); _mousePositionTimer.Start();
            foreach (var item in plotItems)
            {
                if ((item is Plot2DCurve) && !Double.IsNaN((item as Plot2DCurve).AnnotationPosition.X)) (item as Plot2DCurve).AnnotationPosition = _currentPosition;
            }
        }

        protected void marginChangeTimer_Tick(Object sender, EventArgs e)
        {
            MarginChangeTimer.Stop();
            MarginChangeTimer.Interval = TimeSpan.FromSeconds(0.0);
            InvalidateMeasure();
        }

        void mousePositionTimer_Tick(object sender, EventArgs e)
        {
            foreach (var item in plotItems)
            {
                if (item is Plot2DCurve)
                {
                    if (Double.IsNaN((item as Plot2DCurve).AnnotationPosition.X))
                    {
                        if (((item as Plot2DCurve).Line.IsMouseOver || (item as Plot2DCurve).Markers.IsMouseOver))
                        {
                            (item as Plot2DCurve).AnnotationPosition = _currentPosition;
                        }
                    }
                }
            }
        }
    }
}
