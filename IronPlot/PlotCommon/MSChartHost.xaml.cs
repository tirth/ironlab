using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using ContextMenu = System.Windows.Controls.ContextMenu;
using MenuItem = System.Windows.Controls.MenuItem;
using UserControl = System.Windows.Controls.UserControl;

namespace IronPlot
{   
    /// <summary>
    /// Interaction logic for WindowsFormsPlotHost.xaml
    /// </summary>
    public partial class MsChartHost : UserControl
    {
        public MsChartHost()
        {
            InitializeComponent();
            AddContextMenu();
            WindowsFormsHost.Child = _chart;
            _chart.MouseDown += chart_MouseDown;
        }

        Chart _chart = new Chart();
        public Chart Chart { get { return _chart; } set { _chart = value; } } 

        public void OpenContextMenu(object sender, MouseEventArgs args)
        {
            if (args.Button == MouseButtons.Right) WindowsFormsHost.ContextMenu.IsOpen = true;
        }

        public void chart_MouseDown(object sender, MouseEventArgs e)
        {
            WindowsFormsHost.ContextMenu.IsOpen = true;
        }

        protected void AddContextMenu()
        {
            var mainMenu = new ContextMenu();

            var item1 = new MenuItem { Header = "Copy to Clipboard" };
            mainMenu.Items.Add(item1);

            var item1A = new MenuItem();
            item1A.Header = "96 dpi";
            item1.Items.Add(item1A);
            item1A.Click += OnClipboardCopy_96dpi;

            var item1B = new MenuItem();
            item1B.Header = "300 dpi";
            item1.Items.Add(item1B);
            item1B.Click += OnClipboardCopy_300dpi;

            var item1C = new MenuItem { Header = "Enhanced Metafile" };
            item1.Items.Add(item1C);
            item1C.Click += CopyToEmf;

            //MenuItem item2 = new MenuItem() { Header = "Print..." };
            //mainMenu.Items.Add(item2);
            //item2.Click += InvokePrint;
            WindowsFormsHost.ContextMenu = mainMenu;
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
            using (var stream = new MemoryStream())
            {
                _chart.SaveImage(stream, ChartImageFormat.Emf);
                stream.Seek(0, SeekOrigin.Begin);
                var metafile = new Metafile(stream);
                ClipboardMetafileHelper.PutEnhMetafileOnClipboard(_chart.Handle, metafile);
            }
        }

        public void ToClipboard(int dpi)
        {
            var width = _chart.Width * dpi / 96;
            var height = _chart.Height * dpi / 96;

            using (var stream = new MemoryStream())
            {
                // Suitable for low resolution only:
                //chart.SaveImage(stream, ChartImageFormat.Png);
                //Bitmap bitmap = new Bitmap(stream);
                //System.Windows.Forms.Clipboard.SetDataObject(bitmap);

                _chart.SaveImage(stream, ChartImageFormat.Emf);
                stream.Seek(0, SeekOrigin.Begin);
                var metafile = new Metafile(stream);

                var bitmap = new Bitmap(width, height);
                var graphics = Graphics.FromImage(bitmap);
                graphics.DrawImage(metafile, 0, 0, width, height);
                Clipboard.SetDataObject(bitmap);
            }
        }
    }

    public class ClipboardMetafileHelper
    {
        [DllImport("user32.dll")]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();
        [DllImport("user32.dll")]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
        [DllImport("user32.dll")]
        static extern bool CloseClipboard();
        [DllImport("gdi32.dll")]
        static extern IntPtr CopyEnhMetaFile(IntPtr hemfSrc, IntPtr hNull);
        [DllImport("gdi32.dll")]
        static extern bool DeleteEnhMetaFile(IntPtr hemf);

        // Metafile mf is set to a state that is not valid inside this function.
        static public bool PutEnhMetafileOnClipboard(IntPtr hWnd, Metafile mf)
        {
            var bResult = false;
            IntPtr hEmf, hEmf2;
            hEmf = mf.GetHenhmetafile(); // invalidates mf
            if (!hEmf.Equals(new IntPtr(0)))
            {
                hEmf2 = CopyEnhMetaFile(hEmf, new IntPtr(0));
                if (!hEmf2.Equals(new IntPtr(0)))
                {
                    if (OpenClipboard(hWnd))
                    {
                        if (EmptyClipboard())
                        {
                            var hRes = SetClipboardData(14 /*CF_ENHMETAFILE*/, hEmf2);
                            bResult = hRes.Equals(hEmf2);
                            CloseClipboard();
                        }
                    }
                }
                DeleteEnhMetaFile(hEmf);
            }
            return bResult;
        }
    }
}
