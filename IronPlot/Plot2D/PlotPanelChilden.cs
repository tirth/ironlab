// Copyright (c) 2010 Joe Moorhouse

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;

namespace IronPlot
{
    public partial class PlotPanel : PlotPanelBase
    {
        internal UniqueObservableCollection<Plot2DItem> plotItems;

        public Collection<Plot2DItem> PlotItems => plotItems;

        protected void InitialiseChildenCollection()
        {
            plotItems = new UniqueObservableCollection<Plot2DItem>();
            plotItems.CollectionChanged += plotItems_CollectionChanged;
        }

        protected void plotItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (Plot2DItem child in e.OldItems)
                {
                    child.Host = null;
                }
            }
            if (e.NewItems != null)
            {
                foreach (Plot2DItem child in e.NewItems)
                {
                    child.Host = this;
                }
            }
            var allAxes = Axes.XAxes.Concat(Axes.YAxes);
            foreach (var axis in allAxes)
            {
                var axisRange = GetRangeFromChildren(axis);
                if (axisRange.Length != 0) axis.SetValue(Axis2D.RangeProperty, axisRange);
            }
        }

        protected Range GetRangeFromChildren(Axis2D axis)
        {
            var range = new Range(0, 0);
            Plot2DItem child;
            var rangeUpdated = false;
            for (var i = 0; i < plotItems.Count; ++i)
            {
                child = plotItems[i];
                if ((child.XAxis != axis) && (child.YAxis != axis)) continue;
                var bounds = child.PaddedBounds;
                if (rangeUpdated == false)
                {
                    range = (axis is XAxis) ? new Range(bounds.Left, bounds.Right) : new Range(bounds.Top, bounds.Bottom);
                    rangeUpdated = true;
                }
                else range = range.Union((axis is XAxis) ? new Range(bounds.Left, bounds.Right) : new Range(bounds.Top, bounds.Bottom));
            }
            return range;
        }

        protected Rect GetBoundsFromChildren()
        {
            var bounds = new Rect(new Size(10, 10)); // default if there are no children.
            Plot2DItem child;
            for (var i = 0; i < plotItems.Count; ++i)
            {
                child = plotItems[i];
                if (i == 0) bounds = child.PaddedBounds;
                else bounds.Union(child.PaddedBounds);
            }
            return bounds;
        }
    }
}
