// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IronPlot
{   
    public class Plot2DCurve : Plot2DItem
    {
        //VisualLine visualLine;
        protected MatrixTransform GraphToCanvas = new MatrixTransform(Matrix.Identity);
        protected MatrixTransform CanvasToGraph = new MatrixTransform(Matrix.Identity);
        
        // WPF elements:
        private PlotPath _line;
        private PlotPath _markers;
        private PlotPath _legendLine;
        private PlotPath _legendMarker;
        //private MarkersVisualHost markersVisual = new MarkersVisualHost();
        private LegendItem _legendItem;

        // An annotation marker for displaying coordinates of a position.
        PlotPointAnnotation _annotation; 
        
        // Direct2D elements:
        private DirectPath _lineD2D;
        private DirectPathScatter _markersD2D;

        #region DependencyProperties
        public static readonly DependencyProperty MarkersTypeProperty =
            DependencyProperty.Register("MarkersType",
            typeof(MarkersType), typeof(Plot2DCurve),
            new PropertyMetadata(MarkersType.None,
                OnMarkersChanged));

        public static readonly DependencyProperty MarkersSizeProperty =
            DependencyProperty.Register("MarkersSize",
            typeof(double), typeof(Plot2DCurve),
            new PropertyMetadata(10.0,
                OnMarkersChanged));

        public static readonly DependencyProperty MarkersFillProperty =
            DependencyProperty.Register("MarkersFill",
            typeof(Brush), typeof(Plot2DCurve),
            new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke",
            typeof(Brush), typeof(Plot2DCurve),
            new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness",
            typeof(double), typeof(Plot2DCurve),
            new PropertyMetadata(1.0));

        public static readonly DependencyProperty QuickLineProperty =
            DependencyProperty.Register("QuickLine",
            typeof(string), typeof(Plot2DCurve),
            new PropertyMetadata("-k",
            OnQuickLinePropertyChanged));

        public static readonly DependencyProperty QuickStrokeDashProperty =
            DependencyProperty.Register("QuickStrokeDash",
            typeof(QuickStrokeDash), typeof(Plot2DCurve),
            new PropertyMetadata(QuickStrokeDash.Solid,
            OnQuickStrokeDashPropertyChanged));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title",
            typeof(string), typeof(Plot2DCurve),
            new PropertyMetadata(String.Empty));

        public static readonly DependencyProperty AnnotationPositionProperty =
            DependencyProperty.Register("AnnotationPosition",
            typeof(Point), typeof(Plot2DCurve),
            new PropertyMetadata(new Point(Double.NaN, Double.NaN),
                OnAnnotationPositionChanged));

        public static readonly DependencyProperty AnnotationEnabledProperty =
            DependencyProperty.Register("AnnotationEnabled",
            typeof(bool), typeof(Plot2DCurve),
            new PropertyMetadata(true,
                OnAnnotationEnabledChanged));

        public static readonly DependencyProperty UseDirect2DProperty =
            DependencyProperty.Register("UseDirect2D",
            typeof(bool), typeof(Plot2DCurve),
            new PropertyMetadata(false, OnUseDirect2DChanged));

        /// <summary>
        /// Desired annotation mapping goes here.
        /// </summary>
        public Func<Point, string> AnnotationFromPoint = (point => point.ToString());
        //public Func<Point, string> AnnotationFromPoint = (point => String.Format("{0:F},{0:F}", point.X, point.Y));

        /// <summary>
        /// Get or set line in <line><markers><colour> notation, e.g. --sr for dashed red line with square markers
        /// </summary>
        public string QuickLine
        {
            set
            {
                SetValue(QuickLineProperty, value);
            }
            get
            {
                return GetLinePropertyFromStrokeProperties();
            }
        }

        public QuickStrokeDash QuickStrokeDash
        {
            set
            {
                SetValue(QuickStrokeDashProperty, value);
            }
            get { return (QuickStrokeDash)GetValue(QuickStrokeDashProperty); }
        }

        public MarkersType MarkersType
        {
            set
            {
                SetValue(MarkersTypeProperty, value);
            }
            get { return (MarkersType)GetValue(MarkersTypeProperty); }
        }

        public double MarkersSize
        {
            set
            {
                SetValue(MarkersSizeProperty, value);
            }
            get { return (double)GetValue(MarkersSizeProperty); }
        }

        public Brush MarkersFill
        {
            set
            {
                SetValue(MarkersFillProperty, value);
            }
            get { return (Brush)GetValue(MarkersFillProperty); }
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

        public string Title
        {
            set
            {
                SetValue(TitleProperty, value);
            }
            get { return (string)GetValue(TitleProperty); }
        }

        public Point AnnotationPosition
        {
            set
            {
                SetValue(AnnotationPositionProperty, value);
            }
            get { return (Point)GetValue(AnnotationPositionProperty); }
        }

        public bool AnnotationEnabled
        {
            set
            {
                SetValue(AnnotationEnabledProperty, value);
            }
            get { return (bool)GetValue(AnnotationEnabledProperty); }
        }

        protected static void OnMarkersChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Plot2DCurve)obj).UpdateLegendMarkers();
            if (((Plot2DCurve)obj).Plot != null)
                ((Plot2DCurve)obj).Plot.PlotPanel.InvalidateArrange();
        }

        protected static void OnQuickLinePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var lineProperty = (string)(e.NewValue);
            ((Plot2DCurve)obj).SetStrokePropertiesFromLineProperty(lineProperty);
        }

        protected static void OnQuickStrokeDashPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Plot2DCurve)obj)._line.QuickStrokeDash = (QuickStrokeDash)(e.NewValue);
        }

        protected static void OnAnnotationPositionChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var canvasPosition = (Point)e.NewValue;
            var localCurve = (Plot2DCurve)obj;
            if (Double.IsNaN(canvasPosition.X) || !localCurve.AnnotationEnabled)
            {
                localCurve._annotation.Visibility = Visibility.Collapsed;
                return;
            }
            localCurve._annotation.Visibility = Visibility.Visible;
            int index;
            var curveCanvas = localCurve.SnappedCanvasPoint(canvasPosition, out index);
            localCurve._annotation.Annotation = localCurve.AnnotationFromPoint(new Point(localCurve._curve.x[index], localCurve._curve.y[index]));
            localCurve._annotation.SetValue(Canvas.LeftProperty, curveCanvas.X);
            localCurve._annotation.SetValue(Canvas.TopProperty, curveCanvas.Y);
            localCurve._annotation.InvalidateVisual();
        }

        protected static void OnAnnotationEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var localCurve = (Plot2DCurve)obj;
            if ((bool)e.NewValue == false) localCurve._annotation.Visibility = Visibility.Collapsed; 
        }

        #endregion

        private readonly Curve _curve;
        
        protected override void OnHostChanged(PlotPanel host)
        {
            base.OnHostChanged(host);
            if (this.host != null)
            {
                try
                {
                    RemoveElements((bool)GetValue(UseDirect2DProperty));
                    this.host = host;
                    BindingOperations.ClearBinding(this, UseDirect2DProperty);
                }
                catch (Exception) 
                { 
                    // Just swallow any exception 
                }
            }
            else this.host = host;
            if (this.host != null)
            {
                AddElements();
                _curve.Transform(xAxis.GraphTransform, yAxis.GraphTransform);
                // Add binding:
                _bindingDirect2D = new Binding("UseDirect2D") { Source = host, Mode = BindingMode.OneWay };
                BindingOperations.SetBinding(this, UseDirect2DProperty, _bindingDirect2D);
            }
            SetBounds();
        }
        Binding _bindingDirect2D;

        private void AddElements()
        {
            if ((bool)GetValue(UseDirect2DProperty) == false)
            {   
                _line.Data = LineGeometries.PathGeometryFromCurve(_curve, null);
                _line.SetValue(Panel.ZIndexProperty, 200);
                _line.Data.Transform = GraphToCanvas;
                _markers.SetValue(Panel.ZIndexProperty, 200);
                host.Canvas.Children.Add(_line);
                host.Canvas.Children.Add(_markers);
            }
            else
            {
                if (!host.Direct2DControl.InitializationFailed)
                {
                    host.Direct2DControl.AddPath(_lineD2D);
                    host.Direct2DControl.AddPath(_markersD2D);
                    _markersD2D.GraphToCanvas = GraphToCanvas;
                }
            }
            Plot.Legend.Items.Add(_legendItem);
            _annotation.SetValue(Panel.ZIndexProperty, 201);
            _annotation.Visibility = Visibility.Collapsed;
            host.Canvas.Children.Add(_annotation);
            //
            UpdateLegendMarkers();
        }

        private void RemoveElements(bool removeDirect2DComponents)
        {
            if (removeDirect2DComponents)
            {
                if (!host.Direct2DControl.InitializationFailed)
                {
                    host.Direct2DControl.RemovePath(_lineD2D);
                    host.Direct2DControl.RemovePath(_markersD2D);
                }
            }
            else
            {
                host.Canvas.Children.Remove(_line);
                host.Canvas.Children.Remove(_markers);
            }
            Plot.Legend.Items.Remove(_legendItem);
            host.Canvas.Children.Remove(_annotation);
        }

        public Plot2DCurve(Curve curve)
        {
            _curve = curve;
            Initialize();
        }

        public Plot2DCurve(object x, object y)
        {
            _curve = new Curve(Plotting.Array(x), Plotting.Array(y));
            Initialize();
        }

        protected void Initialize()
        {
            _line = new PlotPath();
            _markers = new PlotPath();
            _line.StrokeLineJoin = PenLineJoin.Bevel;
            _line.Visibility = Visibility.Visible;
            _markers.Visibility = Visibility.Visible;
            //
            _annotation = new PlotPointAnnotation();
            //
            _legendItem = CreateLegendItem();
            //
            // Name binding
            var titleBinding = new Binding("Title") { Source = this, Mode = BindingMode.OneWay };
            _legendItem.SetBinding(LegendItem.TitleProperty, titleBinding);
            // Other bindings:
            BindToThis(_line, false, true);
            BindToThis(_legendLine, false, true);
            BindToThis(_markers, true, false);
            BindToThis(_legendMarker, true, false);
        }

        protected virtual LegendItem CreateLegendItem()
        {
            var legendItem = new LegendItem();
            var legendItemGrid = new Grid();
            _legendLine = new PlotPath();
            _legendMarker = new PlotPath();
            _legendMarker.HorizontalAlignment = HorizontalAlignment.Center; _legendMarker.VerticalAlignment = VerticalAlignment.Center;
            _legendLine.HorizontalAlignment = HorizontalAlignment.Center; _legendLine.VerticalAlignment = VerticalAlignment.Center;
            _legendLine.Data = new LineGeometry(new Point(0, 0), new Point(30, 0));
            legendItemGrid.Children.Add(_legendLine);
            legendItemGrid.Children.Add(_legendMarker);
            legendItem.Content = legendItemGrid;
            return legendItem;
        }

        protected static void OnUseDirect2DChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var plot2DCurveLocal = ((Plot2DCurve)obj);
            if ((bool)e.NewValue && plot2DCurveLocal._lineD2D == null)
            {
                plot2DCurveLocal._lineD2D = new DirectPath();
                plot2DCurveLocal._markersD2D = new DirectPathScatter { Curve = plot2DCurveLocal._curve };
                plot2DCurveLocal.BindToThis(plot2DCurveLocal._lineD2D, false, true);
                plot2DCurveLocal.BindToThis(plot2DCurveLocal._markersD2D, true, false);
            }
            if (plot2DCurveLocal.host == null) return;
            plot2DCurveLocal.RemoveElements((bool)e.OldValue);
            plot2DCurveLocal.AddElements();
        }

        internal override void OnAxisTypeChanged()
        {
            _curve.Transform(xAxis.GraphTransform, yAxis.GraphTransform);
            SetBounds();
        }

        internal override void BeforeArrange()
        {
            GraphToCanvas.Matrix = new Matrix(xAxis.Scale, 0, 0, -yAxis.Scale, -xAxis.Offset - xAxis.AxisPadding.Lower, yAxis.Offset + yAxis.AxisTotalLength - yAxis.AxisPadding.Upper);
            CanvasToGraph = (MatrixTransform)(GraphToCanvas.Inverse); 
            Curve.FilterMinMax(CanvasToGraph, new Rect(new Point(xAxis.Min, yAxis.Min), new Point(xAxis.Max, yAxis.Max)));
            if (host.UseDirect2D && !host.Direct2DControl.InitializationFailed)
            {
                _lineD2D.Geometry = _curve.ToDirect2DPathGeometry(_lineD2D.Factory, GraphToCanvas);
                _markersD2D.SetGeometry((MarkersType)GetValue(MarkersTypeProperty), (double)GetValue(MarkersSizeProperty));
                //host.direct2DControl.RequestRender();
            }
            else
            {
                _line.Data = LineGeometries.PathGeometryFromCurve(_curve, GraphToCanvas);
                _markers.Data = MarkerGeometries.MarkersAsGeometry(Curve, GraphToCanvas, (MarkersType)GetValue(MarkersTypeProperty), (double)GetValue(MarkersSizeProperty));
            }
            var annotationPoint = GraphToCanvas.Transform(new Point(_curve.XTransformed[0], _curve.YTransformed[0]));
            _annotation.SetValue(Canvas.TopProperty, annotationPoint.Y); _annotation.SetValue(Canvas.LeftProperty, annotationPoint.X);
        }

        internal Point SnappedCanvasPoint(Point canvasPoint, out int index)
        {
            index = CurveIndexFromCanvasPoint(canvasPoint);
            return GraphToCanvas.Transform(new Point(_curve.XTransformed[index], _curve.YTransformed[index]));
        }

        internal int CurveIndexFromCanvasPoint(Point canvasPoint)
        {
            var graphPoint = CanvasToGraph.Transform(canvasPoint);
            var value = _curve.SortedValues == SortedValues.X ? graphPoint.X : graphPoint.Y;
            var index = Curve.GetInterpolatedIndex(_curve.TransformedSorted, value);
            if (index == (_curve.XTransformed.Length - 1)) return _curve.SortedToUnsorted[_curve.XTransformed.Length - 1]; 
            // otherwise return nearest:
            if ((_curve.TransformedSorted[index + 1] - value) < (value - _curve.TransformedSorted[index]))
                return _curve.SortedToUnsorted[index + 1];
            return _curve.SortedToUnsorted[index];
        }

        private void SetBounds()
        {
            bounds = _curve.Bounds();
        }

        public override Rect TightBounds => TransformRect(bounds, xAxis.CanvasTransform, yAxis.CanvasTransform);

        public override Rect PaddedBounds
        {
            get 
            {  
                var paddedBounds =  new Rect(bounds.Left - 0.05 * bounds.Width, bounds.Top - 0.05 * bounds.Height, bounds.Width * 1.1, bounds.Height * 1.1);
                return TransformRect(paddedBounds, xAxis.CanvasTransform, yAxis.CanvasTransform); 
            }
        }

        private Rect TransformRect(Rect rect, Func<double, double> transformX, Func<double, double> transformY)
        {
            return new Rect(new Point(transformX(rect.Left), transformY(rect.Top)), new Point(transformX(rect.Right), transformY(rect.Bottom))); 
        }

        public PlotPath Line => _line;

        public PlotPath Markers => _markers;

        public Rect Bounds => bounds;

        public Curve Curve => _curve;

        protected string GetLinePropertyFromStrokeProperties()
        {
            var lineProperty = "";
            switch ((QuickStrokeDash)GetValue(QuickStrokeDashProperty))
            {
                case QuickStrokeDash.Solid:
                    lineProperty = "-";
                    break;
                case QuickStrokeDash.Dash:
                    lineProperty = "--";
                    break;
                case QuickStrokeDash.Dot:
                    lineProperty = ":";
                    break;
                case QuickStrokeDash.DashDot:
                    lineProperty = "-.";
                    break;
                case QuickStrokeDash.None:
                    lineProperty = "";
                    break;
            }
            switch ((MarkersType)GetValue(MarkersTypeProperty))
            {
                case MarkersType.Square:
                    lineProperty += "s";
                    break;
                case MarkersType.Circle:
                    lineProperty += "o";
                    break;
                case MarkersType.TrianglePointUp:
                    lineProperty += "^";
                    break;
            }
            var brush = (Brush)GetValue(StrokeProperty);
            if (Equals(brush, Brushes.Red)) lineProperty += "r";
            if (Equals(brush, Brushes.Green)) lineProperty += "g";
            if (Equals(brush, Brushes.Blue)) lineProperty += "b";
            if (Equals(brush, Brushes.Yellow)) lineProperty += "y";
            if (Equals(brush, Brushes.Cyan)) lineProperty += "c";
            if (Equals(brush, Brushes.Magenta)) lineProperty += "m";
            if (Equals(brush, Brushes.Black)) lineProperty += "k";
            if (Equals(brush, Brushes.White)) lineProperty += "w";
            return lineProperty;
        }

        protected void SetStrokePropertiesFromLineProperty(string lineProperty)
        {
            if (lineProperty == "") lineProperty = "-";
            var currentIndex = 0;
            // First check for line type
            string firstTwo = null; string firstOne = null;
            if (lineProperty.Length >= 2) firstTwo = lineProperty.Substring(0, 2);
            if (lineProperty.Length >= 1) firstOne = lineProperty.Substring(0, 1);
            if (firstTwo == "--") { SetValue(QuickStrokeDashProperty, QuickStrokeDash.Dash); currentIndex = 2; }
            else if (firstTwo == "-.") { SetValue(QuickStrokeDashProperty, QuickStrokeDash.DashDot); currentIndex = 2; }
            else if (firstOne == ":") { SetValue(QuickStrokeDashProperty, QuickStrokeDash.Dot); currentIndex = 1; }
            else if (firstOne == "-") { SetValue(QuickStrokeDashProperty, QuickStrokeDash.Solid); currentIndex = 1; }
            else SetValue(QuickStrokeDashProperty, QuickStrokeDash.None);
            // 
            // Next check for markers type
            string marker = null;
            if (lineProperty.Length >= currentIndex + 1) marker = lineProperty.Substring(currentIndex, 1);
            if (marker == "s") { SetValue(MarkersTypeProperty, MarkersType.Square); currentIndex++; }
            else if (marker == "o") { SetValue(MarkersTypeProperty, MarkersType.Circle); currentIndex++; }
            else if (marker == "^") { SetValue(MarkersTypeProperty, MarkersType.TrianglePointUp); currentIndex++; }
            else SetValue(MarkersTypeProperty, MarkersType.None);
            //
            // If no line and no marker, assume solid line
            if ((MarkersType == MarkersType.None) && (QuickStrokeDash == QuickStrokeDash.None))
            {
                QuickStrokeDash = QuickStrokeDash.Solid;
            }
            // Finally check for colour
            string colour = null;
            if (lineProperty.Length >= currentIndex + 1) colour = lineProperty.Substring(currentIndex, 1);
            if (colour == "r") SetValue(StrokeProperty, Brushes.Red);
            else if (colour == "g") SetValue(StrokeProperty, Brushes.Green);
            else if (colour == "b") SetValue(StrokeProperty, Brushes.Blue);
            else if (colour == "y") SetValue(StrokeProperty, Brushes.Yellow);
            else if (colour == "c") SetValue(StrokeProperty, Brushes.Cyan);
            else if (colour == "m") SetValue(StrokeProperty, Brushes.Magenta);
            else if (colour == "k") SetValue(StrokeProperty, Brushes.Black);
            else if (colour == "w") SetValue(StrokeProperty, Brushes.White);
            else SetValue(StrokeProperty, Brushes.Black);
        }

        protected void BindToThis(PlotPath target, bool includeFill, bool includeDotDash)
        {
            // Set Stroke property to apply to both the Line and Markers
            var strokeBinding = new Binding("Stroke") { Source = this, Mode = BindingMode.OneWay };
            target.SetBinding(Shape.StrokeProperty, strokeBinding);
            // Set StrokeThickness property also to apply to both the Line and Markers
            var strokeThicknessBinding = new Binding("StrokeThickness") { Source = this, Mode = BindingMode.OneWay };
            target.SetBinding(Shape.StrokeThicknessProperty, strokeThicknessBinding);
            // Fill binding
            var fillBinding = new Binding("MarkersFill") { Source = this, Mode = BindingMode.OneWay };
            if (includeFill) target.SetBinding(Shape.FillProperty, fillBinding);
            // Dot-dash of line
            if (includeDotDash)
            {
                var dashBinding = new Binding("QuickStrokeDash") { Source = this, Mode = BindingMode.OneWay };
                target.SetBinding(PlotPath.QuickStrokeDashProperty, dashBinding);
            }     
        }

        protected void BindToThis(DirectPath target, bool includeFill, bool includeDotDash)
        {
            // Set Stroke property to apply to both the Line and Markers
            var strokeBinding = new Binding("Stroke") { Source = this, Mode = BindingMode.OneWay };
            target.SetBinding(DirectPath.StrokeProperty, strokeBinding);
            // Set StrokeThickness property also to apply to both the Line and Markers
            var strokeThicknessBinding = new Binding("StrokeThickness") { Source = this, Mode = BindingMode.OneWay };
            target.SetBinding(DirectPath.StrokeThicknessProperty, strokeThicknessBinding);
            // Fill binding
            var fillBinding = new Binding("MarkersFill") { Source = this, Mode = BindingMode.OneWay };
            if (includeFill) target.SetBinding(DirectPath.FillProperty, fillBinding);
            // Dot-dash of line
            if (includeDotDash)
            {
                var dashBinding = new Binding("QuickStrokeDash") { Source = this, Mode = BindingMode.OneWay };
                target.SetBinding(DirectPath.QuickStrokeDashProperty, dashBinding);
            }
        }

        internal void UpdateLegendMarkers()
        {
            var markersSize = (double)GetValue(MarkersSizeProperty);
            _legendMarker.Data = MarkerGeometries.LegendMarkerGeometry((MarkersType)GetValue(MarkersTypeProperty), markersSize);
            if (_legendMarker.Data != null) _legendMarker.Data.Transform = new TranslateTransform(markersSize / 2, markersSize / 2);
        }
    }
}
