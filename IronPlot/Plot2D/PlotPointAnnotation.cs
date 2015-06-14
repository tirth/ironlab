using System.Windows;
using System.Windows.Controls;

namespace IronPlot
{
    public class PlotPointAnnotation : ContentControl
    {
        public static DependencyProperty AnnotationProperty =
            DependencyProperty.Register("Annotation",
            typeof(string), typeof(PlotPointAnnotation),
            new FrameworkPropertyMetadata("Annotation", FrameworkPropertyMetadataOptions.AffectsRender));

        public string Annotation
        {
            set
            {
                SetValue(AnnotationProperty, value);
            }
            get { return (string)GetValue(AnnotationProperty); }
        }

        static PlotPointAnnotation()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PlotPointAnnotation), new FrameworkPropertyMetadata(typeof(PlotPointAnnotation)));
        }
    }
}
