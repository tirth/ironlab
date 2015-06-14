using System;
using System.Windows.Media;
using SharpDX;
using SharpDX.Direct2D1;
using Matrix = SharpDX.Matrix;

namespace IronPlot
{
    /// <summary>
    /// A Direct2D Path that is plotted multiple times at different locations.
    /// In other words, this is a scatter plot.
    /// </summary>
    public class DirectPathScatter : DirectPath
    {
        private Curve _curve;
        public Curve Curve { get { return _curve; } set { _curve = value; } }

        public double XOffsetMarker;
        public double YOffsetMarker;

        private MatrixTransform _graphToCanvas;
        public MatrixTransform GraphToCanvas { get { return _graphToCanvas; } set { _graphToCanvas = value; } }

        public void RenderScatterGeometry(RenderTarget renderTarget)
        {
            var x = _curve.X;
            var y = _curve.Y;
            var length = x.Length;
            double xScale, xOffset, yScale, yOffset;
            xScale = _graphToCanvas.Matrix.M11;
            xOffset = _graphToCanvas.Matrix.OffsetX - XOffsetMarker;
            yScale = _graphToCanvas.Matrix.M22;
            yOffset = _graphToCanvas.Matrix.OffsetY - YOffsetMarker;
            var include = _curve.IncludeMarker;
            var properties = new StrokeStyleProperties {LineJoin = LineJoin.MiterOrBevel};
            var strokeStyle = new StrokeStyle(renderTarget.Factory, properties);
            for (var i = 0; i < length; ++i)
            {
                if (!include[i]) continue;
                renderTarget.Transform = Matrix.Translation((float)(x[i] * xScale + xOffset), (float)(y[i] * yScale + yOffset), 0);
                renderTarget.FillGeometry(Geometry, FillBrush);
                renderTarget.DrawGeometry(Geometry, Brush, (float)StrokeThickness, strokeStyle);
            }
            renderTarget.Transform = Matrix3x2.Identity;
        }

        public void SetGeometry(MarkersType markersType, double markersSize)
        { 
            if (Geometry != null)
            {
                Geometry.Dispose();
                Geometry = null;
            }
            if (Factory == null) return;
            var width = (float)Math.Abs(markersSize);
            var height = (float)Math.Abs(markersSize);
            XOffsetMarker = 0; // width / 2;
            YOffsetMarker = 0; // height / 2;
            Geometry = MarkerGeometriesD2D.MarkerGeometry(markersType, Factory, width, height);
           
        }
    }
}
