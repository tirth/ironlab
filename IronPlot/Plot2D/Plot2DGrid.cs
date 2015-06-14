using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace IronPlot
{
    public enum AlignmentDirection { Row, Column };

    public class AlignedAxes
    {
        public List<Axis2D> Axes;
        public AlignmentDirection AlignmentDirection;
        public int Position;
        public int Span;
    }

    public class Plot2DGrid : Grid
    {
        readonly List<AlignedAxes> _alignedAxesList = new List<AlignedAxes>();
        readonly List<PlotPanel> _plotPanelList = new List<PlotPanel>();

        protected override Size MeasureOverride(Size constraint)
        {
            FindPlotPanelsAndAlignedAxes();
            return base.MeasureOverride(constraint);
        }

        public void FindPlotPanelsAndAlignedAxes()
        {
            _plotPanelList.Clear();
            _alignedAxesList.Clear();
            foreach (UIElement element in Children)
            {
                if (element is Plot2D)
                {
                    var plotPanel = (element as Plot2D).PlotPanel;
                    _plotPanelList.Add(plotPanel);
                    var column = (int)element.GetValue(ColumnProperty);
                    var columnSpan = (int)element.GetValue(ColumnSpanProperty);
                    var row = (int)element.GetValue(RowProperty);
                    var rowSpan = (int)element.GetValue(RowSpanProperty);
                    GetAlignedAxes(AlignmentDirection.Row, row, rowSpan).Axes.AddRange(plotPanel.Axes.YAxes);
                    GetAlignedAxes(AlignmentDirection.Column, column, columnSpan).Axes.AddRange(plotPanel.Axes.XAxes);
                }
            }
        }

        public AlignedAxes GetAlignedAxes(AlignmentDirection alignmentDirection, int position, int span)
        {
            var axes = _alignedAxesList.Where(t => t.AlignmentDirection == alignmentDirection
                && t.Position == position && t.Span == span).DefaultIfEmpty(null).First();
            if (axes == null)
            {
                var newAxes = new AlignedAxes
                {
                    Axes = new List<Axis2D>(),
                    AlignmentDirection = alignmentDirection,
                    Position = position,
                    Span = span
                };
                _alignedAxesList.Add(newAxes);
                return newAxes;
            }
            return axes;
        }
        
        public void PlaceAxesFull()
        {
            var iter = 0;
            foreach (var axis in _plotPanelList.Select(t => t.Axes))
            {
                axis.UpdateAxisPositions();
            }
            while (iter <= 1)
            {
                // Step 1: for each PlotPanel in isolation, set the margins for each axis, given the currently visible
                // labels
                

                // Step 2: taking into account all aligned axes across all PlotPanels, 
                // increase axis margins, keeping the total axis length the same, or if this cannot be done
                // (axis length specified, or available space too little), change axis length. 

                // Step 3: take account of any equal axes, across all PlotPanels
                
                iter++;
            }
        }
    }
}
