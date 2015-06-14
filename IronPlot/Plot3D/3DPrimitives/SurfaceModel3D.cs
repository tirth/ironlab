// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using SharpDX;
using SharpDX.Direct3D9;
using Light = SharpDX.Direct3D9.Light;
using Material = SharpDX.Direct3D9.Material;

#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
using ILNumerics.Exceptions;
#endif

namespace IronPlot.Plotting3D
{
    public enum SurfaceShading { Faceted, Smooth, None };
    public enum MeshLines { None, Triangles }; // quads
    
    /// <summary>
    /// Geometric primitive class for surfaces from ILArrays.
    /// </summary>
    public partial class SurfaceModel3D : Model3D
    {
        VertexPositionNormalColor[] _vertices;
        Point3D[] _modelVertices;
        int _lengthU, _lengthV;
        int[] _indices;
        protected VertexDeclaration VertexDeclaration;
        protected VertexBuffer VertexBuffer;
        protected int VertexBufferLength = -1;
        protected IndexBuffer IndexBuffer;
        protected int IndexBufferLength = -1;
        protected ColourMap ColourMap;
        
        // This is only used if there is somewhere to display the bar (e.g. this is within a Plot3D) 
        protected ColourBar colourBar;
        public ColourBar ColourBar => colourBar;
        DispatcherTimer _colourMapUpdateTimer = new DispatcherTimer();

        protected UInt16[] ColourMapIndices;
        protected List<Light> lights;

        private static readonly DependencyProperty SurfaceShadingProperty =
            DependencyProperty.Register("SurfaceShading",
            typeof(SurfaceShading),
            typeof(SurfaceModel3D),
            new PropertyMetadata(SurfaceShading.Smooth, OnSurfaceShadingChanged));

        private static readonly DependencyProperty MeshLinesProperty =
            DependencyProperty.Register("MeshLines",
            typeof(MeshLines),
            typeof(SurfaceModel3D),
            new PropertyMetadata(MeshLines.None, OnMeshLinesChanged));

        private static readonly DependencyProperty TransparencyProperty =
            DependencyProperty.Register("Transparency",
            typeof(byte),
            typeof(SurfaceModel3D),
            new PropertyMetadata((byte)0, OnTransparencyChanged));

        public List<Light> Lights
        {
            get 
            {
                RequestRender(EventArgs.Empty);
                return lights; 
            }
        }

        Material _material;
        public Material Material { get { return _material; } set { _material = value; } }

        public SurfaceShading SurfaceShading
        {
            set { SetValue(SurfaceShadingProperty, value); }
            get { return (SurfaceShading)GetValue(SurfaceShadingProperty); }
        }

        public MeshLines MeshLines
        {
            set { SetValue(MeshLinesProperty, value); }
            get { return (MeshLines)GetValue(MeshLinesProperty); }
        }

        public byte Transparency
        {
            set { SetValue(TransparencyProperty, value); }
            get { return (byte)GetValue(TransparencyProperty); }
        }
        
        static void OnSurfaceShadingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var surface = obj as SurfaceModel3D;
            surface.CreateVertsAndInds();
            surface.SetColorFromIndices();
            surface.RecreateBuffers();
            surface.RequestRender(EventArgs.Empty);
        }

