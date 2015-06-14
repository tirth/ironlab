﻿// Copyright (c) 2010 Joe Moorhouse

using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

//using System.Windows.Xps.Packaging;
//using System.Windows.Xps.Serialization;

#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
using ILNumerics.Exceptions;
#endif


namespace IronPlot
{
    public enum QuickStrokeDash { None, Solid, Dash, Dot, DashDot, DashDotDot };
    
    /// <summary>
    /// Extension class of Path that allows more convenient setting of line properties
    /// Has to inherit from Shape since Path is sealed
    /// </summary>
    public class PlotPath : Shape
    {
        static PlotPath()
        {
            DataProperty = DependencyProperty.Register("Data", typeof(Geometry), typeof(PlotPath), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure), null);
        }

        //[CommonDependencyProperty]
        public static readonly DependencyProperty DataProperty;

        public Geometry Data
        {
            get
            {
                return (Geometry)GetValue(DataProperty);
            }
            set
            {
                SetValue(DataProperty, value);
            }
        }

        protected override Geometry DefiningGeometry
        {
            get
            {
                var data = Data;
                if (data == null)
                {
                    data = Geometry.Empty;
                }
                return data;
            }
        }
 
        public static readonly DependencyProperty QuickStrokeDashProperty =
            DependencyProperty.Register("QuickStrokeDash",
            typeof(QuickStrokeDash), typeof(PlotPath),
            new PropertyMetadata(QuickStrokeDash.Solid,
                OnQuickStrokeDashPropertyChanged));

        public QuickStrokeDash QuickStrokeDash
        {
            set
            {
                SetValue(QuickStrokeDashProperty, value);
            }
            get
            {
                return (QuickStrokeDash)GetValue(QuickStrokeDashProperty); 
            }
        }

        protected static void OnQuickStrokeDashPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var doubleColl = new DoubleCollection();
            if ((QuickStrokeDash)(e.NewValue) == QuickStrokeDash.None)
            {
                ((PlotPath)obj).SetValue(VisibilityProperty, Visibility.Collapsed);
            }
            else
            {
                ((PlotPath)obj).SetValue(VisibilityProperty, Visibility.Visible);
            }
            if ((QuickStrokeDash)(e.NewValue) == QuickStrokeDash.Solid)
            {
                ((PlotPath)obj).SetValue(StrokeDashArrayProperty, null);
                ((PlotPath)obj).SetValue(StrokeDashCapProperty, PenLineCap.Flat);
            }
            else
            {
                switch ((QuickStrokeDash)(e.NewValue))
                {
                    case QuickStrokeDash.Dash:
                        doubleColl.Add(4); doubleColl.Add(4);
                        break;
                    case QuickStrokeDash.Dot:
                        doubleColl.Add(1); doubleColl.Add(4);
                        break;
                    case QuickStrokeDash.DashDot:
                        doubleColl.Add(4); doubleColl.Add(4); doubleColl.Add(1); doubleColl.Add(4);
                        break;
                    case QuickStrokeDash.DashDotDot:
                        doubleColl.Add(4); doubleColl.Add(4); doubleColl.Add(1); doubleColl.Add(4); doubleColl.Add(1); doubleColl.Add(4);
                        break;
                    default:
                        doubleColl.Add(4); doubleColl.Add(0);
                        break;
                }
                ((PlotPath)obj).SetValue(StrokeDashArrayProperty, doubleColl);
                ((PlotPath)obj).SetValue(StrokeDashCapProperty, PenLineCap.Square);
            }
        }
    }
}
