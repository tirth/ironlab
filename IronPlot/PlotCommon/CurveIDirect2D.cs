using System.Drawing;
using System.Windows.Media;
using SharpDX.Direct2D1;
using PathGeometry = SharpDX.Direct2D1.PathGeometry;

namespace IronPlot
{
    public partial class Curve
    {
        public PathGeometry ToDirect2DPathGeometry(Factory factory, MatrixTransform graphToCanvas)
        {
            var xScale = graphToCanvas.Matrix.M11;
            var xOffset = graphToCanvas.Matrix.OffsetX;
            var yScale = graphToCanvas.Matrix.M22;
            var yOffset = graphToCanvas.Matrix.OffsetY;

            var geometry = new PathGeometry(factory);

            using (var sink = geometry.Open())
            {

                var xCanvas = (float)(XTransformed[0] * xScale + xOffset);
                var yCanvas = (float)(YTransformed[0] * yScale + yOffset);
                var p0 = new PointF(xCanvas, yCanvas);

                sink.BeginFigure(p0, FigureBegin.Hollow);
                for (var i = 1; i < x.Length; ++i)
                {
                    if (!IncludeLinePoint[i]) continue;
                    xCanvas = (float)(XTransformed[i] * xScale + xOffset);
                    yCanvas = (float)(YTransformed[i] * yScale + yOffset);
                    sink.AddLine(new PointF(xCanvas, yCanvas));
                }
                sink.EndFigure(FigureEnd.Open);
                sink.Close();

            }
            return geometry;
        }
    }
}
