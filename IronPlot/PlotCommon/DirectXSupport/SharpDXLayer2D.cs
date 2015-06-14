// Copyright (c) 2010 Joe Moorhouse

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using SharpDX;

namespace IronPlot.Plotting3D
{
    public class SharpDxLayer2D : I2DLayer
    {
        protected Canvas canvas;

        public Canvas Canvas
        {
            get { return canvas; }
            set { value = canvas; }
        }

        public SharpDxLayer2D(Canvas canvas, ViewportImage viewportImage, MatrixTransform3D modelToWorld)
        {
            this.canvas = canvas;
            ModelToWorld = modelToWorld;
            ViewportImage = viewportImage;
        }

        internal MatrixTransform3D ModelToWorld;

        internal ViewportImage ViewportImage;

        public Point CanvasPointFrom3DPoint(Point3D modelPoint3D)
        {
            var worldPoint = ViewportImage.ModelToWorld.Transform(modelPoint3D);
            var worldPointSharpDx = new Vector3((float)worldPoint.X, (float)worldPoint.Y, (float)worldPoint.Z);
            float width = ViewportImage.ImageWidth;
            float height = ViewportImage.ImageHeight;
            var trans = Matrix.Multiply(ViewportImage.View, ViewportImage.Projection);
            //Vector3 point2DSharpDX = Vector3.Project(worldPointSharpDX, 0, 0, ViewportImage.Width, ViewportImage.Height, 0.0f, 1.0f, trans);
            var point2DSharpDx = Vector3.Project(worldPointSharpDx, 0, 0, width, height, 0.0f, 1.0f, trans);
            return new Point(point2DSharpDx.X, point2DSharpDx.Y);
        }
    }
}
