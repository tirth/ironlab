using System;
using System.Windows;
using System.Windows.Media;
using SharpDX;
using SharpDX.Direct2D1;
using Brush = System.Windows.Media.Brush;
using Geometry = SharpDX.Direct2D1.Geometry;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace IronPlot
{
    // Has managed and non-managed part. The latter comprises:
    // Brush and Geometry
    // non-managed items are disposed whenever visibility is lost.
    /// <summary>
    /// Path implemented in Direct2D
    /// </summary>
    public class DirectPath : FrameworkElement, IDisposable
    {
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("Fill",
            typeof(Brush), typeof(DirectPath),
            new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke",
            typeof(Brush), typeof(DirectPath),
            new PropertyMetadata(Brushes.Black, OnStrokePropertyChanged));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness",
            typeof(double), typeof(DirectPath),
            new PropertyMetadata(1.0));

        public static readonly DependencyProperty QuickStrokeDashProperty =
            DependencyProperty.Register("QuickStrokeDash",
            typeof(QuickStrokeDash), typeof(DirectPath),
            new PropertyMetadata(QuickStrokeDash.Solid));

        public QuickStrokeDash QuickStrokeDash
        {
            set
            {
                SetValue(QuickStrokeDashProperty, value);
            }
            get { return (QuickStrokeDash)GetValue(QuickStrokeDashProperty); }
        }

        public Brush Fill
        {
            set
            {
                SetValue(FillProperty, value);
            }
            get { return (Brush)GetValue(FillProperty); }
        }

        public Brush Stroke
        {
            set
            {
                SetValue(StrokeProperty, value);
            }
            get { return (Brush)GetValue(StrokeProperty); }
        }

        public double StrokeThickness
        {
            set
            {
                SetValue(StrokeThicknessProperty, value);
            }
            get { return (double)GetValue(StrokeThicknessProperty); }
        }

        private static void OnStrokePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var localDirectPath = (DirectPath)obj;
            localDirectPath.SetBrush(((SolidColorBrush)e.NewValue).Color);
        }

        DirectImage _directImage;
        
        Geometry _geometry;
        internal Geometry Geometry
        { 
            get { return _geometry; }
            set 
            { 
                if (_geometry != null) _geometry.Dispose(); 
                _geometry = value; 
            }
        }

        SharpDX.Direct2D1.Brush _brush;
        internal SharpDX.Direct2D1.Brush Brush => _brush;

        SharpDX.Direct2D1.Brush _fillBrush;
        internal SharpDX.Direct2D1.Brush FillBrush => _fillBrush;

        public Factory Factory
        {
            get
            {
                if (_directImage != null) return _directImage.RenderTarget.Factory;
                return null;
            }
        }

        public DirectImage DirectImage
        {
            get { return _directImage; }
            set
            {
                _directImage = value;
            }
        }

        private void SetBrush(Color colour)
        {
            if (_brush != null) _brush.Dispose();
            if (_directImage != null) _brush = new SharpDX.Direct2D1.SolidColorBrush(_directImage.RenderTarget, new Color4(colour.ScR, colour.ScG, colour.ScB, colour.ScA));
        }

        private void SetFillBrush(Color colour)
        {
            if (_fillBrush != null) _fillBrush.Dispose();
            if (_directImage != null) _fillBrush = new SharpDX.Direct2D1.SolidColorBrush(_directImage.RenderTarget, new Color4(colour.ScR, colour.ScG, colour.ScB, colour.ScA));
        }

        public DirectPath()
        {
            _geometry = null;
            _directImage = null;
        }

        internal void DisposeDisposables()
        {
            if (_brush != null)
            {
                _brush.Dispose();
                _brush = null;
            }
            if (_fillBrush != null)
            {
                _fillBrush.Dispose();
                _fillBrush = null;
            }
            if (_geometry != null)
            {
                _geometry.Dispose();
                _geometry = null;
            }
        }

        internal void RecreateDisposables()
        {
            DisposeDisposables();
            SetBrush(((SolidColorBrush)GetValue(StrokeProperty)).Color);
            SetFillBrush(((SolidColorBrush)GetValue(FillProperty)).Color);
            // Geometry will be re-created when the containing Direct2DControl is next Arranged.
        }

        public void Dispose()
        {
            DisposeDisposables();
        }
    }
}
