// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace IronPlot
{
    public class AxisPair
    {
        public XAxis XAxis;
        public YAxis YAxis;

        public AxisPair(XAxis xAxis, YAxis yAxis)
        {
            XAxis = xAxis; YAxis = yAxis;
        }
    }

    public class Axes2D : DependencyObject
    {
        public static readonly DependencyProperty AxisSpacingProperty =
            DependencyProperty.Register("AxisSpacing",
            typeof(double), typeof(Axes2D),
            new PropertyMetadata((double)5, UpdatePanel));

        public static readonly DependencyProperty MinAxisMarginProperty =
           DependencyProperty.Register("MinAxisMargin",
           typeof(Thickness), typeof(Axes2D),
           new PropertyMetadata(new Thickness(0), UpdatePanel));

        public static readonly DependencyProperty WidthProperty =
           DependencyProperty.Register("Width",
           typeof(double), typeof(Axes2D),
           new FrameworkPropertyMetadata(Double.NaN, UpdatePanel));

        public static readonly DependencyProperty HeightProperty =
           DependencyProperty.Register("Height",
           typeof(double), typeof(Axes2D),
           new PropertyMetadata(Double.NaN, UpdatePanel));

        public static readonly DependencyProperty EqualAxesProperty =
            DependencyProperty.Register("EqualAxes",
            typeof(AxisPair), typeof(Axes2D),
            new PropertyMetadata(null, UpdatePanel));

        public double AxisSpacing
        {
            set { SetValue(AxisSpacingProperty, value); }
            get { return (double)GetValue(AxisSpacingProperty); }
        }

        /// <summary>
        /// The minimum Thickness of the region in which axes are rendered. 
        /// </summary>
        public Thickness MinAxisMargin
        {
            set { SetValue(MinAxisMarginProperty, value); }
            get { return (Thickness)GetValue(MinAxisMarginProperty); }
        }

        /// <summary>
        /// The two axes (if any) which should be made to have an equal scale.
        /// </summary>
        public AxisPair EqualAxes
        {
            set { SetValue(EqualAxesProperty, value); }
            get { return (AxisPair)GetValue(EqualAxesProperty); }
        }

        public double Width
        {
            set { SetValue(WidthProperty, value); }
            get { return (double)GetValue(WidthProperty); }
        }
        
        public double Height
        {
            set { SetValue(HeightProperty, value); }
            get { return (double)GetValue(HeightProperty); }
        }

        protected static void UpdatePanel(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axes2D)obj)._plotPanel.InvalidateMeasure();
        }

        /// <summary>
        /// Set the innermost bottom X Axis and innermost left Y Axis to have equal scales.
        /// </summary>
        public void SetAxesEqual()
        {
            EqualAxes = new AxisPair(XAxes.Bottom, YAxes.Left);
        }

        internal double WidthForEqualAxes = Double.NaN;
        internal double HeightForEqualAxes = Double.NaN;

        /// <summary>
        /// Set no axes to have equal scales.
        /// </summary>
        public void ResetAxesEqual()
        {
            EqualAxes = null;
        }

        Size _maxCanvasSize = new Size(10000, 10000);
        readonly PlotPanel _plotPanel;
        
        internal XAxis XAxisBottom, XAxisTop; 
        internal YAxis YAxisLeft, YAxisRight;

        private XAxis _xAxis;
        private YAxis _yAxis;
        private readonly Axis2DCollection _xAxes;
        private readonly Axis2DCollection _yAxes;

        public XAxis2DCollection XAxes => _xAxes as XAxis2DCollection;
        public YAxis2DCollection YAxes => _yAxes as YAxis2DCollection;

        private readonly AxesFrame _frame;

        /// <summary>
        /// This is the basic box on which the axes are presented. 
        /// </summary>
        public Path Frame => _frame.Frame;

        public Axes2D(PlotPanel plotPanel)
        {
            _plotPanel = plotPanel;
            _frame = new AxesFrame();
            _frame.SetValue(Panel.ZIndexProperty, 300);
            // note that individual axes have index of 200
            plotPanel.Children.Add(_frame);

            XAxisBottom = new XAxis();
            XAxisBottom.SetValue(XAxis.XAxisPositionProperty, XAxisPosition.Bottom);
            XAxisTop = new XAxis();
            XAxisTop.SetValue(XAxis.XAxisPositionProperty, XAxisPosition.Top);
            //
            _xAxes = new XAxis2DCollection(plotPanel) {XAxisBottom, XAxisTop};
            _xAxis = XAxisBottom;
            XAxisTop.LabelsVisible = false;
            XAxisTop.TicksVisible = true;
            XAxisTop.GridLines.Visibility = Visibility.Collapsed;
            XAxisTop.BindToAxis(XAxisBottom);
            //
            YAxisLeft = new YAxis();
            YAxisLeft.SetValue(YAxis.YAxisPositionProperty, YAxisPosition.Left);
            YAxisRight = new YAxis();
            YAxisRight.SetValue(YAxis.YAxisPositionProperty, YAxisPosition.Right);
            //
            _yAxes = new YAxis2DCollection(plotPanel) {YAxisLeft, YAxisRight};
            _yAxis = YAxisLeft;
            YAxisRight.LabelsVisible = false;
            YAxisRight.TicksVisible = true;
            YAxisRight.GridLines.Visibility = Visibility.Collapsed;
            YAxisRight.BindToAxis(YAxisLeft);
            //
            UpdateTicks();        
        }

        internal void ArrangeEachAxisAndFrame(Rect axesRegionLocation)
        {
            foreach (var axis in XAxes) axis.Arrange(axesRegionLocation);
            foreach (var axis in YAxes) axis.Arrange(axesRegionLocation);
            _frame.Arrange(axesRegionLocation);
        }

        internal void RenderEachAxisAndFrame(Rect axesRegionLocation)
        {
            var allAxis = _xAxes.Concat(_yAxes);
            foreach (var axis in allAxis) axis.RenderAxis();
            _frame.Render(axesRegionLocation);
        }

        internal void UpdateTicks()
        {
            foreach (var axis in _xAxes) axis.DeriveTicks();
            foreach (var axis in _yAxes) axis.DeriveTicks();
        }

        /// <summary>
        /// Increase axis margins by amount necessary to ensure both that there is room
        /// for labels and that the necessary axes are aligned.
        /// </summary>
        /// <param name="alignedAxes">List of axes that need to be aligned.</param>
        private static void ExpandAxisMargins(List<Axis2D> alignedAxes, double plotLength)
        {
            // Calculate margins
            var margin = new Thickness1D(alignedAxes.Max(axis => axis.AxisPadding.Lower), alignedAxes.Max(axis => axis.AxisPadding.Upper));

            const double minPlotLength = 1.0;
            if (plotLength > minPlotLength)
            {
                var newTotalLength = plotLength + margin.Total();
                foreach (var axis in alignedAxes) axis.AxisTotalLength = newTotalLength;
            }
            else if ((alignedAxes[0].AxisTotalLength - margin.Total()) < minPlotLength)
            {
                foreach (var axis in alignedAxes) axis.AxisTotalLength = margin.Total() + minPlotLength;
            }

            // Set the margin and update Scale and Offset.
            foreach (var axis in alignedAxes) axis.ResetAxisMargin(margin);
            
            var tickIndex = 0;
            var maxTickIndex = alignedAxes.Max(axis => axis.Ticks.Length) / 2;
            var tickPair = new int[2];
            Axis2D limitingLowerAxis = null;
            var limitingLowerTickIndex = 0;
            var limitingLowerSemiWidth = margin.Lower;
            Axis2D limitingUpperAxis = null;
            var limitingUpperTickIndex = 0;
            var limitingUpperSemiWidth = margin.Upper;
            double offsetLower = 0;
            var deltaLower = alignedAxes[0].MaxTransformed - alignedAxes[0].MinTransformed;
            var deltaUpper = deltaLower;
            var axisTotalLength = alignedAxes[0].AxisTotalLength;
            double offsetUpper = 0;

            var nRescales = 0; // for diagnosic purposes only

            while ((tickIndex <= maxTickIndex) && (nRescales < 10))
            {
                var reset = false;
                // if a rescaling is required, start again from the beginning.
                for (var i = 0; i < alignedAxes.Count; ++i)
                {
                    var currentAxis = alignedAxes[i];
                    tickPair[0] = tickIndex;
                    tickPair[1] = currentAxis.TicksTransformed.Length - 1 - tickIndex;
                    if ((currentAxis.TicksTransformed.Length - 1 - tickIndex) < tickIndex) continue;
                    for (var j = 0; j <= 1; ++j)
                    {
                        var index = tickPair[j];
                        if (!currentAxis.LabelsVisible || currentAxis.TickLabelCache[index].Label.Text == "" || !currentAxis.TickLabelCache[index].IsShown) continue;
                        if ((currentAxis.Scale * currentAxis.TicksTransformed[index] - currentAxis.Offset - currentAxis.LimitingTickLabelSizeForLength(index) / 2) < -0.1)
                        {
                            // need to rescale axes
                            limitingLowerAxis = currentAxis;
                            limitingLowerTickIndex = index;
                            limitingLowerSemiWidth = currentAxis.LimitingTickLabelSizeForLength(index) / 2;
                            offsetLower = currentAxis.TicksTransformed[index] - currentAxis.MinTransformed;
                            deltaLower = currentAxis.MaxTransformed - currentAxis.MinTransformed;
                        }
                        else if ((currentAxis.Scale * currentAxis.TicksTransformed[index] - currentAxis.Offset + currentAxis.LimitingTickLabelSizeForLength(index) / 2) > (currentAxis.AxisTotalLength + 0.1))
                        {
                            // need to rescale axes
                            limitingUpperAxis = currentAxis;
                            limitingUpperTickIndex = index;
                            limitingUpperSemiWidth = currentAxis.LimitingTickLabelSizeForLength(index) / 2;
                            offsetUpper = currentAxis.MaxTransformed - currentAxis.TicksTransformed[index];
                            deltaUpper = currentAxis.MaxTransformed - currentAxis.MinTransformed;
                        }
                        else continue;
                        
                        // Reset required:
                        reset = true; nRescales++;
                        var offsetUpperPrime = offsetUpper * deltaLower / deltaUpper;
                        
                        // scale for lower-limiting axis
                        var newScale = (axisTotalLength - limitingLowerSemiWidth - limitingUpperSemiWidth) /
                            (deltaLower - offsetLower - offsetUpperPrime);
                        if (plotLength > minPlotLength)
                        {
                            // Axis is fixed to plotLength.
                            newScale = plotLength / deltaLower;
                            margin = new Thickness1D(limitingLowerSemiWidth - offsetLower * newScale, limitingUpperSemiWidth - offsetUpperPrime * newScale);
                            foreach (var axis in alignedAxes) axis.AxisTotalLength = plotLength + margin.Total();
                        }
                        if (newScale * deltaLower <= minPlotLength) 
                        {
                            // Axis is fixed to minPlotLength
                            newScale = minPlotLength / deltaLower;
                            margin = new Thickness1D(limitingLowerSemiWidth - offsetLower * newScale, limitingUpperSemiWidth - offsetUpperPrime * newScale);
                            foreach (var axis in alignedAxes) axis.AxisTotalLength = minPlotLength + margin.Total();
                        }
                        // otherwise, axis is unfixed.
                        margin = new Thickness1D(limitingLowerSemiWidth - offsetLower * newScale, limitingUpperSemiWidth - offsetUpperPrime * newScale);
                        foreach (var axis in alignedAxes) axis.RescaleAxis(newScale * deltaLower / (axis.MaxTransformed - axis.MinTransformed), margin);
                        break;
                    }
                    if (reset) break;
                }
                if (reset) tickIndex = 0;
                else tickIndex++;
            }
            if (nRescales == 10)
            {
                Console.WriteLine("Many rescales...");
            }
        }

        IEnumerable<Axis2D> _xAxesBottom; 
        IEnumerable<Axis2D> _xAxesTop; 
        IEnumerable<Axis2D> _yAxesLeft;
        IEnumerable<Axis2D> _yAxesRight;
        IEnumerable<Axis2D> _allAxes;

        private Thickness _margin;
        private Thickness _axisSpacings;

        internal void InitializeMargins()
        {
            foreach (var axis in _allAxes) axis.CalculateAxisThickness();
            var axisSpacing = AxisSpacing;
            _axisSpacings = new Thickness(Math.Max((_yAxesLeft.Count() - 1) * axisSpacing, 0),
                Math.Max((_xAxesTop.Count() - 1) * axisSpacing, 0), Math.Max((_yAxesRight.Count() - 1) * axisSpacing, 0), Math.Max((_xAxesBottom.Count() - 1) * axisSpacing, 0));

            var minAxisMargin = MinAxisMargin;

            _margin = new Thickness(
                Math.Max(_yAxesLeft.Sum(axis => axis.AxisThickness) + _axisSpacings.Left + _plotPanel.LegendRegion.Left, minAxisMargin.Left),
                Math.Max(_xAxesTop.Sum(axis => axis.AxisThickness) + _axisSpacings.Top + _plotPanel.LegendRegion.Top, minAxisMargin.Top),
                Math.Max(_yAxesRight.Sum(axis => axis.AxisThickness) + _axisSpacings.Right + _plotPanel.LegendRegion.Right, minAxisMargin.Right),
                Math.Max(_xAxesBottom.Sum(axis => axis.AxisThickness) + _axisSpacings.Bottom + _plotPanel.LegendRegion.Bottom, minAxisMargin.Bottom));

            ResetMarginsXAxes(_plotPanel.AvailableSize, _margin);
            ResetMarginsYAxes(_plotPanel.AvailableSize, _margin);
        }
        
        /// <summary>
        /// Given the available size for the plot area and axes, determine the
        /// required size and the position of the plot region within this region.
        /// Also sets the axes scales and positions labels.
        /// </summary>
        /// <param name="availableSize"></param>
        /// <param name="canvasPosition"></param>
        /// <param name="axesCanvasPositions"></param>
        internal void PlaceAxesFull()
        {
            // The arrangement process is
            // 1 - Calculate axes thicknesses
            // 2 - Expand axis margins if required, taking into account alignment requirements
            // 3 - Reduce axis if equal axes is demanded
            // 4 - Cull labels
            // 5 - Repeat 1 - 3 with culled labels
            var iter = 0;
            // Sort axes into top, bottom, right and left
            UpdateAxisPositions();
            while (iter <= 1)
            {
                // Step 1: calculate axis thicknesses (given any removed labels)
                InitializeMargins();

                // Step 2: expand axis margins if necessary,
                // taking account of any specified Width and/or Height
                ExpandAxisMargins(_xAxes.ToList(), Width);
                ExpandAxisMargins(_yAxes.ToList(), Height);
  
                // Step 3: take account of equal axes
                // This is only done on the second (and final iteration)
                if ((iter == 1) && (EqualAxes != null))
                {
                    HeightForEqualAxes = WidthForEqualAxes = Double.NaN;
                    if (EqualAxes.XAxis.Scale > EqualAxes.YAxis.Scale)
                    {
                        // XAxes will be reduced in size. First reset all margins to the minimum, then expand margins given the fixed scale.
                        WidthForEqualAxes = EqualAxes.YAxis.Scale * (EqualAxes.XAxis.MaxTransformed - EqualAxes.XAxis.MinTransformed); 
                    }
                    else
                    {
                        HeightForEqualAxes = EqualAxes.XAxis.Scale * (EqualAxes.YAxis.MaxTransformed - EqualAxes.YAxis.MinTransformed); 
                    }
                    if (!Double.IsNaN(WidthForEqualAxes))
                    {
                        ResetMarginsXAxes(_plotPanel.AvailableSize, _margin);
                        foreach (var axis in _xAxes) axis.SetToShowAllLabels(); 
                        ExpandAxisMargins(_xAxes.ToList(), WidthForEqualAxes);
                    }
                    if (!Double.IsNaN(WidthForEqualAxes))
                    {
                        ResetMarginsYAxes(_plotPanel.AvailableSize, _margin);
                        foreach (var axis in _yAxes) axis.SetToShowAllLabels(); 
                        ExpandAxisMargins(_yAxes.ToList(), WidthForEqualAxes);
                    }
                }

                PlaceEachAxis(AxisSpacing);
                // Step 4: cull labels
                var cullOverlapping = (iter == 0) || (EqualAxes != null && iter == 1);
                foreach (var axis in _allAxes) axis.PositionLabels(cullOverlapping);

                iter++;
            }
            foreach (var axis in _allAxes) axis.SetLabelVisibility(); 
            _plotPanel.AxesRegion = new Rect(0, 0, _xAxes[0].AxisTotalLength, _yAxes[0].AxisTotalLength);
            _plotPanel.CanvasLocation = new Rect(new Point(_xAxes[0].AxisPadding.Lower, _yAxes[0].AxisPadding.Upper),
                new Point(_plotPanel.AxesRegion.Width - _xAxes[0].AxisPadding.Upper, _plotPanel.AxesRegion.Height - _yAxes[0].AxisPadding.Lower));
        }

        internal void UpdateAxisPositions()
        {
            _xAxesBottom = _xAxes.Where(axis => (axis as XAxis).Position == XAxisPosition.Bottom);
            _xAxesTop = _xAxes.Where(axis => (axis as XAxis).Position == XAxisPosition.Top);
            _yAxesLeft = _yAxes.Where(axis => (axis as YAxis).Position == YAxisPosition.Left);
            _yAxesRight = _yAxes.Where(axis => (axis as YAxis).Position == YAxisPosition.Right);
            _allAxes = _xAxes.Concat(_yAxes);
            if (_xAxesBottom.First() != null) _xAxesBottom.First().IsInnermost = true;
            if (_xAxesTop.First() != null) _xAxesTop.First().IsInnermost = true;
            if (_yAxesLeft.First() != null) _yAxesLeft.First().IsInnermost = true;
            if (_yAxesRight.First() != null) _yAxesRight.First().IsInnermost = true;
        }

        internal void PlaceEachAxis(double axisSpacing)
        {
            var yPosition = _yAxes[0].AxisTotalLength - _yAxes[0].AxisPadding.Lower;
            foreach (XAxis xAxis in _xAxesBottom)
            {
                xAxis.YPosition = yPosition;
                yPosition += xAxis.AxisThickness + axisSpacing;
            }
            yPosition = _yAxes[0].AxisPadding.Upper;
            foreach (XAxis xAxis in _xAxesTop)
            {
                xAxis.YPosition = yPosition;
                yPosition -= xAxis.AxisThickness + axisSpacing;
            }
            var xPosition = _xAxes[0].AxisPadding.Lower;
            foreach (YAxis yAxis in _yAxesLeft)
            {
                yAxis.XPosition = xPosition;
                xPosition -= yAxis.AxisThickness + axisSpacing;
            }
            xPosition = _xAxes[0].AxisTotalLength - _xAxes[0].AxisPadding.Upper;
            foreach (YAxis yAxis in _yAxesRight)
            {
                yAxis.XPosition = xPosition;
                xPosition += yAxis.AxisThickness + axisSpacing;
            }
        }

        internal Thickness CalculateInitialAxesMargin()
        {
            return new Thickness();
        }

        private void ResetMarginsXAxes(Size availableSize, Thickness margin)
        {
            foreach (var axis in _xAxes)
            {
                axis.AxisTotalLength = Math.Min(availableSize.Width, _maxCanvasSize.Width + axis.AxisPadding.Total());
                axis.ResetAxisMargin(new Thickness1D(margin.Left, margin.Right));
            }
        }

        private void ResetMarginsYAxes(Size availableSize, Thickness margin)
        {
            foreach (var axis in _yAxes)
            {
                axis.AxisTotalLength = Math.Min(availableSize.Height, _maxCanvasSize.Height + axis.AxisPadding.Total());
                axis.ResetAxisMargin(new Thickness1D(margin.Bottom, margin.Top));
            }
        }

        internal void UpdateAxisPositionsOffsetOnly()
        {
            var allAxes = _xAxes.Concat(_yAxes);
            foreach (var axis in allAxes)
            {
                axis.UpdateOffset();
                axis.PositionLabels(true);
                axis.SetLabelVisibility(); 
            }
            _plotPanel.AxesRegion = new Rect(0, 0, XAxisBottom.AxisTotalLength, YAxisLeft.AxisTotalLength);
            _plotPanel.CanvasLocation = new Rect(XAxisBottom.AxisPadding.Lower, 
                 YAxisLeft.AxisPadding.Upper,
                XAxisBottom.AxisTotalLength - XAxisBottom.AxisPadding.Lower - XAxisBottom.AxisPadding.Upper,
                YAxisLeft.AxisTotalLength - YAxisLeft.AxisPadding.Lower - YAxisLeft.AxisPadding.Upper);
        }
    }
}