        static void OnMeshLinesChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var surface = obj as SurfaceModel3D;
            surface.RequestRender(EventArgs.Empty);
        }

        static void OnTransparencyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var surface = obj as SurfaceModel3D;
            surface.SetColorFromIndices();
            surface.RecreateBuffers();
            surface.RequestRender(EventArgs.Empty);
        }

        protected override void OnModelToWorldChanged()
        {
            TransformVertsAndInds();
            RecreateBuffers();
            base.OnModelToWorldChanged();
        }

        /// <summary>
        /// Constructs a surface primitive.
        /// </summary>
        public SurfaceModel3D(double[,] x, double[,] y, double[,] z)
        {
            InitializeSurface(x, y, z);
        }

        public SurfaceModel3D(double[] x, double[] y, double[,] z)
        {
            if (x.Length != z.GetLength(1)) throw new ArgumentException("Length of x vector must be equal to columns of z.");
            if (y.Length != z.GetLength(0)) throw new ArgumentException("Length of y vector must be equal to rows of z.");
            InitializeSurface(x, y, z);
        }

        public SurfaceModel3D(IEnumerable<double> x, IEnumerable<double> y, IEnumerable<double> z, int xLength, int yLength)
        {
            CreateMesh(x, y, z, xLength, yLength);
        }

        public SurfaceModel3D(IEnumerable<object> z)
        {
            int xLength, yLength;
            var za = GeneralArray.ToImageEnumerator(z, out xLength, out yLength);
            if (yLength == 0) throw new ArgumentException("z cannot be a vector.");
            var xa = MathHelper.MeshGridX(MathHelper.Counter(xLength), yLength);
            var ya = MathHelper.MeshGridY(MathHelper.Counter(yLength), xLength);
            CreateMesh(xa, ya, za, xLength, yLength);
        }

        public SurfaceModel3D(IEnumerable<object> x, IEnumerable<object> y, IEnumerable<object> z)
        {
            var xLengths = new int[3]; var yLengths = new int[3]; 
            var imageEnumerators = new IEnumerable<double>[3];
            var adjustedImageEnumerators = new IEnumerable<double>[3];

            imageEnumerators[0] = GeneralArray.ToImageEnumerator(x, out xLengths[0], out yLengths[0]);
            imageEnumerators[1] = GeneralArray.ToImageEnumerator(y, out xLengths[1], out yLengths[1]);
            imageEnumerators[2] = GeneralArray.ToImageEnumerator(z, out xLengths[2], out yLengths[2]);
            
            var xLength = -1; var yLength = -1;
            if (yLengths[0] == 0) xLength = xLengths[0];
            if (yLengths[1] == 0) yLength = xLengths[1];
            for (var i = 0; i < 3; ++i)
            {
                adjustedImageEnumerators[i] = imageEnumerators[i];
                if (yLengths[i] != 0)
                {
                    if (xLength == -1) xLength = xLengths[i];
                    else if (xLength != xLengths[i]) throw new ArgumentException("x dimensions are not consistent.");
                    if (yLength == -1) yLength = yLengths[i];
                    else if (yLength != yLengths[i]) throw new ArgumentException("y dimensions are not consistent.");
                }
            }
            if (yLengths[0] == 0) adjustedImageEnumerators[0] = MathHelper.MeshGridX(imageEnumerators[0], yLength);
            if (yLengths[1] == 0) adjustedImageEnumerators[1] = MathHelper.MeshGridY(imageEnumerators[1], xLength);
            if (yLengths[2] == 0 && xLengths[2] != xLength * yLength) throw new ArgumentException("Wrong number of elements in z.");
            
            CreateMesh(adjustedImageEnumerators[0], adjustedImageEnumerators[1], adjustedImageEnumerators[2], xLength, yLength);
        }

        protected void InitializeSurface(double[] x, double[] y, double[,] z)
        {
            CreateMesh(x, y, z);
        }

        protected void InitializeSurface(double[,] x, double[,] y, double[,] z)
        {
            CreateMesh(x, y, z);
        }
#if ILNumerics
        /// <summary>
        /// Constructs a surface primitive.
        /// </summary>
        public SurfaceModel3D(ILArray<double> x, ILArray<double> y, ILArray<double> z)
            : base()
        {
            InitializeSurfaceILArray(x, y, z);
        }
        
        public void InitializeSurfaceILArray(ILArray<double> x, ILArray<double> y, ILArray<double> z)
        {
            CreateMeshILArray(x, y, z);
        }

        protected void CreateMeshILArray(ILArray<double> x, ILArray<double> y, ILArray<double> z)
        {
            bounds = new Cuboid(x.MinValue, y.MinValue, z.MinValue, x.MaxValue, y.MaxValue, z.MaxValue);
            lengthU = x.Dimensions[0];
            lengthV = x.Dimensions[1];
            ILArray<double> xs, ys, zs;
            if (x.IsReference)
                xs = x.Clone() as ILArray<double>;
            else xs = x;
            if (y.IsReference)
                ys = y.Clone() as ILArray<double>;
            else ys = y;
            if (z.IsReference)
                zs = z.Clone() as ILArray<double>;
            else zs = z;
            //if (x.IsReference || y.IsReference || z.IsReference) throw new Exception("x, y and z must be solid arrays");
            double[] xa = xs.InternalArray4Experts;
            double[] ya = ys.InternalArray4Experts;
            double[] za = zs.InternalArray4Experts;
            Cuboid modelBounds = new Cuboid(new System.Windows.Media.Media3D.Point3D(-10, -10, -10), new System.Windows.Media.Media3D.Point3D(10, 10, 10));
            UpdateModelVertices(xa, ya, za, lengthU, lengthV);
            CreateVertsAndInds();
            colourMap = new ColourMap(ColourMapType.Jet, 256);
            colourMapIndices = FalseColourImage.IEnumerableToIndexArray(za, lengthU, lengthV, 256);
            SetColorFromIndices();
        } 
