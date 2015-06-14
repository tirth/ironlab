using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Xps.Packaging;

namespace IronPlot
{
    /// <summary>
    /// Interaction logic for Plot.xaml
    /// </summary>
    public partial class Plot2D : UserControl
    {
        public Plot2D()
        {
            InitializeComponent();
            _children = new ObservableCollection<Plot2DItem>();
            AddContextMenu();
            // Adapter used for templating; actually not needed for UserControl implementation.
            _childrenAdapter = new ObservableCollectionListAdapter<Plot2DItem>
            {
                Collection = _children,
                TargetList = PlotPanel.PlotItems
            };
            _childrenAdapter.Populate();
        }

        private ObservableCollection<Plot2DItem> _children;
        private ObservableCollectionListAdapter<Plot2DItem> _childrenAdapter;

        public bool EqualAxes
        {
            set
            {
                PlotPanel.SetValue(PlotPanel.EqualAxesProperty, value);
            }
            get { return (bool)PlotPanel.GetValue(PlotPanel.EqualAxesProperty); }
        }

        public bool UseDirect2D
        {
            set
            {
                PlotPanel.SetValue(PlotPanel.UseDirect2DProperty, value);
            }
            get { return (bool)PlotPanel.GetValue(PlotPanel.UseDirect2DProperty); }
        }

        public Axes2D Axes => PlotPanel.Axes;

        public Position LegendPosition
        {
            set { PlotPanelBase.SetPosition(Legend, value); }
            get { return PlotPanelBase.GetPosition(Legend); }
        }

        public Brush BackgroundPlotSurround
        {
            get { return PlotPanel.Background; }
            set { PlotPanel.Background = value; }
        }

        public Brush BackgroundPlot
        {
            get { return PlotPanel.BackgroundCanvas.Background; }
            set { PlotPanel.BackgroundCanvas.Background = value; }
        }

        protected void CommonConstructor()
        {
            _children = new ObservableCollection<Plot2DItem>();
            AddContextMenu();
            _childrenAdapter = new ObservableCollectionListAdapter<Plot2DItem>();
        }

        // To add templating; alternate to UserControl.
        //public override void OnApplyTemplate()
        //{
        //    base.OnApplyTemplate();
        //    plotPanel = GetTemplateChild("PlotPanel") as PlotPanel;
        //    childrenAdapter.Collection = children;
        //    childrenAdapter.TargetList = plotPanel.PlotItems;
        //    childrenAdapter.Populate();
        //}

        public Collection<Plot2DItem> Children => _children;

        protected void AddContextMenu()
        {
            var mainMenu = new ContextMenu();

            var item1 = new MenuItem { Header = "Copy to Clipboard" };
            mainMenu.Items.Add(item1);

            var item1A = new MenuItem { Header = "96 dpi" };
            item1.Items.Add(item1A);
            item1A.Click += OnClipboardCopy_96dpi;

            var item1B = new MenuItem { Header = "300 dpi" };
            item1.Items.Add(item1B);
            item1B.Click += OnClipboardCopy_300dpi;

            var item1C = new MenuItem { Header = "Enhanced Metafile" };
            item1.Items.Add(item1C);
            item1C.Click += CopyToEmf;

            var item2 = new MenuItem { Header = "Print..." };
            mainMenu.Items.Add(item2);
            item2.Click += InvokePrint;

            ContextMenu = mainMenu;
        }

        protected void OnClipboardCopy_96dpi(object sender, EventArgs e)
        {
            ToClipboard(96);
        }

        protected void OnClipboardCopy_300dpi(object sender, EventArgs e)
        {
            ToClipboard(300);
        }

        protected void CopyToEmf(object sender, EventArgs e)
        {
            try
            {
                var direct2D = UseDirect2D;
                if (direct2D) UseDirect2D = false;
                EmfCopy.CopyVisualToWmfClipboard(this, Window.GetWindow(this));
                if (direct2D) UseDirect2D = true;
            }
            catch (Exception)
            {
                // Swallow exception
                //throw new Exception("Writing to enhanced metafile failed for plot.");
            }
        }

