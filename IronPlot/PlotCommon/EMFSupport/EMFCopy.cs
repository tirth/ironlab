using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using BCDev.XamlToys;
using WpfToWmfClipboard;

// This comes from http://xamltoys.codeplex.com/ (including source)

namespace IronPlot
{
    public class EmfCopy
    {
        public static void CopyVisualToWmfClipboard(Visual visual, Window clipboardOwnerWindow)
        {
            CopyXamlStreamToWmfClipBoard(visual, clipboardOwnerWindow);
        }

        public static object LoadXamlFromStream(Stream stream)
        {
            using (var s = stream)
                return XamlReader.Load(s);
        }

        public static Graphics CreateEmf(Stream wmfStream, Rect bounds)
        {
            if (bounds.Width == 0 || bounds.Height == 0) bounds = new Rect(0, 0, 1, 1);
            using (var refDc = Graphics.FromImage(new Bitmap(1, 1)))
            {
                var graphics = Graphics.FromImage(new Metafile(wmfStream, refDc.GetHdc(), bounds.ToGdiPlus(), MetafileFrameUnit.Pixel, EmfType.EmfPlusDual));
                return graphics;
            }
        }

        public static T GetDependencyObjectFromVisualTree<T>(DependencyObject startObject)
            // don't restrict to DependencyObject items, to allow retrieval of interfaces
            //where T : DependencyObject
            where T : class
        {
            //Walk the visual tree to get the parent(ItemsControl) 
            //of this control
            var parent = startObject;
            while (parent != null)
            {
                var pt = parent as T;
                if (pt != null)
                    return pt;
                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        private static void CopyXamlStreamToWmfClipBoard(Visual visual, Window clipboardOwnerWindow)
        {
            // http://xamltoys.codeplex.com/
            var drawing = Utility.GetDrawingFromXaml(visual);

            var bounds = drawing.Bounds;
            Console.WriteLine("Drawing Bounds: {0}", bounds);

            var wmfStream = new MemoryStream();

            using (var g = CreateEmf(wmfStream, bounds))
                Utility.RenderDrawingToGraphics(drawing, g);

            wmfStream.Position = 0;

            var metafile = new Metafile(wmfStream);

            IntPtr hEmf, hEmf2;
            hEmf = metafile.GetHenhmetafile(); // invalidates mf
            if (!hEmf.Equals(new IntPtr(0)))
            {
                hEmf2 = NativeMethods.CopyEnhMetaFile(hEmf, new IntPtr(0));
                if (!hEmf2.Equals(new IntPtr(0)))
                {
                    if (NativeMethods.OpenClipboard(((IWin32Window)clipboardOwnerWindow.OwnerAsWin32()).Handle))
                    {
                        if (NativeMethods.EmptyClipboard())
                        {
                            NativeMethods.SetClipboardData(14 /*CF_ENHMETAFILE*/, hEmf2);
                            NativeMethods.CloseClipboard();
                        }
                    }
                }
                NativeMethods.DeleteEnhMetaFile(hEmf);
            }
        }
    }
}