#endif
        protected void CreateMesh(double[] x, double[] y, double[,] z)
        {
            CreateMesh(MathHelper.MeshGridX(x, y.Length), MathHelper.MeshGridY(y, x.Length), z.ArrayEnumerator(EnumerationOrder2D.ColumnMajor), x.GetLength(0), x.GetLength(1));
        }

        protected void CreateMesh(double[,] x, double[,] y, double[,] z)
        {
            CreateMesh(x.ArrayEnumerator(EnumerationOrder2D.ColumnMajor), y.ArrayEnumerator(EnumerationOrder2D.ColumnMajor), z.ArrayEnumerator(EnumerationOrder2D.ColumnMajor), x.GetLength(0), x.GetLength(1));
        }

        protected void CreateMesh(IEnumerable<double> x, IEnumerable<double> y, IEnumerable<double> z, int xLength, int yLength)
        {
            _lengthU = xLength;
            _lengthV = yLength;
            bounds = new Cuboid(x.Min(), y.Min(), z.Min(), x.Max(), y.Max(), z.Max());
            var modelBounds = new Cuboid(new Point3D(-10, -10, -10), new Point3D(10, 10, 10));
            UpdateModelVertices(x, y, z, xLength, yLength);
            CreateVertsAndInds();
            ColourMap = new ColourMap(ColourMapType.Hsv, 256);
            ColourMapIndices = FalseColourImage.EnumerableToIndexArray(z, xLength, yLength, 256);
            SetColorFromIndices();
            _colourMapUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.2) };
            _colourMapUpdateTimer.Tick += colourMapUpdateTimer_Tick;

            lights = new List<Light>();
            var light = new Light { Type = LightType.Directional };
            light.Diffuse = new Color4(0.4f, 0.4f, 0.4f, 1.0f);
            light.Direction = new Vector3(0.3f, 0.3f, -0.7f);
            light.Specular = new Color4(0.05f, 0.05f, 0.05f, 1.0f);
            lights.Add(light);

            light = new Light { Type = LightType.Directional };
            light.Diffuse = new Color4(0.4f, 0.4f, 0.4f, 1.0f);
            light.Direction = new Vector3(-0.3f, -0.3f, -0.7f);
            light.Specular = new Color4(0.05f, 0.05f, 0.05f, 1.0f);
            lights.Add(light);

            _material = new Material();
            _material.Specular = new Color4(0.0f, 0.0f, 0.0f, 1.0f);
            _material.Diffuse = new Color4(0.0f, 0.0f, 0.0f, 1.0f);
            _material.Ambient = new Color4(0.0f, 0.0f, 0.0f, 1.0f);
            _material.Power = 10;
        }

        internal override void OnViewportImageChanged(ViewportImage newViewportImage)
        {
            // if the ViewportImage is owned by a Plot3D, we can add a ColourBar.
            if (ViewportImage != null && ViewportImage.ViewPort3D != null && colourBar != null)
            {
                ViewportImage.ViewPort3D.Annotations.Remove(colourBar);
                colourBar.ColourMapChanged -= colourBar_ColourMapChanged;
            }
            base.OnViewportImageChanged(newViewportImage);
            if (ViewportImage.ViewPort3D != null)
            {
                if (colourBar == null)
                {
                    colourBar = new ColourBar(ColourMap);
                    colourBar.Min = bounds.Minimum.Z; colourBar.Max = bounds.Maximum.Z; 
                    colourBar.ColourMapChanged += colourBar_ColourMapChanged;
                }
                ViewportImage.ViewPort3D.Annotations.Add(colourBar);
            }
        }
       
        void colourBar_ColourMapChanged(object sender, RoutedEventArgs e)
        {
            _colourMapUpdateTimer.Start();
        }

        bool _updateInProgress;
        readonly object _updateLocker = new object();

        void colourMapUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_updateInProgress)
            {
                _colourMapUpdateTimer.Start();
                return;
            }
            _updateInProgress = true;
            _colourMapUpdateTimer.Stop(); 
            ThreadPool.QueueUserWorkItem(UpdateColours, new object());
        }

        private void UpdateColours(object state)
        {
            lock (_updateLocker)
            {
                SetColorFromIndices();
                RecreateBuffers();
            }
            Dispatcher.BeginInvoke(new Action(delegate
            {
                RequestRender(EventArgs.Empty);
                _updateInProgress = false;
            }));
        }

        protected void CreateVertsAndInds()
        {
            if (SurfaceShading == SurfaceShading.Smooth)
            {
                var newVerticesLength = _lengthU * _lengthV * 2; // assume two-sided
                var newIndicesLength = 2 * 6 * (_lengthU - 1) * (_lengthV - 1);
                if (_vertices == null || (_vertices.Length != newVerticesLength)) _vertices = new VertexPositionNormalColor[newVerticesLength];
                if (_indices == null || (_indices.Length != newIndicesLength)) _indices = new int[newIndicesLength];
                UpdateVertsAndIndsSmooth(false, false);
            }
            else
            {
                var newVerticesLength = 6 * (_lengthU - 1) * (_lengthV - 1);
                var newIndicesLength = 2 * 6 * (_lengthU - 1) * (_lengthV - 1);
                if (_vertices == null || (_vertices.Length != newVerticesLength)) _vertices = new VertexPositionNormalColor[newVerticesLength];
                if (_indices == null || (_indices.Length != newIndicesLength)) _indices = new int[newIndicesLength];
                UpdateVertsAndIndsGeneral(false, false);
            }
        }

        protected void RecreateBuffers()
        {
            if (ViewportImage == null) return;
            lock (_updateLocker)
            {
                Pool pool;
                if (ViewportImage.GraphicsDeviceService.UseDeviceEx) pool = Pool.Default;
                else pool = Pool.Managed;
                if ((VertexBufferLength != _vertices.Length) || (VertexBuffer == null))
                {
                    if (VertexBuffer != null) VertexBuffer.Dispose();
                    VertexBuffer = new VertexBuffer(graphicsDevice, _vertices.Length * VertexPositionNormalColor.SizeInBytes,
                    Usage.WriteOnly, VertexFormat.Position | VertexFormat.Normal | VertexFormat.Diffuse, pool);
                    VertexBufferLength = _vertices.Length;
                }

                using (var stream = VertexBuffer.Lock(0, 0, LockFlags.None))
                {
                    stream.WriteRange(_vertices);
                    VertexBuffer.Unlock();
                }

                graphicsDevice.VertexFormat = VertexFormat.Position | VertexFormat.Normal | VertexFormat.Diffuse;

                if ((IndexBufferLength != _indices.Length) || (IndexBuffer == null))
                {
                    if (IndexBuffer != null) IndexBuffer.Dispose();
                    IndexBuffer = new IndexBuffer(graphicsDevice, _indices.Length * Marshal.SizeOf(typeof(int)),
                        Usage.WriteOnly, pool, false);
                    IndexBufferLength = _indices.Length;
                }

                using (var streamIndex = IndexBuffer.Lock(0, 0, LockFlags.None))
                {
                    streamIndex.WriteRange(_indices);
                    IndexBuffer.Unlock();
                }
            }
        }

        protected void TransformVertsAndInds()
        {
            if (SurfaceShading == SurfaceShading.Smooth)
            {
                UpdateVertsAndIndsSmooth(true, false);
            }
            else
            {
                UpdateVertsAndIndsGeneral(true, false);
            }
        }

        /// <summary>
        /// Store off the vertices in model space; these will then be transformed into world space.
        /// Overload to update model vertices with just changes x, y or z (useful for animation).
        /// </summary>
        protected void UpdateModelVertices(IEnumerable<double> x, IEnumerable<double> y, IEnumerable<double> z, int lengthU, int lengthV)
        {
            // Changing everything: just recreate array: 
            _modelVertices = new Point3D[lengthU * lengthV];
            var index = 0;
            IEnumerator<double> xi, yi, zi;
            xi = x.GetEnumerator(); yi = y.GetEnumerator(); zi = z.GetEnumerator(); 
            for (var v = 0; v < lengthV; v++)
            {
                for (var u = 0; u < lengthU; u++)
                {
                    xi.MoveNext(); yi.MoveNext(); zi.MoveNext(); 
                    _modelVertices[index] = new Point3D(xi.Current, yi.Current, zi.Current);
                    index++;
                }
            }
        }

        protected override void UpdateGeometry()
        {
        }

        /// <summary>
        /// Draws the primitive model, using the specified effect. Unlike the other
        /// Draw overload where you just specify the world/view/projection matrices
        /// and color, this method does not set any renderstates, so you must make
        /// sure all states are set to sensible values before you call it.
        /// </summary>
        public override void Draw()
        {
            base.Draw();

            if (VertexBuffer == null || IndexBuffer == null) return;

            graphicsDevice.SetRenderState(RenderState.SpecularEnable, true);

            graphicsDevice.Material = _material;
            graphicsDevice.SetRenderState(RenderState.Ambient, Color.DarkGray.ToArgb());
            graphicsDevice.SetRenderState(RenderState.SpecularEnable, true);

            graphicsDevice.SetRenderState(RenderState.ZEnable, ZBufferType.UseZBuffer);
            graphicsDevice.SetRenderState(RenderState.ZWriteEnable, true);
            graphicsDevice.SetRenderState(RenderState.ZFunc, Compare.LessEqual); 
            graphicsDevice.SetRenderState(RenderState.NormalizeNormals, true);

            for (var i = 0; i < lights.Count; ++i)
            {
                var light = lights[i];
                graphicsDevice.SetLight(i, ref light);
                graphicsDevice.EnableLight(i, true);
            }

            graphicsDevice.VertexFormat = VertexFormat.Position | VertexFormat.Normal | VertexFormat.Diffuse;
            graphicsDevice.SetStreamSource(0, VertexBuffer, 0, Marshal.SizeOf(typeof(VertexPositionNormalColor)));
            graphicsDevice.Indices = IndexBuffer;

            graphicsDevice.SetRenderState(RenderState.AlphaBlendEnable, true);
            graphicsDevice.SetRenderState(RenderState.BlendOperationAlpha, BlendOperation.Add);
            graphicsDevice.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
            graphicsDevice.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
            graphicsDevice.SetRenderState(RenderState.SeparateAlphaBlendEnable, false);
            graphicsDevice.SetRenderState(RenderState.CullMode, Cull.Counterclockwise);
            var primitiveCount = _indices.Length / 3;

            graphicsDevice.SetRenderState(RenderState.Lighting, true);

            if (MeshLines != MeshLines.None)
            {
                graphicsDevice.SetRenderState(RenderState.DepthBias, -0.0001f);
                graphicsDevice.SetRenderState(RenderState.AmbientMaterialSource, ColorSource.Material);
                graphicsDevice.SetRenderState(RenderState.DiffuseMaterialSource, ColorSource.Material);
                graphicsDevice.SetRenderState(RenderState.SpecularMaterialSource, ColorSource.Material);
                graphicsDevice.SetRenderState(RenderState.FillMode, FillMode.Wireframe);
                if (MeshLines == MeshLines.Triangles)
                {
                    graphicsDevice.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, _vertices.Length, 0, primitiveCount);
                }
                //else graphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineStrip, 0, 0, vertices.Length, 0, 1);
            }
            if (SurfaceShading != SurfaceShading.None)
            {
                graphicsDevice.SetRenderState(RenderState.DepthBias, 0);
                graphicsDevice.SetRenderState(RenderState.AmbientMaterialSource, ColorSource.Color1);
                graphicsDevice.SetRenderState(RenderState.DiffuseMaterialSource, ColorSource.Color1);
                graphicsDevice.SetRenderState(RenderState.SpecularMaterialSource, ColorSource.Color1);
                graphicsDevice.SetRenderState(RenderState.FillMode, FillMode.Solid);
                graphicsDevice.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, _vertices.Length, 0, primitiveCount);
            }
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
            RecreateBuffers();
        }
    }
}