        public void ToClipboard(int dpi)
        {
            var direct2D = UseDirect2D;
            if (direct2D) UseDirect2D = false;
            try
            {
                var drawingVisual = new DrawingVisual();
                var drawingContext = drawingVisual.RenderOpen();
                UpdateLayout();
                var sourceBrush = new VisualBrush(this);
                var scale = dpi / 96.0;
                var actualWidth = RenderSize.Width;
                var actualHeight = RenderSize.Height;
                using (drawingContext)
                {
                    drawingContext.PushTransform(new ScaleTransform(scale, scale));
                    drawingContext.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0), new Point(actualWidth, actualHeight)));
                }
                InvalidateVisual();
                var renderBitmap = new RenderTargetBitmap((int)(actualWidth * scale), (int)(actualHeight * scale), 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(drawingVisual);
                Clipboard.SetImage(renderBitmap);
            }
            catch (Exception)
            {
                // Swallow exception
                //throw new Exception("Creating image failed for plot.");
            }
            if (direct2D) UseDirect2D = true;
        }

        private bool? _print;
        private PrintDialog _printDialog;

        private void InvokePrint(object sender, RoutedEventArgs e)
        {
            _printDialog = new PrintDialog();

            _print = false;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                new Action(delegate
                                {
                                    _print = _printDialog.ShowDialog();
                                    this.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                        new Action(AfterInvokePrint));
                                }));

        }

        private void AfterInvokePrint()
        {
            if (_print != true) return;
            var filename = Path.GetTempPath() + "IronPlotPrint.xps";
            var package = Package.Open(filename, FileMode.Create);
            var xpsDoc = new XpsDocument(package);
            var xpsWriter = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
            var direct2D = UseDirect2D;
            if (direct2D) UseDirect2D = false;
            try
            {
                UpdateLayout();
                xpsWriter.Write(PlotPanel);
                xpsDoc.Close();
                package.Close();
            }
            finally
            {
                if (direct2D) UseDirect2D = true;
            }
            var printQueue = _printDialog.PrintQueue;
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(delegate
                {
                    printQueue.AddJob("IronPlot Print", filename, false);
                }));
        }

        #region ConvenienceMethods

        public Plot2DCurve AddLine(double[] y)
        {
            var x = MathHelper.Counter(y.Length);
            var plot2DCurve = AddLine(x, y);
            return plot2DCurve;
        }

        public Plot2DCurve AddLine(double[] y, string quickLine)
        {
            var x = MathHelper.Counter(y.Length);
            var plot2DCurve = AddLine(x, y);
            plot2DCurve.QuickLine = quickLine;
            return plot2DCurve;
        }

        public Plot2DCurve AddLine(double[] x, double[] y, string quickLine)
        {
            var plot2DCurve = AddLine(x, y);
            plot2DCurve.QuickLine = quickLine;
            return plot2DCurve;
        }

        public Plot2DCurve AddLine(object x, object y, string quickLine)
        {
            var plot2DCurve = AddLine(x, y);
            plot2DCurve.QuickLine = quickLine;
            return plot2DCurve;
        }

        public Plot2DCurve AddLine(double[] x, double[] y)
        {
            var curve = new Curve(x, y);
            var plot2DCurve = new Plot2DCurve(curve);
            Children.Add(plot2DCurve);
            return plot2DCurve;
        }

        public Plot2DCurve AddLine(object x, object y)
        {
            var curve = new Curve(Plotting.Array(x), Plotting.Array(y));
            var plot2DCurve = new Plot2DCurve(curve);
            Children.Add(plot2DCurve);
            return plot2DCurve;
        }

        public FalseColourImage AddFalseColourImage(double[,] image)
        {
            var falseColour = new FalseColourImage(image);
            Children.Add(falseColour);
            return falseColour;
        }

        public FalseColourImage AddFalseColourImage(IEnumerable<object> image)
        {
            var falseColour = new FalseColourImage(image);
            Children.Add(falseColour);
            return falseColour;
        }

        public FalseColourImage AddFalseColourImage(double[] x, double[] y, double[,] image)
        {
            var falseColour =
                new FalseColourImage(new Rect(new Point(x.Min(), y.Min()), new Point(x.Max(), y.Max())), image, true);
            Children.Add(falseColour);
            return falseColour;
        }

        public FalseColourImage AddFalseColourImage(IEnumerable<object> x, IEnumerable<object> y, IEnumerable<object> image)
        {
            int xLength, yLength;
            var xa = GeneralArray.ToImageEnumerator(x, out xLength, out yLength);
            var ya = GeneralArray.ToImageEnumerator(y, out xLength, out yLength);
            var falseColour =
                new FalseColourImage(new Rect(new Point(xa.Min(), ya.Min()), new Point(xa.Max(), ya.Max())), image, true);
            Children.Add(falseColour);
            return falseColour;
        }

        #endregion ConvenienceMethods
    }
}
