// Copyright (c) 2010 Joe Moorhouse

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace IronPlot
{
    /// <summary>
    /// Panel class derived from PlotPanel suitable for colour bars
    /// </summary>
    public class ColourBarPanel : PlotPanel
    {
        internal List<Slider> SliderList;
        
        internal ColourBarPanel()
        {
            // Assume vertical alignment for now.
            Axes.XAxisBottom.LabelsVisible = Axes.XAxisTop.LabelsVisible = false;
            Axes.XAxisBottom.TicksVisible = Axes.XAxisTop.TicksVisible = false;
            var allAxes = Axes.XAxes.Concat(Axes.YAxes);
            foreach (var axis in allAxes) axis.GridLines.Visibility = Visibility.Collapsed; 
            Axes.Width = 20;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var slider in SliderList) slider.Measure(availableSize);
            var thumbWidth = SliderList[0].DesiredSize.Width;
            var thumbSemiHeight = SliderList[0].DesiredSize.Height / 2;
            Axes.MinAxisMargin = new Thickness(thumbWidth, thumbSemiHeight, 0, thumbSemiHeight);
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var thumbWidth = SliderList[0].DesiredSize.Width;
            var thumbHeight = SliderList[0].DesiredSize.Height;
            var sliderLocation = new Rect(new Point(CanvasLocation.Left - thumbWidth, CanvasLocation.Top - thumbHeight/2),
                new Size(thumbWidth, CanvasLocation.Height + thumbHeight));
            foreach (var slider in SliderList) slider.Arrange(sliderLocation);
            return base.ArrangeOverride(finalSize);
        }

        internal void AddSliders(List<Slider> sliders)
        {
            SliderList = sliders;
            foreach (var slider in SliderList)
            {
                Children.Add(slider);
                slider.SetValue(ZIndexProperty, 400);
            }
        }

        internal void RemoveSliders()
        {
            if (SliderList == null) return;
            foreach (var slider in SliderList) Children.Remove(slider);
        }
    }
}
