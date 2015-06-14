using System.Windows;
using System.Windows.Controls;

namespace IronPlot
{
    public class Title : ContentControl
    {
#if !SILVERLIGHT
        /// <summary>
        /// Initializes the static members of the Title class.
        /// </summary>
        static Title()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Title), new FrameworkPropertyMetadata(typeof(Title)));
        }

#endif
    }
}
