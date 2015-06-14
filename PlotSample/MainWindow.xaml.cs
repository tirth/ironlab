using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using IronPlot;
using IronPlot.Plotting3D;

namespace PlotTest
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Plot2DMultipleAxes();

            DatesPlot();

            EqualAxesPlot();

            FalseColourPlot();

            SurfacePlot();

            ScrollViewerPlot();
        }

        void Plot2DMultipleAxes()
        {
            // Example 2D plot:
            // First curve:
            var curve1 = PlotMultipleAxes.AddLine(new[] { 1.2, 1.3, 2.8, 5.6, 1.9, -5.9 });
            curve1.Stroke = Brushes.Blue; curve1.StrokeThickness = 1.5; curve1.MarkersType = MarkersType.Square;
            // Second curve:
            const int nPoints = 2000;
            var x = new double[nPoints];
            var y = new double[nPoints];
            var rand = new Random();
            for (var i = 0; i < nPoints; ++i)
            {
                x[i] = i * 10.0 / nPoints;
                y[i] = Math.Sin(x[i]) + 0.1 * Math.Sin(x[i] * 100);
            }
            var curve2 = new Plot2DCurve(new Curve(x, y)) { QuickLine = "r-" };
            PlotMultipleAxes.Children.Add(curve2);
            // Third curve:
            var curve3 = new Plot2DCurve(new Curve(new[] {1, 3, 1.5, 7}, new[] {4.5, 9.0, 3.2, 4.5}))
            {
                StrokeThickness = 3.0,
                Stroke = Brushes.Green,
                QuickLine = "o",
                MarkersFill = Brushes.Blue,
                Title = "Test3"
            };
            //curve3.QuickStrokeDash = QuickStrokeDash.Dash;
            PlotMultipleAxes.Children.Add(curve3);
            // Can use Direct2D acceleration, but requires DirectX10 (Windows 7)
            //plotMultipleAxes.UseDirect2D = true; 
            //plotMultipleAxes.EqualAxes = true;

            // If you want to lose the gradient background:
            //plotMultipleAxes.Legend.Background = Brushes.White;
            //plotMultipleAxes.BackgroundPlot = Brushes.White;

            // Additional labels:
            //plotMultipleAxes.BottomLabel.Text = "Bottom label";
            //plotMultipleAxes.LeftLabel.Text = "Left label";
            PlotMultipleAxes.FontSize = 14;
            PlotMultipleAxes.Axes.XAxes[0].AxisLabel.Text = "Innermost X Axis";
            PlotMultipleAxes.Axes.YAxes[0].AxisLabel.Text = "Innermost Y Axis";
            var xAxisOuter = new XAxis(); var yAxisOuter = new YAxis();
            xAxisOuter.AxisLabel.Text = "Added X Axis";
            yAxisOuter.AxisLabel.Text = "Added Y Axis";
            PlotMultipleAxes.Axes.XAxes.Add(xAxisOuter);
            PlotMultipleAxes.Axes.YAxes.Add(yAxisOuter);
            yAxisOuter.Position = YAxisPosition.Left;
            PlotMultipleAxes.Axes.XAxes[0].FontStyle = PlotMultipleAxes.Axes.YAxes[0].FontStyle = FontStyles.Oblique;
            //curve3.XAxis = xAxisOuter;
            curve3.YAxis = yAxisOuter;
            // Other alyout options to try:
            //plotMultipleAxes.Axes.EqualAxes = new AxisPair(plot1.Axes.XAxes.Bottom, plot1.Axes.YAxes.Left);
            //plotMultipleAxes.Axes.SetAxesEqual();
            //plotMultipleAxes.Axes.Height = 100;
            //plotMultipleAxes.Axes.Width = 500;
            //plotMultipleAxes.Axes.MinAxisMargin = new Thickness(200, 0, 0, 0);
            PlotMultipleAxes.Axes.XAxes.Top.TickLength = 5;
            PlotMultipleAxes.Axes.YAxes.Left.TickLength = 5;

            xAxisOuter.Min = 6.5e-5;
            xAxisOuter.Max = 7.3e-3;
            xAxisOuter.AxisType = AxisType.Log;
            PlotMultipleAxes.Children.Add(new Plot2DCurve(new Curve(new[] { 0.01, 10 }, new double[] { 5, 6 })) { XAxis = xAxisOuter });
            PlotMultipleAxes.UseDirect2D = false;
        }

        void DatesPlot()
        {
            var start = new DateTime(2006, 4, 26);
            var dates = Enumerable.Range(0, 100).Select(t => start.AddMonths(t)).ToArray();
            var random = new Random();
            var values = Enumerable.Range(0, 100).Select(t => random.NextDouble()).ToArray();
            var curve = datesPlot.AddLine(dates, values);
            curve.XAxis.AxisType = AxisType.Date;
            datesPlot.UseDirect2D = false;
        }

        void EqualAxesPlot()
        {
            var x = Enumerable.Range(0, 1000).Select(t => (double)t * 10 / 1000);
            var y = x.Select(t => 5 * Math.Exp(-t * t / 5));
            equalAxesPlot.AddLine(x, y).Title = "Test";
            equalAxesPlot.Axes.YAxes.Left.FormatOverride = FormatOverrides.Currency;
            //equalAxesPlot.Axes.YAxes.Left.FormatOverride = value => value.ToString("N");
        }

        void FalseColourPlot()
        {
            // Example false colour plot: 
            var falseColour = new double[128, 128];
            for (var i = 0; i < 128; ++i)
                for (var j = 0; j < 128; ++j) falseColour[i, j] = i + j;
            falseColourPlot.AddFalseColourImage(falseColour);
            //falseColourPlot.Axes.XAxes.GridLines.Visibility = Visibility.Collapsed;
            //falseColourPlot.Axes.YAxes.GridLines.Visibility = Visibility.Collapsed;
        }

        void SurfacePlot()
        {
            // Example surface plot:
            const int nx = 96;
            const int ny = 96;
            var x2 = MathHelper.MeshGridX(Enumerable.Range(1, nx).Select(t => (double)t - nx / 2), ny);
            var y2 = MathHelper.MeshGridY(Enumerable.Range(1, ny).Select(t => (double)t - ny / 2), nx);
            var z2 = x2.Zip(y2, (u, v) => Math.Exp((-u * u - v * v) / 400)); // .NET4 method
            //var z2 = x2.Select(u => u * u);
            var surface = new SurfaceModel3D(x2, y2, z2, nx, ny)
            {
                Transparency = 5,
                MeshLines = MeshLines.None,
                SurfaceShading = SurfaceShading.Smooth
            };
            surfacePlot.Viewport3D.Models.Add(surface);
            //surfacePlot.LeftLabel.Text = "Left Label"; plot3.BottomLabel.Text = "Bottom Label";
        }

        void ScrollViewerPlot()
        {
            var curve = scrollViewerPlot.AddLine(new[] { 1.2, 1.3, 2.8, 5.6, 1.9, -5.9 });
            scrollViewerPlot.Axes.Height = scrollViewerPlot.Axes.Width = 500;
            //scrollViewerPlot.Height = scrollViewerPlot.Width = 500;
            // Some events.
            //scrollViewerPlot.Axes.YAxes.Right.MouseEnter += new MouseEventHandler(Bottom_MouseEnter);

        }
    }
}
