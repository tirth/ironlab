// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
#endif

namespace IronPlot
{    
    public class FalseColourImage : Plot2DItem
    {
        ColourBar _colourBar;
        readonly IEnumerable<double> _underlyingData;

        readonly int _width;
        readonly int _height;

        UInt16[] _indices;
        ColourMap _colourMap;
        int[] _updateColourMap;
        Path _imageRectangle;
        ImageBrush _imageBrush;
        WriteableBitmap _writeableBitmap;
        // The bounds of the rectangle in graph coordinates
        volatile bool _updateInProgress;
        DispatcherTimer _colourMapUpdateTimer;
        IntPtr _backBuffer;
        private delegate void AfterUpdateCallback();

        internal override void BeforeArrange()
        {
            // Ensure that transform is updated with the latest axes values. 
            _imageRectangle.RenderTransform = Axis2D.GraphToCanvasLinear(xAxis, yAxis); 
        }

        protected override void OnHostChanged(PlotPanel host)
        {
            base.OnHostChanged(host);
            if (this.host != null)
            {
                try
                {
                    host.Canvas.Children.Remove(Rectangle);
                    if (_colourBar != null) host.Annotations.Remove(_colourBar);
                }
                catch (Exception) 
                { 
                    // Just swallow any exception 
                }
            }
            this.host = host;
            if (_colourBar != null)
            {
                host.Annotations.Add(_colourBar);
                _colourBar.ColourMapChanged += OnColourMapChanged;
            }
            _colourMapUpdateTimer.Tick += OnColourMapUpdateTimerElapsed;
            host.Canvas.Children.Add(Rectangle);
        }
        
        // a FalseColourImage creates a UInt16[]
        // The UInt16[] contains indexed pixels that are mapped to colours 
        // via the colourMap
        readonly bool _useIlArray = false;
#if ILNumerics
        ILArray<double> underlyingILArrayData;
#endif


        public Path Rectangle => _imageRectangle;

        public ColourMap ColourMap
        {
            get { return _colourMap; }
            set { 
                _colourMap = value;
                _colourMapUpdateTimer.Start();    
            }
        }

        public static readonly DependencyProperty BoundsProperty =
            DependencyProperty.Register("Bounds",
            typeof(Rect), typeof(FalseColourImage),
            new PropertyMetadata(new Rect(0, 0, 10, 10),
                OnBoundsChanged));

        public Rect Bounds
        {
            set
            {
                SetValue(BoundsProperty, value);
            }
            get { return (Rect)GetValue(BoundsProperty); }
        }

        public override Rect TightBounds => (Rect)GetValue(BoundsProperty);

        public override Rect PaddedBounds => (Rect)GetValue(BoundsProperty);

        protected static void OnBoundsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var bounds = (Rect)e.NewValue;
            Geometry geometry = new RectangleGeometry(bounds);
            ((FalseColourImage)obj)._imageRectangle.Data = geometry;
        }

        public FalseColourImage(double[,] underlyingData)
        {
            _underlyingData = underlyingData.ArrayEnumerator(EnumerationOrder2D.RowMajor);
            _width = underlyingData.GetLength(0);
            _height = underlyingData.GetLength(1);
            Initialize(true);
        }

        public FalseColourImage(IEnumerable<object> underlyingData)
        {
            var array = GeneralArray.ToDoubleArray(underlyingData);
            _underlyingData = ((double[,])array).ArrayEnumerator(EnumerationOrder2D.ColumnMajor);
            _width = array.GetLength(0);
            _height = array.GetLength(1);
            Initialize(true);
        }

        internal FalseColourImage(Rect bounds, double[,] underlyingData, bool newColourBar)
        {
            _underlyingData = underlyingData.ArrayEnumerator(EnumerationOrder2D.ColumnMajor);
            _width = underlyingData.GetLength(0);
            _height = underlyingData.GetLength(1);
            Initialize(newColourBar);
            Bounds = bounds;
        }

        internal FalseColourImage(Rect bounds, IEnumerable<object> underlyingData, bool newColourBar)
        {
            var array = GeneralArray.ToDoubleArray(underlyingData);
            _underlyingData = ((double[,])array).ArrayEnumerator(EnumerationOrder2D.ColumnMajor);
            _width = array.GetLength(0);
            _height = array.GetLength(1);
            Initialize(newColourBar);
            Bounds = bounds;
        }
        
#if ILNumerics
        public FalseColourImage(ILArray<double> underlyingData)
        {
            this.underlyingILArrayData = underlyingData;
            width = underlyingData.Dimensions[0];
            height = underlyingData.Dimensions[1];
            useILArray = true;
            Initialize(true);
        }

