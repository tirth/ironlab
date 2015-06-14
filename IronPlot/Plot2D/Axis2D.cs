// Copyright (c) 2010 Joe Moorhouse

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IronPlot
{
    /// <summary>
    /// Specifies the space at either end of an axis required to accommodate
    /// annotation. Max is the high end of the axis in Canvas coordinates.
    /// </summary>
    public struct Thickness1D
    {
        public double Lower;
        public double Upper;

        public double Total()
        {
            return Lower + Upper;
        }

        public Thickness1D(double lower, double upper)
        {
            Lower = lower;
            Upper = upper;
        }
    }

    public struct Transform1D
    {
        public double Scale;
        public double Offset;

        public Transform1D(double scale, double offset)
        {
            Scale = scale; Offset = offset;
        }

        public double Transform(double input) { return Scale * input - Offset; }

        public double InverseTransform(double input) { return (input + Offset) / Scale; }

        public Transform1D Inverse() { return new Transform1D(1 / Scale, -Offset / Scale); }
    }

    /// <summary>
    /// An Axis2D is an Axis that contains TextBlock annotation and the axis lines (a Shape).
    /// The ticks can appear both sides of the plot.
    /// </summary>
    public abstract class Axis2D : Axis
    {
        public static readonly DependencyProperty RangeProperty =
            DependencyProperty.Register("Range",
            typeof(Range), typeof(Axis2D),
            new PropertyMetadata(new Range(0, 10), OnRangeChanged));

        public override double Min
        {
            set {
                var newRange = (Range)GetValue(RangeProperty);
                newRange.Min = value;
                SetValue(RangeProperty, newRange); 
            }
            get { return ((Range)GetValue(RangeProperty)).Min; }
        }

        public override double Max
        {
            set
            {
                var newRange = (Range)GetValue(RangeProperty);
                newRange.Max = value;
                SetValue(RangeProperty, newRange);
            }
            get { return ((Range)GetValue(RangeProperty)).Max; }
        }

        internal LabelCache TickLabelCache = new LabelCache();

        protected Path axisLine = new Path { Stroke = Brushes.Black };
        /// <summary>
        /// Path representing the axis line.
        /// </summary>
        public Path AxisLine => axisLine;

        protected StreamGeometry AxisLineGeometry = new StreamGeometry();

        protected Label axisLabel = new Label();
        public Label AxisLabel => axisLabel;

        protected Path axisTicks = new Path { Stroke = Brushes.Black };
        /// <summary>
        /// Path representing the axis ticks.
        /// </summary>
        public Path AxisTicks => axisTicks;

        protected StreamGeometry AxisTicksGeometry = new StreamGeometry();

        protected Rectangle interactionPad = new Rectangle();
        public Rectangle InteractionPad => interactionPad;

        protected GridLines gridLines;
        public GridLines GridLines => gridLines;

        // PlotPanel object to which the axis belongs.
        internal PlotPanel PlotPanel;

        // Whether this is one of the innermost axes, or an additional axis.
        internal bool IsInnermost = false;
        internal double AxisThickness;
        
        // The padding region is the extra space needed, in addition to the area between max and min,
        // to accommodate labels
        internal Thickness1D AxisPadding;

        // the margin region is extra space outside the axis itself
        //internal Thickness1D AxisMargin;

        internal double Scale;
        internal double Offset;
        
        /// <summary>
        /// The axis length including labels.
        /// </summary>
        internal double AxisTotalLength; 
        // canvasCoord = transformedGraphCoord * Scale - Offset

        // Assume that any change to the axis (number and length of ticks, change to labels) requires
        // another layout pass of the PlotPanel.
        // In addition, certain changes require re-derivation of ticks and labels:
        // change of tick number, change of max and min
        
        // Height of a one-line label
        protected double SingleLineHeight;

        protected static void OnRangeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var axis2DLocal = ((Axis2D)obj);
            var desiredRange = (Range)e.NewValue;
            if (Double.IsNegativeInfinity(desiredRange.Min) || Double.IsNaN(desiredRange.Min)
                || Double.IsPositiveInfinity(desiredRange.Max) || Double.IsNaN(desiredRange.Max))
            {
                axis2DLocal.SetValue(RangeProperty, e.OldValue);
            }
            if (axis2DLocal.AxisType == AxisType.Log)
            {
                if (desiredRange.Min <= 0 || desiredRange.Max <= 0)
                    axis2DLocal.SetValue(RangeProperty, e.OldValue);  
            }
            else if (axis2DLocal.AxisType == AxisType.Date)
            {
               if (desiredRange.Min < MinDate || desiredRange.Max >= MaxDate)
                   axis2DLocal.SetValue(RangeProperty, e.OldValue); 
            }
            var length = Math.Abs(desiredRange.Length);
            if ((Math.Abs(desiredRange.Min) / length > 1e10) || (Math.Abs(desiredRange.Max) / length > 1e10)) axis2DLocal.SetValue(RangeProperty, e.OldValue);    
            axis2DLocal.DeriveTicks();
            axis2DLocal.PlotPanel?.InvalidateMeasure();
        }

        Binding _axisBinding;
        /// <summary>
        /// Bind the Max and Min of this axis to another axis.
        /// </summary>
        /// <param name="bindingAxis"></param>
        public void BindToAxis(Axis2D bindingAxis)
        {
            _axisBinding = new Binding("Range") { Source = this, Mode = BindingMode.TwoWay };
            bindingAxis.SetBinding(RangeProperty, _axisBinding);
        }

        protected static void OnTicksPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axis2D)obj).DeriveTicks();
            ((Axis2D)obj).UpdateTicksAndLabels();
        }

        protected override void OnAxisTypeChanged()
        {
            base.OnAxisTypeChanged();
            foreach (var item in PlotPanel.PlotItems)
            {
                if (item.XAxis == this || item.YAxis == this) item.OnAxisTypeChanged();
            }
        }

        protected override void UpdateTicksAndLabels()
        {
            PlotPanel?.InvalidateMeasure();
        }

        protected Axis2D()
        {
            MinTransformed = GraphTransform(Min); MaxTransformed = GraphTransform(Max);
            Background = null;
            axisLabel.Visibility = Visibility.Collapsed;
            gridLines = new GridLines(this);
            Canvas.Children.Add(axisLine); axisLine.SetValue(Panel.ZIndexProperty, 100);
            Canvas.Children.Add(axisTicks); axisTicks.SetValue(Panel.ZIndexProperty, 100);
            Canvas.Children.Add(axisLabel); axisLabel.SetValue(Panel.ZIndexProperty, 100);
            Canvas.Children.Add(interactionPad); interactionPad.SetValue(Panel.ZIndexProperty, 50);
            Brush padFill = new SolidColorBrush { Color = Brushes.Aquamarine.Color, Opacity = 0.0 };
            interactionPad.Fill = padFill;
            axisLine.Data = AxisLineGeometry;
            axisTicks.Data = AxisTicksGeometry;
            DeriveTicks();

            var fontSizeDescr = DependencyPropertyDescriptor.
                FromProperty(FontSizeProperty, typeof(Axis2D));

            fontSizeDescr?.AddValueChanged(this, delegate
            {
                TickLabelCache.Invalidate();
            });
        }
        
        static Axis2D()
        {
            LabelsVisibleProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(true, OnLabelsVisibleChanged));
            TickLengthProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(5.0, OnTicksPropertyChanged));
            TicksVisibleProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(true, OnTicksPropertyChanged));
            NumberOfTicksProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(10, OnTicksPropertyChanged));
            FormatOverrideProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(null, OnTicksPropertyChanged));
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var size = base.MeasureOverride(constraint);
            // The parent of an Axis2D is always a PlotPanel. We need to trigger a Measure pass
            // in the parent.
            return size;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var finalSize = base.ArrangeOverride(arrangeSize);
            return finalSize;
        }

        internal static void OnLabelsVisibleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == (bool)e.OldValue) return;
            var axis = ((Axis2D)obj);
            if ((bool)e.NewValue == false)
            {
                foreach (var item in axis.TickLabelCache) axis.Canvas.Children.Remove(item.Label);
                axis.TickLabelCache.Clear();
            }
        }

        internal virtual void PositionLabels(bool cullOverlapping)
        {
            // Do nothing in base Version: no labels.
        }

        internal void SetToShowAllLabels()
        {
            foreach (var t in TickLabelCache)
                t.IsShown = true;
        }

        internal void SetLabelVisibility()
        {
            foreach (var t in TickLabelCache)
                if (!t.IsShown)
                {
                    if (t.Label.Visibility != Visibility.Hidden) t.Label.Visibility = Visibility.Hidden;
                }
                else
                {
                    if (t.Label.Visibility != Visibility.Visible) t.Label.Visibility = Visibility.Visible;
                }
        }

        internal void UpdateAndMeasureLabels()
        {
            // Make sure the labels are up to date
            if (!LabelsVisible) return;
            var isFirstNewItem = true;

            for (var i = 0; i < Ticks.Length; ++i)
            {
                // Reuse the text blocks wherever possible (we do not want to keep adding and taking away TextBlocks
                // from the canvas)
                TextBlock currentTextBlock;
                if (i > (TickLabelCache.Count - 1))
                {
                    var newItem = new LabelCacheItem();
                    currentTextBlock = newItem.Label;
                    currentTextBlock.SetValue(Panel.ZIndexProperty, 100);
                    currentTextBlock.TextAlignment = TextAlignment.Center;
                    Canvas.Children.Add(currentTextBlock);
                    TickLabelCache.Add(newItem);
                }
                else currentTextBlock = TickLabelCache[i].Label;
                UpdateLabelText(i);
                if (TickLabelCache[i].TextRequiresChange(LabelText[i].Key))
                {
                    if (isFirstNewItem)
                    {
                        isFirstNewItem = false;
                        currentTextBlock.Text = "0123456789";
                        currentTextBlock.Measure(new Size(Double.PositiveInfinity, double.PositiveInfinity));
                        SingleLineHeight = currentTextBlock.DesiredSize.Height;
                    }       
                    AddTextToBlock(currentTextBlock, i);
                    currentTextBlock.Visibility = Visibility.Visible;
                    TickLabelCache[i].CacheKey = LabelText[i].Key;
                }
                currentTextBlock.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            }
            axisLabel.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            SetToShowAllLabels();
        }

        // Calculate thickness of axis (size in direction penpendicular to axis vector).
        internal double CalculateAxisThickness()
        {
            double maxThickness = 0;
            for (var i = 0; i < Ticks.Length; ++i)
            {
                if (LabelsVisible && TickLabelCache[i].IsShown) maxThickness = Math.Max(maxThickness, LimitingTickLabelSizeForThickness(i));
            }
            var tickLength = TicksVisible ? Math.Max(TickLength, 0) : 0.0;
            AxisThickness = maxThickness + tickLength + LimitingAxisLabelSizeForThickness();
            return AxisThickness;
        }

        internal virtual double LimitingTickLabelSizeForLength(int index)
        {
            return TickLabelCache[index].Label.DesiredSize.Width;
        }

        protected virtual double LimitingTickLabelSizeForThickness(int index)
        {
            return TickLabelCache[index].Label.DesiredSize.Height;
        }

        protected virtual double LimitingAxisLabelSizeForLength()
        {
            return axisLabel.DesiredSize.Width;
        }

        protected virtual double LimitingAxisLabelSizeForThickness()
        {
            return axisLabel.DesiredSize.Height;
        }

        internal void OverrideAxisScaling(double scale, double offset, Thickness1D axisPadding)
        {
            Scale = scale;
            Offset = offset;
            AxisPadding = axisPadding;
        }

        /// <summary>
        /// Keep margins the same, but reduce axis length.
        /// </summary>
        /// <param name="newScale"></param>
        internal void RescaleAxis(double newScale)
        {
            var axisLength = newScale * (MaxTransformed - MinTransformed);
            Scale = newScale;
            Offset = Scale * MinTransformed - AxisPadding.Lower;
            AxisTotalLength = axisLength + AxisPadding.Total();
        }

        /// <summary>
        /// Change margins and scale, keeping total length constant.
        /// </summary>
        /// <param name="newScale"></param>
        internal void RescaleAxis(double newScale, Thickness1D newMargin)
        {
            AxisPadding = newMargin;
            Scale = newScale;
            Offset = Scale * MinTransformed - AxisPadding.Lower;
        }

        /// <summary>
        /// Reset AxisMargin, keeping TotalLength the same.
        /// </summary>
        /// <param name="newScale"></param>
        internal void ResetAxisMargin(Thickness1D newMargin)
        {
            AxisPadding = newMargin;
            var axisLength = AxisTotalLength - newMargin.Total();
            Scale = axisLength / (MaxTransformed - MinTransformed);
            Offset = Scale * MinTransformed - AxisPadding.Lower;
        }

        // The axis Scale has been reduced to make the axes equal.
        // The axis is reduced, keeping the axis minimum point in the same position.
        internal void ScaleAxis(double newScale, double maxCanvas)
        {
            var axisLength = newScale * (MaxTransformed - MinTransformed);
            Scale = newScale;
            Offset = Scale * MinTransformed - AxisPadding.Lower;
            AxisTotalLength = axisLength + AxisPadding.Lower + AxisPadding.Upper;
        }

        internal virtual Point TickStartPosition(int i)
        {
            return new Point();
        }

        // Updates Offset from current Scale and min position.
        // This is for dragging interations where only Offset changes.
        internal void UpdateOffset()
        {
            Offset = Scale * MinTransformed - AxisPadding.Lower;
        }

        internal virtual void RenderAxis()
        {
            // Derived classes should render axes. Also cause GridLines to be re-rendered.
            gridLines.InvalidateVisual();
        }

        internal abstract Transform1D GraphToAxesCanvasTransform();
        internal abstract Transform1D GraphToCanvasTransform();

        internal abstract double GraphToCanvas(double canvas);
        internal abstract double CanvasToGraph(double graph);

        public static MatrixTransform GraphToCanvasLinear(XAxis xAxis, YAxis yAxis)
        {
            return new MatrixTransform(xAxis.Scale, 0, 0, -yAxis.Scale, -xAxis.Offset - xAxis.AxisPadding.Lower, yAxis.Offset + yAxis.AxisTotalLength - yAxis.AxisPadding.Upper);
        }
    }
}
