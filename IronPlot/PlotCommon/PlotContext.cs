// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
using ILNumerics.Exceptions;
#endif

namespace IronPlot
{
    /// <summary>
    /// A wrapper class that allows Plotting API to be used with state information
    /// For example, PlotContext keeps track of the current Plot2D, allowing a condensed syntax
    /// This is mainly intended for use in a scripting language. States are otherwise to be avoided!
    /// </summary>
    public class PlotContext
    {
        static FrameworkElement _currentPlot; 
        static int? _currentPlotIndex;
        static Window _currentWindow;
        static int? _currentWindowIndex;
        static TabItem _currentTabItem;
        static int? _currentTabItemIndex;
        static FrameworkElement _currentGrid;
        // Dictionary to obtain Window by index
        static readonly Dictionary<int?, Window> _windowDictionary = new Dictionary<int?, Window>();
        // Look up by TabControl to obtain Dictionary of TabIem by index
        static readonly Dictionary<TabControl, Dictionary<int?, TabItem>> _tabItemDictionaryLookup = new Dictionary<TabControl, Dictionary<int?, TabItem>>();
        // Dictionary of TabItem by index for current TabControl (TabControl of current TabItem)
        static Dictionary<int?, TabItem> _tabItemDictionary;
        // Look up by Window or TabItem to obtain Dictionary of plot by index
        static readonly Dictionary<FrameworkElement, Dictionary<int?, FrameworkElement>> _plotDictionaryLookup = new Dictionary<FrameworkElement, Dictionary<int?, FrameworkElement>>();
        // Dictionary of plot by index for current TabItem (if present) or Window
        static Dictionary<int?, FrameworkElement> _plotDictionary;

        static bool _holdState;

        static public bool HoldState
        {
            get { return _holdState; }
            set { _holdState = value; }
        }

        static public Window CurrentWindow => _currentWindow;

        static public FrameworkElement CurrentPlot => _currentPlot;

        static public FrameworkElement NextPlot => _currentPlot;

        static public TabItem CurrentTabItem => _currentTabItem;

        static public FrameworkElement CurrentGrid
        {
            get { return _currentGrid; }
            set { _currentGrid = value; }
        }

        static public int? CurrentWindowIndex
        {
            get { return _currentWindowIndex; }
            set
            {
                _currentWindow = null;
                if (value == null || _windowDictionary.TryGetValue(value, out _currentWindow))
                {
                    // Window exists in the Dictionary: set to use this Window
                    _currentWindowIndex = value;
                    if (_currentWindow != null) _currentWindow.Focus();
                    
                    // If the Window has a TabControl, select the minimum index
                    _tabItemDictionary = null;
                    if ((_currentWindow.Content != null) && (_currentWindow.Content.GetType() == typeof(TabControl))
                        && (_tabItemDictionaryLookup.TryGetValue((TabControl)(_currentWindow.Content), out _tabItemDictionary)))
                    {
                        ((TabControl)_currentWindow.Content).Focus();
                        CurrentTabItemIndex = _tabItemDictionary.Keys.Min();
                    }
                    
                    // If the Window or TabItem contains a Grid of plots, select the minimum index
                    _plotDictionary = null;
                    if ((_currentWindow.Content != null) && (_currentWindow.Content.GetType() == typeof(Grid))
                        && (_plotDictionaryLookup.TryGetValue((Grid)(_currentWindow.Content), out _plotDictionary)))
                    {
                        _currentGrid = (Grid)(_currentWindow.Content);
                        CurrentPlotIndex = _plotDictionary.Keys.Min();
                    }
                    if ((_currentTabItemIndex != null) && (CurrentTabItem.Content != null) && (_currentTabItem.Content.GetType() == typeof(Grid))
                        && (_plotDictionaryLookup.TryGetValue((Grid)(_currentTabItem.Content), out _plotDictionary)))
                    {
                        _currentGrid = (Grid)(CurrentTabItem.Content);
                        CurrentPlotIndex = _plotDictionary.Keys.Min();
                    }
                    
                    // If the Window or TabItem contains a plot, set this to be the current plot
                    if ((_currentWindow.Content != null) && (_currentWindow.Content.GetType() == typeof(Plot2D)))
                    {
                        _currentPlot = (Plot2D)(_currentWindow.Content);
                        _currentPlotIndex = null;
                    }
                    else if ((_currentTabItemIndex != null) && (_currentTabItem.Content != null) && (_currentTabItem.Content.GetType() == typeof(Plot2D)))
                    {
                        _currentPlot = (Plot2D)(_currentTabItem.Content);
                        _currentPlotIndex = null;
                    }
                }
                else
                {
                    var newWindow = new Window { Width = 640, Height = 480, Background = Brushes.White };
                    newWindow.Closed += window_Closed;
                    newWindow.Title = "Plot Window " + value;
                    newWindow.Show();
                    _windowDictionary.Add(value, newWindow);
                    _currentWindow = newWindow;
                    _currentWindowIndex = value;
                    _currentWindow.Focus();
                    _currentWindow.BringIntoView();
                    _currentTabItem = null; _currentTabItemIndex = null;
                    _currentGrid = null; 
                    _tabItemDictionary = null; _plotDictionary = null;
                }
            }
        }

