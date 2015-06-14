// Copyright (c) 2010 Joe Moorhouse

using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IronPlot
{
    public class GridLines : Shape
    {
        // Grid lines geometry and context
        private readonly StreamGeometry _gridLinesGeometry;
        private StreamGeometryContext _gridLinesGeometryContext;

        private readonly Axis2D _axis;

        public GridLines(Axis2D axis)
        {
            _axis = axis;
            _gridLinesGeometry = new StreamGeometry();
            Stroke = Brushes.LightGray;
            StrokeThickness = 1;
            StrokeLineJoin = PenLineJoin.Miter;
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                _gridLinesGeometryContext = _gridLinesGeometry.Open();
                foreach (var t in _axis.Ticks)
                {
                    Point tickStart, tickEnd;
                    if (_axis is XAxis)
                    {
                        tickStart = new Point(_axis.GraphToCanvas(t), 0);
                        tickEnd = new Point(_axis.GraphToCanvas(t), _axis.PlotPanel.Canvas.ActualHeight);
                    }
                    else
                    {
                        tickStart = new Point(0, _axis.GraphToCanvas(t));
                        tickEnd = new Point(_axis.PlotPanel.Canvas.ActualWidth, _axis.GraphToCanvas(t));
                    }
                    _gridLinesGeometryContext.BeginFigure(tickStart, false, false);
                    _gridLinesGeometryContext.LineTo(tickEnd, true, false);
                }
                _gridLinesGeometryContext.Close();
                return _gridLinesGeometry;
            }
        }
    }
}