        public FalseColourImage(Rect bounds, ILArray<double> underlyingData)
        {
            this.underlyingILArrayData = underlyingData;
            width = underlyingData.Dimensions[0];
            height = underlyingData.Dimensions[1];
            useILArray = true;
            Initialize(true);
            Bounds = bounds;
        }

        internal FalseColourImage(Rect bounds, ILArray<double> underlyingData, bool newColourBar)
        {
            this.underlyingILArrayData = underlyingData;
            width = underlyingData.Dimensions[0];
            height = underlyingData.Dimensions[1];
            useILArray = true;
            Initialize(newColourBar);
            Bounds = bounds;
        }

        public UInt16[] UnderlyingILArrayToIndexArray(int nIndices)
        {
            double max = underlyingILArrayData.MaxValue;
            double min = underlyingILArrayData.MinValue;
            double Scale = (nIndices - 1) / (max - min);
            int count = width * height;
            int index = 0;
            UInt16[] indices = new UInt16[count];
            foreach (double value in underlyingILArrayData)
            {
                indices[index] = (UInt16)((value - min) * Scale);
                index++;
            }
            return indices;
        }
#endif

        protected void Initialize(bool newColourBar)
        {
            _colourMapUpdateTimer = new DispatcherTimer();
            _colourMapUpdateTimer.Interval = TimeSpan.FromSeconds(0.2);
            _colourMap = new ColourMap(ColourMapType.Jet, 256);
            _imageRectangle = new Path();
            Geometry geometry = new RectangleGeometry(bounds);
            _imageRectangle.Data = geometry;
            RenderOptions.SetBitmapScalingMode(_imageRectangle, BitmapScalingMode.NearestNeighbor);
#if ILNumerics
            if (useILArray)
            {
                indices = UnderlyingILArrayToIndexArray(colourMap.Length);
            }
#endif
            if (!_useIlArray) _indices = UnderlyingToIndexArray(_colourMap.Length);
            _writeableBitmap = new WriteableBitmap(IndexArrayToBitmapSource());
            _imageBrush = new ImageBrush(_writeableBitmap);
            _imageRectangle.Fill = _imageBrush;
            Bounds = new Rect(0, 0, _writeableBitmap.PixelWidth, _writeableBitmap.PixelHeight);
            if (newColourBar)
            {
                _colourBar = new ColourBar(ColourMap);
#if ILNumerics
                if (useILArray)
                {
                    colourBar.Min = underlyingILArrayData.MinValue;
                    colourBar.Max = underlyingILArrayData.MaxValue;
                }
#endif
                if (!_useIlArray)
                {
                    _colourBar.Min = _underlyingData.Min();
                    _colourBar.Max = _underlyingData.Max();
                }
            }
        }

        public static UInt16[] EnumerableToIndexArray(IEnumerable<double> data, int width, int height, int nIndices)
        {
            var max = data.Max();
            var min = data.Min();
            var scale = (nIndices - 1) / (max - min);
            var count = width * height;
            var index = 0;
            var indices = new UInt16[count];
            foreach (var value in data)
            {
                indices[index] = (UInt16)((value - min) * scale);
                index++;
            }
            return indices;
        }