        public static void OpenNextWindow()
        {
            if (_windowDictionary.Keys.Max() == null) CurrentWindowIndex = 0;
            else
            {
                // find first unassigned integer
                var index = 0;
                while (_windowDictionary.Keys.Contains(index)) index++;
                CurrentWindowIndex = index;
            }
        }

        private static void window_Closed(Object sender, EventArgs e)
        {
            var entry = _windowDictionary.Single(t => t.Value == (Window)sender);
            _windowDictionary.Remove(entry.Key);
            _plotDictionaryLookup.Remove(entry.Value);
            var windowToRemove = (Window)sender;
            if ((windowToRemove.Content != null) && (windowToRemove.Content.GetType() == typeof(TabControl)))
            {
                _tabItemDictionaryLookup.Remove((TabControl)(windowToRemove.Content));
                _plotDictionaryLookup.Remove((TabControl)(windowToRemove.Content));
            }
            _plotDictionary = null; _tabItemDictionary = null;
            if (_currentWindow == (Window)sender)
            {
                _currentWindow = null;
                _currentWindowIndex = null;
                _currentTabItem = null;
                _currentTabItemIndex = null;
                _currentGrid = null;
                _currentPlot = null;
            }
        }

        static public int? CurrentTabItemIndex
        {
            get { return _currentTabItemIndex; }
            set
            {
                if (_currentWindow != null)
                {
                    _currentWindow.Focus();
                    _tabItemDictionary = null;
                    // Ensure a TabControl exists
                    if ((_currentWindow.Content == null) || (_currentWindow.Content.GetType() != typeof(TabControl))
                        || !(_tabItemDictionaryLookup.TryGetValue((TabControl)(_currentWindow.Content), out _tabItemDictionary)))
                    {
                        var newTabControl = new TabControl();
                        newTabControl.Background = Brushes.White;
                        _currentWindow.Content = newTabControl;
                        // Create Dictionary to map int to TabItem for the new Control
                        _tabItemDictionary = new Dictionary<int?, TabItem>();
                        _tabItemDictionaryLookup.Add(newTabControl, _tabItemDictionary);
                    }
                    var currentTabControl = (TabControl)(_currentWindow.Content);
                    _tabItemDictionary = _tabItemDictionaryLookup[currentTabControl];
                    _currentTabItem = null;

                    if (value == null || _tabItemDictionary.TryGetValue(value, out _currentTabItem))
                    {
                        // TabItem exists
                        _currentTabItemIndex = value;
                        if (_currentTabItem != null) _currentTabItem.Focus();

                        // If the TabItem contains a Grid of plots, select the minimum index
                        _plotDictionary = null;
                        if (_currentTabItem != null && CurrentTabItem.Content != null && CurrentTabItem.Content.GetType() == typeof(Grid)
                            && _plotDictionaryLookup.TryGetValue((Grid)(CurrentTabItem.Content), out _plotDictionary))
                        {
                            _currentGrid = (Grid)(CurrentTabItem.Content);
                            CurrentPlotIndex = _plotDictionary.Keys.Min();
                        }

                        // If the TabItem contains a plot, set this to be the current plot
                        if (_currentTabItem != null && CurrentTabItem.Content != null && CurrentTabItem.Content.GetType() == typeof(Plot2D))
                        {
                            _currentPlot = (Plot2D)(_currentTabItem.Content);
                            _currentPlotIndex = null;
                        }

                    }
                    else
                    {
                        // Create new TabItem
                        _currentTabItem = new TabItem();
                        _currentTabItem.Header = "Tab " + value;
                        currentTabControl.Items.Add(_currentTabItem);
                        _tabItemDictionary.Add(value, _currentTabItem);
                        _currentTabItemIndex = value;
                        _currentTabItem.Focus();
                        _currentGrid = null; 
                        _plotDictionary = null;
                    }
                }
            }
        }

