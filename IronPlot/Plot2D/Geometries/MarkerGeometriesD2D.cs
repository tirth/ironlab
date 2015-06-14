using System.Drawing;
using SharpDX.Direct2D1;

namespace IronPlot
{
    public class MarkerGeometriesD2D
    {
        public static Geometry MarkerGeometry(MarkersType markersType, Factory factory, float width, float height)
        {
            Geometry geometry = null;
            switch (markersType)
            {
                case MarkersType.None:
                    break;
                case MarkersType.Square:
                    geometry = new RectangleGeometry(factory, new RectangleF
                    {
                        X = 0,
                        Y = 0,
                        Width = width,
                        Height = height
                    });
                    break;
                case MarkersType.Circle:
                    geometry = new EllipseGeometry(factory, new Ellipse
                    {
                        Point = new PointF(0, 0),
                        RadiusX = width / 2,
                        RadiusY = height / 2
                    });
                    break;
                default:
                    var markerSpecification = MarkerGeometries.GenericMarkerLookup[markersType];
                    geometry = new PathGeometry(factory);
                    using (var sink = (geometry as PathGeometry).Open())
                    {
                        var p0 = new PointF((float)markerSpecification.X[0] * width,  (float)markerSpecification.Y[0] * height); 
                        sink.BeginFigure(p0, FigureBegin.Hollow);
                        var n = markerSpecification.X.Length;
                        for (var i = 1; i < n; ++i)
                        {
                            sink.AddLine(new PointF((float)markerSpecification.X[i] * width, (float)markerSpecification.Y[i] * height)); 
                        }
                        sink.EndFigure(FigureEnd.Closed);
                        sink.Close();
                    }
                    break;
            }
            return geometry;
        }
    }
}
