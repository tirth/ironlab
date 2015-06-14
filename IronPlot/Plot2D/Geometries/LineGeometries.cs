using System.Windows;
using System.Windows.Media;

namespace IronPlot
{
    public static class LineGeometries
    {
        public static StreamGeometry StreamGeometryFromCurve(Curve curve, MatrixTransform graphToCanvas)
        {
            double[] tempX;
            double[] tempY;
            if (graphToCanvas != null)
            {
                tempX = curve.XTransformed.MultiplyBy(graphToCanvas.Matrix.M11).SumWith(graphToCanvas.Matrix.OffsetX);
                tempY = curve.YTransformed.MultiplyBy(graphToCanvas.Matrix.M22).SumWith(graphToCanvas.Matrix.OffsetY);
            }
            else
            {
                tempX = curve.XTransformed; tempY = curve.YTransformed;
            }
            var streamGeometry = new StreamGeometry();
            var context = streamGeometry.Open();
            var lines = 0;
            for (var i = 0; i < curve.x.Length; ++i)
            {
                if (i == 0)
                {
                    context.BeginFigure(new Point(tempX[i], tempY[i]), false, false);
                }
                else
                {
                    if (curve.IncludeLinePoint[i])
                    {
                        context.LineTo(new Point(tempX[i], tempY[i]), true, false);
                        lines++;
                    }
                }
            }
            context.Close();
            return streamGeometry;
        }

        public static PathGeometry PathGeometryFromCurve(Curve curve, MatrixTransform graphToCanvas)
        {
            double xScale, xOffset, yScale, yOffset;
            if (graphToCanvas != null)
            {
                xScale = graphToCanvas.Matrix.M11;
                xOffset = graphToCanvas.Matrix.OffsetX;
                yScale = graphToCanvas.Matrix.M22;
                yOffset = graphToCanvas.Matrix.OffsetY;
            }
            else
            {
                xScale = 1; xOffset = 0;
                yScale = 1; yOffset = 0;
            }

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure();
            LineSegment lineSegment;
            var xCanvas = curve.XTransformed[0] * xScale + xOffset;
            var yCanvas = curve.YTransformed[0] * yScale + yOffset;
            pathFigure.StartPoint = new Point(xCanvas, yCanvas);
            for (var i = 1; i < curve.x.Length; ++i)
            {
                if (curve.IncludeLinePoint[i])
                {
                    lineSegment = new LineSegment();
                    xCanvas = curve.XTransformed[i] * xScale + xOffset;
                    yCanvas = curve.YTransformed[i] * yScale + yOffset;
                    lineSegment.Point = new Point(xCanvas, yCanvas);
                    pathFigure.Segments.Add(lineSegment);
                }
            }
            pathFigure.IsClosed = false;
            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }
    }
}