        static public int? CurrentPlotIndex
        {
            get { return _currentPlotIndex; }
            set
            {
                if (_currentGrid == null)
                {
                    throw new Exception("No Grid or GridSplitter element present");
                }
                var total = ((Grid)_currentGrid).ColumnDefinitions.Count * ((Grid)_currentGrid).RowDefinitions.Count;
                if (value < 0 || value > total - 1)
                {
                    throw new Exception("Index out of range");
                }
                _currentPlotIndex = value;
                FrameworkElement plot = null;
                if (_plotDictionary != null)
                {
                    if (_plotDictionary.TryGetValue(_currentPlotIndex, out plot)) _currentPlot = (Plot2D)plot;
                }
            }
        }

        /// <summary>
        /// Add a subplot (Grid) to the current Window or TabItem 
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="columns"></param>
        static public void AddSubPlot(int rows, int columns)
        {
            var newGrid = new Grid();
            ColumnDefinition col;
            RowDefinition row;
            for (var i = 0; i < rows; ++i)
            {
                row = new RowDefinition(); row.Height = new GridLength(1, GridUnitType.Star);
                newGrid.RowDefinitions.Add(row);
            }
            for (var i = 0; i < columns; ++i)
            {
                col = new ColumnDefinition(); col.Width = new GridLength(1, GridUnitType.Star);
                newGrid.ColumnDefinitions.Add(col);
            }
            var newPlotDictionary = new Dictionary<int?, FrameworkElement>();
            if (_currentTabItem != null)
            {
                _currentTabItem.Content = newGrid;
                _plotDictionaryLookup.Remove(_currentTabItem);
                
            }
            else if (_currentWindow != null)
            {
                _currentWindow.Content = newGrid;
                _plotDictionaryLookup.Remove(_currentWindow);
            }
            else
            {
                OpenNextWindow();
                _currentWindow.Content = newGrid;
            }
            _plotDictionaryLookup.Add(_currentWindow, newPlotDictionary);
            _plotDictionary = newPlotDictionary;
            _currentGrid = newGrid;
            _currentPlotIndex = 0;
        }

        static public void AddPlot(FrameworkElement plot)
        {
            // If there is a currentGrid then add to this
            if (_currentGrid != null)
            {
                FrameworkElement oldPlot = null;
                if (_plotDictionary.TryGetValue(_currentPlotIndex, out oldPlot))
                {
                    ((Grid)_currentGrid).Children.Remove(oldPlot);
                    _plotDictionary.Remove(_currentPlotIndex);
                }
                ((Grid)_currentGrid).Children.Add(plot);
                _plotDictionary.Add(_currentPlotIndex, plot);
                var columns = ((Grid)_currentGrid).ColumnDefinitions.Count;
                plot.SetValue(Grid.ColumnProperty, _currentPlotIndex % columns);
                plot.SetValue(Grid.RowProperty, _currentPlotIndex / columns);
            }
            // otherwise, if there is a currentTabItem then add to this
            else if (_currentTabItem != null)
            {
                _currentTabItem.Content = plot;
            }
            // otherwise, if there is a currentWindow then add to this
            else if (_currentWindow != null)
            {
                _currentWindow.Content = plot;
            }
            _currentPlot = plot;
        }

        //GridSplitter 
    }
}
