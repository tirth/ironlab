// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;

namespace IronPlot
{
    /// <summary>
    /// Base class for any items that are displayed on a Plot2D
    /// </summary>
    public abstract class Plot2DItem : DependencyObject
    {
        protected Rect bounds;
        protected PlotPanel host;

        protected XAxis xAxis;
        public XAxis XAxis
        {
            get { return xAxis; }
            set
            {
                if (host == null) xAxis = value;
                else if (host.Axes.XAxes.Contains(value))
                {
                    xAxis = value;
                    host.InvalidateArrange();
                }
                else throw new Exception("Axis does not belong to plot.");
            }
        }

        protected YAxis yAxis;
        public YAxis YAxis
        {
            get { return yAxis; }
            set
            {
                if (host == null) yAxis = value;
                else if (host.Axes.YAxes.Contains(value))
                {
                    yAxis = value;
                    host.InvalidateArrange();
                }
                else throw new Exception("Axis does not belong to plot.");
            }
        }

        public Plot2D Plot
        {
            get
            {
                DependencyObject parent = host;
                while ((parent != null) && !(parent is Plot2D))
                {
                    parent = LogicalTreeHelper.GetParent(parent);
                }
                return (parent as Plot2D);
            }
        }
    
        public virtual Rect TightBounds => bounds;

        public virtual Rect PaddedBounds => bounds;

        internal PlotPanel Host
        {
            get { return host; }
            set
            {
                OnHostChanged(value);
            }
        }

        protected virtual void OnHostChanged(PlotPanel host)
        {
            if (host == null)
            {
                xAxis = null; yAxis = null;
                return;
            }
            // Update axis to default if null or it it does not belong to the new plot.
            if ((xAxis == null) || (!host.Axes.XAxes.Contains(xAxis)))
            {
                xAxis = host.Axes.XAxes.Bottom;
            }

            if ((yAxis == null) || (!host.Axes.YAxes.Contains(yAxis)))
            {
                yAxis = host.Axes.YAxes.Left;
            }
        }

        internal virtual void OnViewedRegionChanged()
        {
        }

        internal abstract void BeforeArrange();

        internal virtual void OnRender()
        {
        }

        internal virtual void OnAxisTypeChanged() { }
    }
}
