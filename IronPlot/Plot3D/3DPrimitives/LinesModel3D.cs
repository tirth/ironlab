// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using SharpDX;
using SharpDX.Direct3D9;
using Colors = System.Windows.Media.Colors;

namespace IronPlot.Plotting3D
{
    /// <summary>
    /// Custom vertex type for vertices that have a
    /// position, normal and colour.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ThickLinesVertex
    {
        public Vector3 StartPosition;
        public Vector3 EndPosition;
        public Vector2 Texture;
        public int Color;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ThickLinesVertex(Vector3 startPosition, Vector3 endPosition, Vector2 texture, int color)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            Texture = texture;
            Color = color;
        }

        /// <summary>
        /// Size of this vertex type.
        /// </summary>
        public const int SizeInBytes = 12 + 12 + 8 + 4;
    }

    public class LinesModel3D : Model3D, IResolutionDependent
    {
        // Fields associated with rendering of lines
        private VertexPositionColor[] _vertices;
        private ThickLinesVertex[] _thickVertices;
        private VertexDeclaration _vertexDeclaration;
        //Vector3[] verticesVectors;
        private short[] _indices;
        protected VertexBuffer VertexBuffer;
        protected IndexBuffer IndexBuffer;
        private static Effect _effect;
        private bool _pointsChanged = true;
        // Denotes where single pixel lines or thick lines should be drawn
        private bool _thickLines = true;
        private int _dpi = 96;

        bool _effectUnavailable;

        /// <summary>
        /// Update geometry from point collection (rather than have event on collection itself, this must
        /// be called explicitly).
        /// </summary>
        public void UpdateFromPoints()
        {
            _pointsChanged = true;
            GeometryChanged = true;
        }

        private static readonly DependencyProperty PointCollectionProperty =
            DependencyProperty.Register("PointCollection",
            typeof(List<Point3DColor>),
            typeof(LinesModel3D),
            new PropertyMetadata(null));

        private static readonly DependencyProperty LineThicknessProperty =
            DependencyProperty.Register("LineThickness",
            typeof(double),
            typeof(LinesModel3D),
            new PropertyMetadata(1.5, LineThicknessChanged));

        internal float DepthBias = 0f;

        public List<Point3DColor> Points
        {
            private set { SetValue(PointCollectionProperty, value); }
            get { return (List<Point3DColor>)GetValue(PointCollectionProperty); }
        }

        static void LineThicknessChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var lines = obj as LinesModel3D;
            var previousThickLines = lines._thickLines;
            var thickLines = true;
            if (lines._effectUnavailable || (((double)args.NewValue == 1.0) && (lines._dpi == 96))) thickLines = false;
            if (previousThickLines != thickLines)
            {
                lines._pointsChanged = true;
                lines.GeometryChanged = true;
                lines._thickVertices = null;
                lines._indices = null;
                lines._vertices = null;
            }
            lines._thickLines = thickLines;
            lines.ViewportImage.RequestRender();
        }

        public double LineThickness
        {
            set { SetValue(LineThicknessProperty, value); }
            get { return (double)GetValue(LineThicknessProperty); }
        }

        public LinesModel3D()
        {
            Points = new List<Point3DColor>();
            _pointsChanged = true;
        }

        internal override void OnViewportImageChanged(ViewportImage newViewportImage)
        {
            var oldViewportImage = ViewportImage;
            if (oldViewportImage != null)
            {
                ViewportImage.GraphicsDeviceService.DeviceReset -= GraphicsDeviceService_DeviceReset;
                ViewportImage.GraphicsDeviceService.DeviceResetting -= GraphicsDeviceService_DeviceResetting;
            }
            base.OnViewportImageChanged(newViewportImage);
            if (!ViewportImage.GraphicsDeviceService.IsAntialiased) LineThickness = 1.0;
            ViewportImage.GraphicsDeviceService.DeviceReset += GraphicsDeviceService_DeviceReset;
            ViewportImage.GraphicsDeviceService.DeviceResetting += GraphicsDeviceService_DeviceResetting;
            TryCreateEffects();
            UpdateGeometry();
        }

        private void TryCreateEffects()
        {
            if (!_effectUnavailable && _effect == null)
            {
                try
                {
                    var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("IronPlot.Plot3D._3DPrimitives.Line.fxo");
                    _effect = Effect.FromStream(graphicsDevice, stream, ShaderFlags.None);             
                }
                catch (Exception) 
                {
                    _effectUnavailable = true;
                    _thickLines = false;
                }
            }
        }

        protected override void UpdateGeometry()
        {
            if (Points.Count == 0) return;
            if (_pointsChanged)
            {
                if (IsVisible) RecreateBuffers();
                _pointsChanged = false;
            }
            short vertexIndex = 0;
            short index = 0;
            if (!_thickLines)
            {
                Point3D modelPoint;
                foreach (var p in Points)
                {
                    modelPoint = ModelToWorld.Transform(p.Point3D);
                    _vertices[index] = new VertexPositionColor(new Vector3((float)modelPoint.X, (float)modelPoint.Y, (float)modelPoint.Z), Point3DColor.ColorToInt(p.Color));
                    _indices[index] = index;
                    index++;
                }
            }
            else
            {
                Point3D start, end;
                int color;
                for (var i = 0; i < Points.Count - 1; i += 2)
                {
                    start = ModelToWorld.Transform(Points[i].Point3D);
                    end = ModelToWorld.Transform(Points[i + 1].Point3D);
                    color = Point3DColor.ColorToInt(Points[i].Color);
                    _thickVertices[vertexIndex] = new ThickLinesVertex(new Vector3((float)start.X, (float)start.Y, (float)start.Z),
                        new Vector3((float)end.X, (float)end.Y, (float)end.Z),
                        new Vector2(0, -0.5f), color);
                    _thickVertices[vertexIndex + 1] = new ThickLinesVertex(new Vector3((float)start.X, (float)start.Y, (float)start.Z),
                        new Vector3((float)end.X, (float)end.Y, (float)end.Z),
                        new Vector2(1, -0.5f), color);
                    _thickVertices[vertexIndex + 2] = new ThickLinesVertex(new Vector3((float)start.X, (float)start.Y, (float)start.Z),
                        new Vector3((float)end.X, (float)end.Y, (float)end.Z),
                        new Vector2(1, 0.5f), color);
                    _thickVertices[vertexIndex + 3] = new ThickLinesVertex(new Vector3((float)start.X, (float)start.Y, (float)start.Z),
                        new Vector3((float)end.X, (float)end.Y, (float)end.Z),
                        new Vector2(0, 0.5f), color);
                    _indices[index] = vertexIndex; _indices[index + 1] = (short)(vertexIndex + 1); _indices[index + 2] = (short)(vertexIndex + 2);
                    _indices[index + 3] = vertexIndex; _indices[index + 4] = (short)(vertexIndex + 2); _indices[index + 5] = (short)(vertexIndex + 3);
                    vertexIndex += 4;
                    index += 6;
                }
            }
            if (IsVisible) FillBuffers();
        }

        protected void RecreateBuffers()
        {
            Pool pool;
            if (ViewportImage.GraphicsDeviceService.UseDeviceEx) pool = Pool.Default;
            else pool = Pool.Managed;
            if (!_thickLines)
            {
                // Prepare for using single-pixel lines
                if ((_vertices == null) || (_vertices.Length != Points.Count) || (VertexBuffer == null))
                {
                    if (VertexBuffer != null) VertexBuffer.Dispose();
                    if ((_vertices == null) || (_vertices.Length != Points.Count)) _vertices = new VertexPositionColor[Points.Count];
                    VertexBuffer = new VertexBuffer(graphicsDevice, _vertices.Length * VertexPositionColor.SizeInBytes,
                        Usage.WriteOnly, VertexFormat.Position | VertexFormat.Diffuse, pool);
                }
                if ((_indices == null) || (_indices.Length != Points.Count) || (IndexBuffer == null))
                {
                    if (IndexBuffer != null) IndexBuffer.Dispose();
                    if ((_indices == null) || (_indices.Length != Points.Count)) _indices = new short[Points.Count];
                    IndexBuffer = new IndexBuffer(graphicsDevice, _indices.Length * Marshal.SizeOf(typeof(short)),
                        Usage.WriteOnly, pool, true);
                }
            }
            else
            {
                // Prepare for thick lines. 4 vertices and 6 indices per line.
                if ((_thickVertices == null) || (_thickVertices.Length != Points.Count * 4) || (VertexBuffer == null))
                {
                    if (VertexBuffer != null) VertexBuffer.Dispose();
                    if ((_thickVertices == null) || (_thickVertices.Length != Points.Count * 4)) _thickVertices = new ThickLinesVertex[Points.Count * 4];
                    VertexBuffer = new VertexBuffer(graphicsDevice, _thickVertices.Length * ThickLinesVertex.SizeInBytes,
                        Usage.WriteOnly, VertexFormat.Position | VertexFormat.Texture0 | VertexFormat.Texture1 | VertexFormat.Diffuse, pool);
                }
                if ((_indices == null) || (_indices.Length != Points.Count * 6) || (IndexBuffer == null))
                {
                    if (IndexBuffer != null) IndexBuffer.Dispose();
                    if ((_indices == null) || (_indices.Length != Points.Count * 6)) _indices = new short[6 * Points.Count];
                    IndexBuffer = new IndexBuffer(graphicsDevice, _indices.Length * Marshal.SizeOf(typeof(short)),
                        Usage.WriteOnly, pool, true);
                }
            }
        }

        protected void FillBuffers()
        {
            DataStream stream, streamIndex;
            if (!_thickLines)
            {
                stream = VertexBuffer.Lock(0, 0, LockFlags.None);
                stream.WriteRange(_vertices);
                VertexBuffer.Unlock();
                graphicsDevice.VertexFormat = VertexFormat.Position | VertexFormat.Diffuse;
            }
            else
            {
                stream = VertexBuffer.Lock(0, 0, LockFlags.None);
                stream.WriteRange(_thickVertices);
                VertexBuffer.Unlock();
                VertexElement[] velements = {
                     new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
                     new VertexElement(0, 12, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                     new VertexElement(0, 24, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 1),
                     new VertexElement(0, 32, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 0),
                     VertexElement.VertexDeclarationEnd
                };
                _vertexDeclaration = new VertexDeclaration(graphicsDevice, velements);
            }
            streamIndex = IndexBuffer.Lock(0, 0, LockFlags.None);
            streamIndex.WriteRange(_indices);
            IndexBuffer.Unlock();
        }

        /// <summary>
        /// </summary>
        public override void Draw()
        {
            base.Draw();

            if (VertexBuffer == null || IndexBuffer == null) return;

            graphicsDevice.SetRenderState(RenderState.MultisampleAntialias, true);
            graphicsDevice.SetRenderState(RenderState.FillMode, FillMode.Solid);
            graphicsDevice.SetRenderState(RenderState.DepthBias, DepthBias);
            int primitiveCount;
            if (!_thickLines)
            {
                graphicsDevice.SetRenderState(RenderState.Lighting, false);
                graphicsDevice.VertexFormat = VertexFormat.Position | VertexFormat.Diffuse;
                graphicsDevice.SetStreamSource(0, VertexBuffer, 0, Marshal.SizeOf(typeof(VertexPositionColor)));
                graphicsDevice.Indices = IndexBuffer;
                primitiveCount = _indices.Length / 2;
                graphicsDevice.DrawIndexedPrimitive(PrimitiveType.LineList, 0, 0, _vertices.Length, 0, primitiveCount);
            }
            else
            {
                graphicsDevice.VertexDeclaration = _vertexDeclaration;
                graphicsDevice.SetStreamSource(0, VertexBuffer, 0, Marshal.SizeOf(typeof(ThickLinesVertex)));
                graphicsDevice.Indices = IndexBuffer;
                _effect.Technique = "Simplest";
                _effect.SetValue("XPixels", (float)ViewportImage.Width);
                _effect.SetValue("YPixels", (float)ViewportImage.Height);
                _effect.SetValue("LineWidth", (float)LineThickness * _dpi / 96.0f);
                _effect.SetValue("ViewProjection", ViewportImage.View * ViewportImage.Projection);
                primitiveCount = _indices.Length / 6;
                var numpasses = _effect.Begin(0);
                for (var i = 0; i < numpasses; i++)
                {
                    _effect.BeginPass(i);
                    graphicsDevice.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, _thickVertices.Length, 0, primitiveCount);
                    _effect.EndPass();
                }
                _effect.End();
            }
        }

        protected void GraphicsDeviceService_DeviceResetting(object sender, EventArgs e)
        {
            if (_effect != null)
            {
                _effect.Dispose();
                _effect = null;
            }
        }

        protected void GraphicsDeviceService_DeviceReset(object sender, EventArgs e)
        {
            TryCreateEffects();
        }

        public void SetResolution(int dpi)
        {
            if (dpi != _dpi)
            {
                _dpi = dpi;
                _pointsChanged = true;
                GeometryChanged = true;
                _thickVertices = null;
                _indices = null;
                _vertices = null;
            }
            if (_effectUnavailable || ((LineThickness == 1.0) && (dpi == 96))) _thickLines = false;
            else _thickLines = true;
        }

        protected override void DisposeDisposables()
        {
            base.DisposeDisposables();
            if (VertexBuffer != null) VertexBuffer.Dispose();
            if (IndexBuffer != null) IndexBuffer.Dispose();
            VertexBuffer = null; IndexBuffer = null;
        }

        protected override void RecreateDisposables()
        {
            base.RecreateDisposables();
            if (Points.Count != 0)
            {
                RecreateBuffers();
                FillBuffers();
            }
        }
    }

    /// <summary>
    /// Custom vertex type for vertices that have a
    /// position, normal and colour.
    /// </summary>
    public struct Point3DColor
    {
        public Point3D Point3D;
        public Color Color;

        public double X
        {
            get { return Point3D.X; }
            set { Point3D.X = value; }
        }

        public double Y
        {
            get { return Point3D.X; }
            set { Point3D.X = value; }
        }

        public double Z
        {
            get { return Point3D.X; }
            set { Point3D.X = value; }
        }

        public Point3DColor(Point3D point3D, Color color)
        {
            Point3D = point3D;
            Color = color;
        }

        public Point3DColor(Point3D point3D)
        {
            Point3D = point3D;
            Color = Colors.Black;
        }

        public Point3DColor(double x, double y, double z, Color color)
        {
            Point3D = new Point3D(x, y, z);
            Color = color;
        }

        public Point3DColor(double x, double y, double z)
        {
            Point3D = new Point3D(x, y, z);
            Color = Colors.Black;
        }

        public static int ColorToInt(Color color)
        {
            return (255 << 24)      // A 
                | (color.R << 16)    // R
                | (color.G << 8)    // G
                | (color.B << 0);   // B
        }
    }

}
