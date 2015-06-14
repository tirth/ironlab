// Copyright (c) 2010 Joe Moorhouse

using System.Windows.Markup;

namespace IronPlot
{
    [ContentProperty("Annotations")]
    public partial class PlotPanel
    {
        protected override void CreateLegends()
        {
            plotItems = new UniqueObservableCollection<Plot2DItem>();
            plotItems.CollectionChanged += annotations_CollectionChanged;
            base.CreateLegends();
        }
    }
}