using System.Windows;
using System.Windows.Controls;

namespace IronPlot
{
    public class AxisCanvas : Canvas
    {
        protected override Size MeasureOverride(Size constraint)
        {
            var size = base.MeasureOverride(constraint);
            // Allows requests more space than allocated in order to trigger a Measure pass on the parent.
            // We assume that any change that can affect Measure in the AxisCanvas children should triggera full
            // layout pass by the PlotPanel.
            return new Size(ActualWidth + 1, ActualHeight + 1);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var finalSize = base.ArrangeOverride(arrangeSize);
            return finalSize;
        }
    }
}
