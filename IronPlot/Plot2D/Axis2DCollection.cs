// Copyright (c) 2010 Joe Moorhouse

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace IronPlot
{
    public class XAxis2DCollection : Axis2DCollection
    {
        public XAxis2DCollection(PlotPanel panel)
            : base(panel) { }

        public XAxis Top { get { return this[1] as XAxis; } set { this[0] = value; } }
        public XAxis Bottom { get { return this[0] as XAxis; } set { this[1] = value; } }
    }

    public class YAxis2DCollection : Axis2DCollection
    {
        public YAxis2DCollection(PlotPanel panel)
            : base(panel) { }

        public YAxis Left { get { return this[0] as YAxis; } set { this[0] = value; } }
        public YAxis Right { get { return this[1] as YAxis; } set { this[1] = value; } }
    }

    public class Axis2DCollection : Collection<Axis2D>
    {
        // The panel to which the axes belong.
        readonly PlotPanel _panel;   

        public Axis2DCollection(PlotPanel panel)
        {
            _panel = panel;
        }

        protected override void InsertItem(int index, Axis2D newItem)
        {
            base.InsertItem(index, newItem);
            newItem.PlotPanel = _panel;
            _panel.Children.Add(newItem);
            _panel.BackgroundCanvas.Children.Add(newItem.GridLines);
            newItem.SetValue(Panel.ZIndexProperty, 200);
            _panel.AddAxisInteractionEvents(new List<Axis2D> { newItem });
        }

        protected override void SetItem(int index, Axis2D newItem)
        {
            _panel.RemoveAxisInteractionEvents(new List<Axis2D> { this[index] });
            _panel.Children.Remove(this[index]);
            _panel.BackgroundCanvas.Children.Remove(this[index].GridLines);
            base.SetItem(index, newItem);
            newItem.PlotPanel = _panel;
            _panel.Children.Add(newItem);
            _panel.BackgroundCanvas.Children.Add(newItem.GridLines);
            newItem.SetValue(Panel.ZIndexProperty, 200);
            _panel.AddAxisInteractionEvents(new List<Axis2D> { newItem });
        }

        protected override void RemoveItem(int index)
        {
            _panel.RemoveAxisInteractionEvents(new List<Axis2D> { this[index] });
            _panel.Children.Remove(this[index]);
            _panel.BackgroundCanvas.Children.Remove(this[index].GridLines);
            base.RemoveItem(index);
        }
    }
}

