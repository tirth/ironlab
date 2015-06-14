using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IronPlot.Plotting3D
{
    /// <summary>
    /// Interaction logic for Plot3D.xaml
    /// </summary>
    public partial class Plot3D
    {
        #region DependencyProperties
        public static readonly DependencyProperty ProjectionTypeProperty =
            DependencyProperty.Register("ProjectionType",
            typeof(ProjectionType), typeof(Plot3D),
            new FrameworkPropertyMetadata(ProjectionType.Perspective));

        public ProjectionType ProjectionType
        {
            set { SetValue(ProjectionTypeProperty, value); }
            get { return (ProjectionType)GetValue(ProjectionTypeProperty); }
        }
        #endregion

        public Plot3D()
        {
            InitializeComponent();
            AddContextMenu();
        }

        public Viewport3D Viewport3D => viewport3D;

        protected void AddContextMenu()
        {
            var mainMenu = new ContextMenu();

            var item1 = new MenuItem {Header = "Copy to Clipboard"};
            mainMenu.Items.Add(item1);

            //MenuItem item2 = new MenuItem();
            //item2.Header = "Print...";
            //mainMenu.Items.Add(item2);

            var item1A = new MenuItem();
            item1A.Header = "96 dpi";
            item1.Items.Add(item1A);
            item1A.Click += OnClipboardCopy_96dpi;

            var item1B = new MenuItem();
            item1B.Header = "192 dpi";
            item1.Items.Add(item1B);
            item1B.Click += OnClipboardCopy_192dpi;

            var item1C = new MenuItem();
            item1C.Header = "288 dpi";
            item1.Items.Add(item1C);
            item1C.Click += OnClipboardCopy_288dpi;

            //MenuItem item2 = new MenuItem();
            //item2.Header = "Print...";
            //mainMenu.Items.Add(item2);
            //item2.Click += InvokePrint;

            ContextMenu = mainMenu;
        }

        private Point _startPosition;

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            _startPosition = e.GetPosition(this);
            base.OnMouseRightButtonUp(e);
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            // Only allow Context Menu on a click, not on click and drag.
            var endPosition = e.GetPosition(this);
            base.OnMouseRightButtonUp(e);
            if ((endPosition.Y - _startPosition.Y) != 0) e.Handled = true;
        }

        protected void OnClipboardCopy_96dpi(object sender, EventArgs e)
        {
            ToClipboard(96);
        }

        protected void OnClipboardCopy_192dpi(object sender, EventArgs e)
        {
            ToClipboard(192);
        }

        protected void OnClipboardCopy_288dpi(object sender, EventArgs e)
        {
            ToClipboard(288);
        }

        public void ToClipboard(int dpi)
        {
            var drawingVisual = new DrawingVisual();
            var drawingContext = drawingVisual.RenderOpen();
            var sourceBrush = new VisualBrush(Viewport3D);
            var scale = dpi / 96.0;
            var actualWidth = Viewport3D.RenderSize.Width;
            var actualHeight = Viewport3D.RenderSize.Height;
            using (drawingContext)
            {
                drawingContext.PushTransform(new ScaleTransform(scale, scale));
                drawingContext.DrawRectangle(sourceBrush, null, new Rect(new Point(0, 0), new Point(actualWidth, actualHeight)));
            }
            Viewport3D.SetResolution(dpi);
            Viewport3D.InvalidateVisual();
            var renderBitmap = new RenderTargetBitmap((int)(actualWidth * scale), (int)(actualHeight * scale), 96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(drawingVisual);
            Clipboard.SetImage(renderBitmap);
            Viewport3D.SetResolution(96);
        }
    }
}
