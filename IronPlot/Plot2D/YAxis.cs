// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Windows.Media;

namespace IronPlot
{
    public enum YAxisPosition { Left, Right };

    public class YAxis : Axis2D
    {
        internal double XPosition = 0;

        public static readonly DependencyProperty YAxisPositionProperty =
            DependencyProperty.Register("YAxisPosition",
            typeof(YAxisPosition), typeof(YAxis),
            new PropertyMetadata(YAxisPosition.Left));

        public YAxisPosition Position { get { return (YAxisPosition)GetValue(YAxisPositionProperty); } set { SetValue(YAxisPositionProperty, value); } }

        public YAxis()
        {
            axisLabel.LayoutTransform = new RotateTransform(-90);
        }

        internal override void PositionLabels(bool cullOverlapping)
        {
            if (!LabelsVisible) return;

            int missOut = 0, missOutMax = 0;
            var lastTop = double.PositiveInfinity;
            var tickOffset = TicksVisible ? Math.Max(TickLength, 0.0) : 0;

            // Go through ticks in order of decreasing Canvas coordinate
            for (var i = 0; i < TicksTransformed.Length; ++i)
            {
                // Miss out labels if these would overlap.
                var currentTextBlock = TickLabelCache[i].Label;
                var verticalOffset = currentTextBlock.DesiredSize.Height - SingleLineHeight / 2;
                var currentTop = AxisTotalLength - (Scale * TicksTransformed[i] - Offset) - verticalOffset;
                currentTextBlock.Foreground = Brushes.DarkGray;  // TODO: expose property
                currentTextBlock.SetValue(System.Windows.Controls.Canvas.TopProperty, currentTop);

                if ((YAxisPosition)GetValue(YAxisPositionProperty) == YAxisPosition.Left)
                {
                    currentTextBlock.TextAlignment = TextAlignment.Right;
                    currentTextBlock.SetValue(System.Windows.Controls.Canvas.LeftProperty, XPosition - currentTextBlock.DesiredSize.Width - tickOffset - 3);
                }
                else
                {
                    currentTextBlock.TextAlignment = TextAlignment.Left;
                    currentTextBlock.SetValue(System.Windows.Controls.Canvas.LeftProperty, XPosition + tickOffset + 3);
                }

                if ((currentTop + currentTextBlock.DesiredSize.Height) > lastTop)
                    ++missOut;
                else
                {
                    lastTop = currentTop;
                    missOutMax = Math.Max(missOut, missOutMax);
                    missOut = 0;
                }
            }

            missOutMax = Math.Max(missOutMax, missOut);
            missOut = 0;

            if (cullOverlapping)
                for (var i = 0; i < TicksTransformed.Length; ++i)
                    if ((missOut < missOutMax) && (i > 0))
                    {
                        missOut += 1;
                        TickLabelCache[i].IsShown = false;
                    }
                    else missOut = 0;

            // Cycle through any now redundant TextBlocks and make invisible.
            for (var i = TicksTransformed.Length; i < TickLabelCache.Count; ++i)
            {
                TickLabelCache[i].IsShown = false;
            }

            // Finally, position axisLabel.
            if ((YAxisPosition)GetValue(YAxisPositionProperty) == YAxisPosition.Left)
                axisLabel.SetValue(System.Windows.Controls.Canvas.LeftProperty, XPosition - AxisThickness);
            else axisLabel.SetValue(System.Windows.Controls.Canvas.LeftProperty, XPosition + AxisThickness - axisLabel.DesiredSize.Width);

            var yPosition = AxisTotalLength - (Scale * 0.5 * (MaxTransformed + MinTransformed) - Offset) - axisLabel.DesiredSize.Height / 2.0;
            axisLabel.SetValue(System.Windows.Controls.Canvas.TopProperty, yPosition);
        }

        internal override double LimitingTickLabelSizeForLength(int index)
        {
            return TickLabelCache[index].Label.DesiredSize.Height;
        }

        protected override double LimitingTickLabelSizeForThickness(int index)
        {
            return TickLabelCache[index].Label.DesiredSize.Width + 3.0;
        }

        protected override double LimitingAxisLabelSizeForLength()
        {
            return axisLabel.DesiredSize.Height;
        }

        protected override double LimitingAxisLabelSizeForThickness()
        {
            return axisLabel.DesiredSize.Width;
        }

        internal override Point TickStartPosition(int i)
        {
            return new Point(XPosition, AxisTotalLength - TicksTransformed[i] * Scale + Offset);
        }

        internal override void RenderAxis()
        {
            var position = (YAxisPosition)GetValue(YAxisPositionProperty);

            var lineContext = AxisLineGeometry.Open();
            if (!IsInnermost)
            {
                var axisStart = new Point(XPosition, AxisTotalLength - MinTransformed * Scale + Offset - axisLine.StrokeThickness / 2);
                lineContext.BeginFigure(axisStart, false, false);
                lineContext.LineTo(new Point(XPosition, AxisTotalLength - MaxTransformed * Scale + Offset + axisLine.StrokeThickness / 2), true, false);
            }
            lineContext.Close();

            var context = AxisTicksGeometry.Open();
            if (TicksVisible)
            {
                for (var i = 0; i < Ticks.Length; ++i)
                {
                    Point tickPosition;
                    if (position == YAxisPosition.Left)
                    {
                        tickPosition = TickStartPosition(i);
                        context.BeginFigure(tickPosition, false, false);
                        tickPosition.X = tickPosition.X - TickLength;
                        context.LineTo(tickPosition, true, false);
                    }

                    if (position != YAxisPosition.Right) continue;

                    tickPosition = TickStartPosition(i);
                    context.BeginFigure(tickPosition, false, false);
                    tickPosition.X = tickPosition.X + TickLength;
                    context.LineTo(tickPosition, true, false);
                }
            }
            context.Close();
            
            interactionPad.Height = AxisTotalLength - AxisPadding.Total();
            interactionPad.Width = AxisThickness;
            if (position == YAxisPosition.Left) interactionPad.SetValue(System.Windows.Controls.Canvas.LeftProperty, XPosition - AxisThickness);
            else interactionPad.SetValue(System.Windows.Controls.Canvas.LeftProperty, XPosition);
            var yPosition = AxisTotalLength - MaxTransformed * Scale + Offset;
            interactionPad.SetValue(System.Windows.Controls.Canvas.TopProperty, yPosition);
            base.RenderAxis();
        }

        internal override Transform1D GraphToAxesCanvasTransform()
        {
            return new Transform1D(-Scale, -Offset - AxisTotalLength);
        }

        internal override Transform1D GraphToCanvasTransform()
        {
            return new Transform1D(-Scale, -Offset - AxisTotalLength - AxisPadding.Upper);
        }

        internal override double GraphToCanvas(double canvas)
        {
            return -GraphTransform(canvas) * Scale + Offset + AxisTotalLength - AxisPadding.Upper;
        }

        internal override double CanvasToGraph(double graph)
        {
            return CanvasTransform(-graph / Scale + (Offset + AxisTotalLength - AxisPadding.Upper) / Scale);
        }
    }
}
