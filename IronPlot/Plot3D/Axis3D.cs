// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace IronPlot.Plotting3D
{
    public interface I2DLayer
    {
        Point CanvasPointFrom3DPoint(Point3D point3D);
        Canvas Canvas { get; }
    }

    public enum XAxisType { MinusY, PlusY };
    public enum YAxisType { MinusX, PlusX };
    public enum ZAxisType { MinusXMinusY, MinusXPlusY, PlusXPlusY, PlusXMinusY };
    
    /// <summary>
    /// Axis for 3D plots. The Axis3D comprises ticks and labels (but not the line itself). 
    /// </summary>
    public abstract class Axis3D : Axis
    {
        // All 3D axis objects are associated with an Axes3D object.
        protected Axes3D Axes;

        // The axes will have multiple x, y and z axes. These are administered by Axis3DCollection objects.
        protected Axis3DCollection AxisCollection;

        //protected string axisLabelText;

        protected LinesModel3D model3D;

        protected List<TextBlock> AxisLabels;

        protected TextBlock axisLabel;

        static Axis3D()
        {
            LabelsVisibleProperty.OverrideMetadata(typeof(Axis3D), new PropertyMetadata(true, OnLabelsVisibleChanged));
            TicksVisibleProperty.OverrideMetadata(typeof(Axis3D), new PropertyMetadata(true, OnTicksVisibleChanged));
            NumberOfTicksProperty.OverrideMetadata(typeof(Axis3D), new PropertyMetadata(10, OnNumberOfTicksChanged));
            TickLengthProperty.OverrideMetadata(typeof(Axis3D), new PropertyMetadata(0.05, OnTickLengthChanged));
        }

        protected override void UpdateTicksAndLabels()
        {
            UpdateLabels();
            UpdateLabelPositions(true);
        }

        internal static void OnLabelsVisibleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == (bool)e.OldValue) return;
            var label = ((Axis3D)obj).axisLabel;
            if ((bool)e.NewValue == false)
            {
                foreach (var textBlock in ((Axis3D)obj).AxisLabels)
                {
                    textBlock.Visibility = Visibility.Collapsed;
                }
                if (label != null) label.Visibility = Visibility.Collapsed;
            }
            if ((bool)e.NewValue)
            {
                foreach (var textBlock in ((Axis3D)obj).AxisLabels)
                {
                    textBlock.Visibility = Visibility.Visible;
                }
                if (label != null) label.Visibility = Visibility.Visible;
            }
        }

        internal static void OnTicksVisibleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == (bool)e.OldValue) return;
            ((Axis3D)obj).model3D.IsVisible = (bool)e.NewValue;
            ((Axis3D)obj).model3D.RequestRender(EventArgs.Empty);
        }

        internal new static void OnNumberOfTicksChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axis3D)obj).DeriveTicks();
            ((Axis3D)obj).UpdateLabels();
            ((Axis3D)obj).UpdateLabelPositions(true);
            ((Axis3D)obj).model3D.RequestRender(EventArgs.Empty);
        }

        internal static void OnTickLengthChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axis3D)obj).DeriveTicks();
            ((Axis3D)obj).UpdateLabels();
            ((Axis3D)obj).UpdateLabelPositions(true);
            ((Axis3D)obj).model3D.RequestRender(EventArgs.Empty);
        }

        public Axis3D(Axes3D axes, Axis3DCollection axisCollection)
        {
            AxisLabels = new List<TextBlock>();
            _labelProperties = new LabelProperties(axisCollection.TickLabels);
            model3D = new LinesModel3D();
            Axes = axes;
            AxisCollection = axisCollection;
            Axes.Children.Add(model3D);
        }

        public TextBlock AxisLabel => axisLabel;

        private readonly LabelProperties _labelProperties;

        public LabelProperties Labels => _labelProperties;

        public abstract Point3D TickStartPoint(int i);
        public abstract Point3D TickEndPoint(int i);
        Point3D _start, _end, _centre, _offsetCentre;
        public abstract void AxisProperties(ref Point3D start, ref Point3D end, ref Point3D centre, ref Point3D offsetCentre);

        internal LinesModel3D Model3D => model3D;

        internal void UpdateLabels()
        {
            if (Axes.Layer2D == null) return;
            if (axisLabel == null)
            {
                axisLabel = new TextBlock();
                AxisCollection.AxisLabels.BindTextBlock(axisLabel);
                Axes.Layer2D.Canvas.Children.Add(axisLabel);
            }
            TextBlock currentTextBlock;
            for (var i = 0; i < Ticks.Length; ++i)
            {
                if (i > (AxisLabels.Count - 1))
                {
                    currentTextBlock = new TextBlock();
                    _labelProperties.BindTextBlock(currentTextBlock);
                    if ((bool)GetValue(LabelsVisibleProperty) == false) currentTextBlock.Visibility = Visibility.Collapsed;
                    Axes.Layer2D.Canvas.Children.Add(currentTextBlock);
                    AxisLabels.Add(currentTextBlock);
                }
                else currentTextBlock = AxisLabels[i];
                UpdateLabelText(i);
                AddTextToBlock(currentTextBlock, i);
                currentTextBlock.TextAlignment = TextAlignment.Center;
                //currentTextBlock.;
            }
            UpdateLabelPositions(false);
            // Cycle through any now redundant TextBlocks and make invisible
            for (var i = Ticks.Length; i < AxisLabels.Count; ++i)
            {
                AxisLabels[i].Text = "";
            }
        }

        internal void UpdateLabelPositions(bool allowHide)
        {
            if (((bool)GetValue(LabelsVisibleProperty) == false) || (Axes.Layer2D == null)) return;
            Point3D tickStartPoint3D;
            Point3D tickEndPoint3D;
            Point tickStartPoint2D;
            Point tickEndPoint2D;
            double toBottom = 0, toRight = 0;
            TextBlock currentTextBlock;
            var missOutMax = 0;
            var missOut = 0;
            var lastRect = new Rect(new Point(Double.MaxValue, Double.MaxValue), new Point(Double.MaxValue, Double.MaxValue));
            Rect currentRect;
            for (var i = 0; i < Ticks.Length; ++i)
            {
                currentTextBlock = AxisLabels[i];
                currentTextBlock.Visibility = Visibility.Visible;
                tickEndPoint3D = TickEndPoint(i);
                tickEndPoint2D = Axes.Layer2D.CanvasPointFrom3DPoint(tickEndPoint3D);
                if (Double.IsInfinity(tickEndPoint2D.X) || Double.IsInfinity(tickEndPoint2D.Y))
                {
                    continue;
                }
                if (i == 0)
                {
                    tickStartPoint3D = TickStartPoint(i);
                    tickStartPoint2D = Axes.Layer2D.CanvasPointFrom3DPoint(tickStartPoint3D);
                    toBottom = (tickEndPoint2D.Y > tickStartPoint2D.Y) ? 0.0 : 1.0; // decide whether end of tick hits top or bottom of label
                    if (Math.Abs((tickEndPoint2D.Y - tickStartPoint2D.Y) / (tickEndPoint2D.X - tickStartPoint2D.X)) < 0.4)
                    {
                        toBottom = 0.5; // end of tick hits the label half way up
                    }
                    toRight = (tickEndPoint2D.X > tickStartPoint2D.X) ? 0.0 : 1.0; // end of tick hits the left of label if 0.0; otherwise the right
                }
                var offset = new Point(toRight * currentTextBlock.ActualWidth,
                    toBottom * currentTextBlock.ActualHeight);
                currentRect = new Rect(new Point(tickEndPoint2D.X - offset.X, tickEndPoint2D.Y - offset.Y),
                    new Size(currentTextBlock.ActualWidth, currentTextBlock.ActualHeight));
                currentTextBlock.SetValue(System.Windows.Controls.Canvas.LeftProperty, currentRect.Left);
                currentTextBlock.SetValue(System.Windows.Controls.Canvas.TopProperty, currentRect.Top);
                if (Intersect(currentRect, lastRect))
                {
                    missOut++;
                }
                else
                {
                    lastRect = currentRect;
                    missOutMax = Math.Max(missOut, missOutMax);
                    missOut = 0;
                }
            }
            missOutMax = Math.Max(missOut, missOutMax);
            missOut = 0;
            for (var i = 0; i < Ticks.Length; ++i)
            {
                if ((missOut < missOutMax) && (i > 0))
                {
                    missOut += 1;
                    if (allowHide) AxisLabels[i].Visibility = Visibility.Collapsed;
                }
                else
                {
                    missOut = 0;
                }
            }
            UpdateAxisLabelPosition();
        }

        internal override void DeriveTicks()
        {
            base.DeriveTicks();
            // update axis information
            AxisProperties(ref _start, ref _end, ref _centre, ref _offsetCentre);
            UpdateModel3D();
        }
        
        /// <summary>
        /// Add ticks to the Model3D
        /// </summary>
        internal void UpdateModel3D()
        {
            model3D.Points.Clear();
            for (var i = 0; i < Ticks.Length; ++i)
            {
                model3D.Points.Add(new Point3DColor(TickStartPoint(i)));
                model3D.Points.Add(new Point3DColor(TickEndPoint(i)));
            }
            model3D.UpdateFromPoints();
        }

        private bool Intersect(Rect rect1, Rect rect2)
        {
            if ((rect2.Left > rect1.Right) || (rect2.Right < rect1.Left)) return false;
            if ((rect2.Top > rect1.Bottom) || (rect2.Bottom < rect1.Top)) return false;
            return true;
        }

        protected void UpdateAxisLabelPosition()
        {
            if (axisLabel == null) return;
            var labelWidth = axisLabel.ActualWidth;
            var labelHeight = axisLabel.ActualHeight;

            //Point3D start, end, centre, offsetCentre;
            // Find perpendicular distance of all labels from axis
            var start2D = Axes.Layer2D.CanvasPointFrom3DPoint(_start);
            var end2D = Axes.Layer2D.CanvasPointFrom3DPoint(_end);
            var centre2D = Axes.Layer2D.CanvasPointFrom3DPoint(_centre);
            var offsetCentre2D = Axes.Layer2D.CanvasPointFrom3DPoint(_offsetCentre);
            double distance;
            var axis = new Line(start2D.ToVector(), end2D.ToVector() - start2D.ToVector());
            var centreTick = (offsetCentre2D.ToVector() - centre2D.ToVector());
            centreTick.Normalize();
            // Calculate maximum perpendicular distance of furthest point on any label from line
            distance = 0;
            Vector offset;
            var axisNormal = axis.GetNormalDirection(); axisNormal.Normalize();
            if (Math.Abs(Vector.AngleBetween(axisNormal, centreTick)) > 90.0) axisNormal = axisNormal * -1.0;
            if (axisNormal.X < 0 && axisNormal.Y > 0) // labels in bottom left quadrant of axis
            {
                foreach (var label in AxisLabels)
                {
                    var newDistance = Math.Abs(Geomery.PointToLineDistance((new Point((double)label.GetValue(System.Windows.Controls.Canvas.LeftProperty), 
                        (double)label.GetValue(System.Windows.Controls.Canvas.TopProperty) + label.ActualHeight)).ToVector(), axis));
                    if (newDistance > distance) distance = newDistance;
                }
                // Top right corner of axis label
                offset = new Vector(labelWidth / 2.0, -labelHeight / 2.0);
            }
            else if (axisNormal.X >= 0 && axisNormal.Y > 0) // bottom right quadrant
            {
                foreach (var label in AxisLabels)
                {
                    var newDistance = Math.Abs(Geomery.PointToLineDistance((new Point((double)label.GetValue(System.Windows.Controls.Canvas.LeftProperty) + label.ActualWidth, 
                        (double)label.GetValue(System.Windows.Controls.Canvas.TopProperty) + label.ActualHeight)).ToVector(), axis));
                    if (newDistance > distance) distance = newDistance;
                }
                // Top left corner of axis label
                offset = new Vector(-labelWidth / 2.0, -labelHeight / 2.0);
            }
            else if (axisNormal.X < 0 && axisNormal.Y <= 0) // top left quadrant
            {
                foreach (var label in AxisLabels)
                {
                    var newDistance = Math.Abs(Geomery.PointToLineDistance((new Point((double)label.GetValue(System.Windows.Controls.Canvas.LeftProperty),
                        (double)label.GetValue(System.Windows.Controls.Canvas.TopProperty))).ToVector(), axis));
                    if (newDistance > distance) distance = newDistance;
                }
                // Bottom right corner of axis label
                offset = new Vector(labelWidth / 2.0, labelHeight / 2.0);
            }
            else 
            {
                foreach (var label in AxisLabels) // top right quadrant
                {
                    var newDistance = Math.Abs(Geomery.PointToLineDistance((new Point((double)label.GetValue(System.Windows.Controls.Canvas.LeftProperty) + label.ActualWidth,
                        (double)label.GetValue(System.Windows.Controls.Canvas.TopProperty))).ToVector(), axis));
                    if (newDistance > distance) distance = newDistance;
                }
                // Bottom left corner of axis label
                offset = new Vector(-labelWidth / 2.0, labelHeight / 2.0);
            }
            // Our new label is on this line:
            var offsetAxis = new Line(axis.PointOnLine + axisNormal * distance, axis.Direction);
            // And also on this line:
            //Line offsetCentreTick = new Line(centre2D.ToVector() + Offset, centreTick);
            var offsetCentreTick = new Line(centre2D.ToVector() + offset, axisNormal);
            var topLeft = offsetAxis.IntersectionWithLine(offsetCentreTick) - offset - (new Vector(labelWidth / 2.0, labelHeight / 2.0));
            axisLabel.SetValue(System.Windows.Controls.Canvas.LeftProperty, topLeft.X);
            axisLabel.SetValue(System.Windows.Controls.Canvas.TopProperty, topLeft.Y);
        }

    }

    public class XAxis3D : Axis3D
    {
        readonly XAxisType _axisType = XAxisType.MinusY;
        
        public override double Min
        {
            set
            {
                var oldPoint = Axes.GraphMin;
                oldPoint.X = value;
                Axes.GraphMin = oldPoint;
            }
            get { return Axes.GraphMin.X; }
        }

        public override double Max
        {
            set
            {
                var oldPoint = Axes.GraphMax;
                oldPoint.X = value;
                Axes.GraphMax = oldPoint;
            }
            get { return Axes.GraphMax.X; }
        }

        private double _offset;

        public XAxis3D(Axes3D axes, Axis3DCollection axisCollection)
            : base(axes, axisCollection)
        { 
        }

        public XAxis3D(Axes3D axes, Axis3DCollection axisCollection, XAxisType axisType)
            : base(axes, axisCollection)
        {
            _axisType = axisType;
        }

        public override Point3D TickStartPoint(int i)
        {
            Point3D tickStartPoint3D;
            if (_axisType == XAxisType.MinusY)
                tickStartPoint3D = new Point3D(Ticks[i], Axes.GraphMin.Y, Axes.GraphMin.Z);
            else 
                tickStartPoint3D = new Point3D(Ticks[i], Axes.GraphMax.Y, Axes.GraphMin.Z);
            return tickStartPoint3D;
        }

        public override Point3D TickEndPoint(int i)
        {
            _offset = TickLength * (Axes.GraphMax.Y - Axes.GraphMin.Y);
            Point3D tickEndPoint3D;
            if (_axisType == XAxisType.MinusY)
                tickEndPoint3D = new Point3D(Ticks[i], Axes.GraphMin.Y - _offset, Axes.GraphMin.Z);
            else
                tickEndPoint3D = new Point3D(Ticks[i], Axes.GraphMax.Y + _offset, Axes.GraphMin.Z);
            return tickEndPoint3D;
        }

        public override void AxisProperties(ref Point3D start, ref Point3D end, ref Point3D centre, ref Point3D offsetCentre)
        {
            _offset = TickLength * (Axes.GraphMax.Y - Axes.GraphMin.Y);
            if (_axisType == XAxisType.MinusY)
            {
                start = new Point3D(Min, Axes.GraphMin.Y, Axes.GraphMin.Z);
                end = new Point3D(Max, start.Y, start.Z);
                centre = new Point3D((Max + Min) / 2.0, start.Y, start.Z);
                offsetCentre = new Point3D(centre.X, centre.Y - _offset, centre.Z);
            }
            else
            {
                start = new Point3D(Min, Axes.GraphMax.Y, Axes.GraphMin.Z);
                end = new Point3D(Max, start.Y, start.Z);
                centre = new Point3D((Max + Min) / 2.0, start.Y, start.Z);
                offsetCentre = new Point3D(centre.X, centre.Y + _offset, centre.Z);
            }
        }
    }

    public class YAxis3D : Axis3D
    {
        readonly YAxisType _axisType = YAxisType.MinusX;
        
        public override double Min
        {
            set
            {
                var oldPoint = Axes.GraphMin;
                oldPoint.Y = value;
                Axes.GraphMin = oldPoint;
            }
            get { return Axes.GraphMin.Y; }
        }

        public override double Max
        {
            set
            {
                var oldPoint = Axes.GraphMax;
                oldPoint.Y = value;
                Axes.GraphMax = oldPoint;
            }
            get { return Axes.GraphMax.Y; }
        }

        private double _offset;

        public YAxis3D(Axes3D axes, Axis3DCollection axisCollection)
            : base(axes, axisCollection)
        { }

        public YAxis3D(Axes3D axes, Axis3DCollection axisCollection, YAxisType axisType)
            : base(axes, axisCollection)
        {
            _axisType = axisType;
        }

        public override Point3D TickStartPoint(int i)
        {
            Point3D tickStartPoint3D;
            if (_axisType == YAxisType.MinusX)
                tickStartPoint3D = new Point3D(Axes.GraphMin.X, Ticks[i], Axes.GraphMin.Z);
            else
                tickStartPoint3D = new Point3D(Axes.GraphMax.X, Ticks[i], Axes.GraphMin.Z);
            return tickStartPoint3D;
        }

        public override Point3D TickEndPoint(int i)
        {
            _offset = TickLength * (Axes.GraphMax.X - Axes.GraphMin.X);
            Point3D tickEndPoint3D;
            if (_axisType == YAxisType.MinusX)
                tickEndPoint3D = new Point3D(Axes.GraphMin.X - _offset, Ticks[i], Axes.GraphMin.Z);
            else
                tickEndPoint3D = new Point3D(Axes.GraphMax.X + _offset, Ticks[i], Axes.GraphMin.Z);
            return tickEndPoint3D;
        }

        public override void AxisProperties(ref Point3D start, ref Point3D end, ref Point3D centre, ref Point3D offsetCentre)
        {
            _offset = TickLength * (Axes.GraphMax.X - Axes.GraphMin.X);
            if (_axisType == YAxisType.MinusX)
            {
                start = new Point3D(Axes.GraphMin.X, Min, Axes.GraphMin.Z);
                end = new Point3D(start.X, Max, start.Z);
                centre = new Point3D(start.X, (Max + Min) / 2.0, start.Z);
                offsetCentre = new Point3D(centre.X - _offset, centre.Y, centre.Z);
            }
            else
            {
                start = new Point3D(Axes.GraphMax.X, Min, Axes.GraphMin.Z);
                end = new Point3D(start.X, Max, start.Z);
                centre = new Point3D(start.X, (Max + Min) / 2.0, start.Z);
                offsetCentre = new Point3D(centre.X + _offset, centre.Y, centre.Z);
            }
        }
    }

    public class ZAxis3D : Axis3D
    {
        readonly ZAxisType _axisType = ZAxisType.MinusXPlusY;
        
        public override double Min
        {
            set
            {
                var oldPoint = Axes.GraphMin;
                oldPoint.Z = value;
                Axes.GraphMin = oldPoint;
            }
            get { return Axes.GraphMin.Z; }
        }

        public override double Max
        {
            set
            {
                var oldPoint = Axes.GraphMax;
                oldPoint.Z = value;
                Axes.GraphMax = oldPoint;
            }
            get { return Axes.GraphMax.Z; }
        }

        private double _offsetX, _offsetY;

        public ZAxis3D(Axes3D axes, Axis3DCollection axisCollection)
            : base(axes, axisCollection)
        { }

        public ZAxis3D(Axes3D axes, Axis3DCollection axisCollection, ZAxisType axisType)
            : base(axes, axisCollection)
        {
            _axisType = axisType;
        }

        public override Point3D TickStartPoint(int i)
        {
            Point3D tickStartPoint3D;
            switch (_axisType)
            {
                case ZAxisType.MinusXMinusY:
                    tickStartPoint3D = new Point3D(Axes.GraphMin.X, Axes.GraphMin.Y, Ticks[i]);
                    break;
                case ZAxisType.MinusXPlusY:
                    tickStartPoint3D = new Point3D(Axes.GraphMin.X, Axes.GraphMax.Y, Ticks[i]);
                    break;
                case ZAxisType.PlusXMinusY:
                    tickStartPoint3D = new Point3D(Axes.GraphMax.X, Axes.GraphMin.Y, Ticks[i]);
                    break;
                case ZAxisType.PlusXPlusY:
                    tickStartPoint3D = new Point3D(Axes.GraphMax.X, Axes.GraphMax.Y, Ticks[i]);
                    break;
                default: 
                    tickStartPoint3D = new Point3D(Axes.GraphMin.X, Axes.GraphMin.Y, Ticks[i]);
                    break;
            }
            return tickStartPoint3D;
        }

        public override Point3D TickEndPoint(int i)
        {
            _offsetY = TickLength * (Axes.GraphMax.Y - Axes.GraphMin.Y);
            _offsetX = TickLength * (Axes.GraphMax.X - Axes.GraphMin.X);
            Point3D tickEndPoint3D;
            switch (_axisType)
            {
                case ZAxisType.MinusXMinusY:
                    tickEndPoint3D = new Point3D(Axes.GraphMin.X - _offsetX, Axes.GraphMin.Y, Ticks[i]);
                    break;
                case ZAxisType.MinusXPlusY:
                    tickEndPoint3D = new Point3D(Axes.GraphMin.X, Axes.GraphMax.Y + _offsetY, Ticks[i]);
                    break;
                case ZAxisType.PlusXMinusY:
                    tickEndPoint3D = new Point3D(Axes.GraphMax.X, Axes.GraphMin.Y - _offsetY, Ticks[i]);
                    break;
                case ZAxisType.PlusXPlusY:
                    tickEndPoint3D = new Point3D(Axes.GraphMax.X + _offsetX, Axes.GraphMax.Y, Ticks[i]);
                    break;
                default:
                    tickEndPoint3D = new Point3D(Axes.GraphMin.X, Axes.GraphMax.Y + _offsetY, Ticks[i]);
                    break;
            }
            return tickEndPoint3D;
        }

        public override void AxisProperties(ref Point3D start, ref Point3D end, ref Point3D centre, ref Point3D offsetCentre)
        {
            _offsetY = TickLength * (Axes.GraphMax.Y - Axes.GraphMin.Y);
            _offsetX = TickLength * (Axes.GraphMax.X - Axes.GraphMin.X);
            switch (_axisType)
            {
                case ZAxisType.MinusXMinusY:
                    start = new Point3D(Axes.GraphMin.X, Axes.GraphMin.Y, Min);
                    end = new Point3D(start.X, start.Y, Max);
                    centre = new Point3D(start.X, start.Y, (Max + Min) / 2.0);
                    offsetCentre = new Point3D(centre.X - _offsetX, centre.Y, centre.Z);
                    break;
                case ZAxisType.MinusXPlusY:
                    start = new Point3D(Axes.GraphMin.X, Axes.GraphMax.Y, Min);
                    end = new Point3D(start.X, start.Y, Max);
                    centre = new Point3D(start.X, start.Y, (Max + Min) / 2.0);
                    offsetCentre = new Point3D(centre.X, centre.Y + _offsetY, centre.Z);
                    break;
                case ZAxisType.PlusXMinusY:
                    start = new Point3D(Axes.GraphMax.X, Axes.GraphMin.Y, Min);
                    end = new Point3D(start.X, start.Y, Max);
                    centre = new Point3D(start.X, start.Y, (Max + Min) / 2.0);
                    offsetCentre = new Point3D(centre.X, centre.Y - _offsetY, centre.Z);
                    break;
                case ZAxisType.PlusXPlusY:
                    start = new Point3D(Axes.GraphMax.X, Axes.GraphMax.Y, Min);
                    end = new Point3D(start.X, start.Y, Max);
                    centre = new Point3D(start.X, start.Y, (Max + Min) / 2.0);
                    offsetCentre = new Point3D(centre.X + _offsetX, centre.Y, centre.Z);
                    break;
                default:
                    start = new Point3D(Axes.GraphMin.X, Axes.GraphMin.Y, Min);
                    end = new Point3D(start.X, start.Y, Max);
                    centre = new Point3D(start.X, start.Y, (Max + Min) / 2.0);
                    offsetCentre = new Point3D(centre.X - _offsetX, centre.Y, centre.Z);
                    break;
            }
        }
    }
}

