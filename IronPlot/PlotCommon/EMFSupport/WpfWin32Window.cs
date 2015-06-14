using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using IWin32Window = System.Windows.Forms.IWin32Window;

namespace WpfToWmfClipboard
{
    /// <summary>
    /// Helper class to pass a WPF Window object to Win32 methods
    /// </summary>
    public sealed class Win32Wrapper : IWin32Window, System.Windows.Interop.IWin32Window
    {
        private readonly Func<IntPtr> _handleGetter;

        public Win32Wrapper(Window window)
        {
            if (window == null)
                throw new ArgumentNullException("window");
            var interop = new WindowInteropHelper(window);
            _handleGetter = () => interop.Handle;
        }

        public Win32Wrapper(Control control)
        {
            if (control == null)
                throw new ArgumentNullException("control");
            _handleGetter = () => control.Handle;
        }

        IntPtr IWin32Window.Handle => _handleGetter();

        IntPtr System.Windows.Interop.IWin32Window.Handle => _handleGetter();
    }

    public static class WpfWin32WindowHelper
    {
        public static Win32Wrapper AsWin32(this Window window)
        {
            return new Win32Wrapper(window);
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

        public static Win32Wrapper OwnerAsWin32(this DependencyObject startObject)
        {
            var window = GetDependencyObjectFromVisualTree<Window>(startObject);
            if (window == null)
                return null;
            return new Win32Wrapper(window);
        }
    }
}