        public UInt16[] UnderlyingToIndexArray(int nIndices)
        {
            var max = _underlyingData.Max();
            var min = _underlyingData.Min();
            var scale = (nIndices - 1) / (max - min);
            var count = _width * _height; 
            var index = 0;
            var indices = new UInt16[count];
            foreach (var value in _underlyingData)
            {
                indices[index] = (UInt16)((value - min) * scale);
                index++;
            }
            return indices;
        }

#if ILNumerics 
        public static BitmapSource ILArrayToBitmapSource(ILArray<double> surface, ColourMap colourMap)
        {
            // Define parameters used to create the BitmapSource.
            PixelFormat pf = PixelFormats.Bgr32;
            int width = surface.Dimensions[0];
            int height = surface.Dimensions[1];
            int bytes = (pf.BitsPerPixel + 7) / 8;
            int rawStride = (width * bytes);
            byte[] rawImage = new byte[rawStride * height];
            int index = 0;
            byte[,] cmap = colourMap.ToByteArray();
            int colourMapLength = colourMap.Length;
            double min = surface.MinValue;
            double range = surface.MaxValue - min;
            int magnitude = 0;
            ILArray<int> scaled = (ILArray<int>)ILMath.convert(NumericType.Int32, ILMath.floor((surface - min) * (double)((colourMapLength - 1) / range)));
            ILIterator<int> iterator = scaled.CreateIterator();
            magnitude = iterator.Value;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    rawImage[index] = cmap[magnitude, 3];
                    rawImage[index + 1] = cmap[magnitude, 2];
                    rawImage[index + 2] = cmap[magnitude, 1];
                    rawImage[index + 3] = cmap[magnitude, 0];
                    index += bytes;
                    magnitude = iterator.Increment();
                }
            }
            // Create a BitmapSource.
            BitmapSource bitmap = BitmapSource.Create(width, height,
                96, 96, pf, null,
                rawImage, rawStride);

            return bitmap;
        }

        public static BitmapSource ILArrayToBitmapSourceReversed(ILArray<double> surface, ColourMap colourMap)
        {
            // Define parameters used to create the BitmapSource.
            PixelFormat pf = PixelFormats.Bgr32;
            int width = surface.Dimensions[0];
            int height = surface.Dimensions[1];
            int bytes = (pf.BitsPerPixel + 7) / 8;
            int rawStride = (width * bytes);
            byte[] rawImage = new byte[rawStride * height];
            int index = 0;
            byte[,] cmap = colourMap.ToByteArray();
            int colourMapLength = colourMap.Length;
            double range = surface.MaxValue - surface.MinValue;
            double min = surface.MinValue;
            int magnitude = 0;
            ILArray<int> scaled = (ILArray<int>)ILMath.convert(NumericType.Int32, ILMath.floor((surface - min) * (double)(colourMapLength - 1) / range));
            ILIterator<int> iterator = scaled.CreateIterator();
            magnitude = iterator.Value;
            for (int y = height - 1; y >= 0; --y)
            {
                index = y * rawStride; 
                for (int x = 0; x < width; ++x)
                {
                    rawImage[index] = cmap[magnitude, 3];
                    rawImage[index + 1] = cmap[magnitude, 2];
                    rawImage[index + 2] = cmap[magnitude, 1];
                    rawImage[index + 3] = cmap[magnitude, 0];
                    index += bytes;
                    magnitude = iterator.Increment();
                }
            }
            // Create a BitmapSource.
            BitmapSource bitmap = BitmapSource.Create(width, height,
                96, 96, pf, null,
                rawImage, rawStride);
            return bitmap;
        }
#endif  
       
        public void OnColourMapChanged(object sender, RoutedEventArgs e)
        {
            _colourMapUpdateTimer.Start();           
        }
        
        private void OnColourMapUpdateTimerElapsed(object sender, EventArgs e)
        {
            if (_updateInProgress)
            {
                _colourMapUpdateTimer.Start();
                return;
            }
            _updateColourMap = _colourMap.ToIntArray();
            _colourMapUpdateTimer.Stop();
            var state = new object();
            _writeableBitmap.Lock();
            _backBuffer = _writeableBitmap.BackBuffer;
            _updateInProgress = true;
            ThreadPool.QueueUserWorkItem(UpdateWriteableBitmap, state);
        }

        private BitmapSource IndexArrayToBitmapSource()
        {
            // Define parameters used to create the BitmapSource.
            var pf = PixelFormats.Bgr32;
            var bytes = (pf.BitsPerPixel + 7) / 8;
            var rawStride = (_width * bytes);
            var rawImage = new byte[rawStride * _height];
            var byteIndex = 0;
            var cmap = _colourMap.ToByteArray();
            foreach (var magnitude in _indices)
            {
                rawImage[byteIndex] = cmap[magnitude, 3];
                rawImage[byteIndex + 1] = cmap[magnitude, 2];
                rawImage[byteIndex + 2] = cmap[magnitude, 1];
                rawImage[byteIndex + 3] = cmap[magnitude, 0];
                byteIndex += bytes;
            }
            // Create a BitmapSource.
            var bitmap = BitmapSource.Create(_width, _height,
                96, 96, pf, null,
                rawImage, rawStride);

            return bitmap;
        }

        private void UpdateWriteableBitmap(Object state)
        {
            unsafe
            {
                var pBackBuffer = (int*)_backBuffer;
                var pf = PixelFormats.Bgr32;
                var bytes = (pf.BitsPerPixel + 7) / 8;
                for (var i = 0; i < _indices.Length; ++i)
                {
                    *pBackBuffer = _updateColourMap[_indices[i]];
                    pBackBuffer++;
                }
            }
            Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new AfterUpdateCallback(AfterUpdateWriteableBitmap));
        }

        private void AfterUpdateWriteableBitmap()
        {
            _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, _writeableBitmap.PixelWidth, _writeableBitmap.PixelHeight));
            _writeableBitmap.Unlock();
            _updateInProgress = false;
        }
    }
}
