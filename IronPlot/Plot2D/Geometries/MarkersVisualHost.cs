using System;
using System.Windows;
using System.Windows.Media;

namespace IronPlot
{
    public class MarkersVisualHost : FrameworkElement
    {
        readonly VisualCollection _visualChildren;

        readonly DrawingVisual _markers = new DrawingVisual();

        public MarkersVisualHost()
        {
            _visualChildren = new VisualCollection(this);
            UpdateMarkersVisual(null);
            _visualChildren.Add(_markers);
        }
        
        void UpdateMarkersVisual(Geometry geometry)
        {
            var context = _markers.RenderOpen();
            context.DrawRectangle(Brushes.Red, new Pen(Brushes.Red, 1), new Rect(30, 30, 30, 30));
            
            context.Close();
        }

        protected override int VisualChildrenCount => _visualChildren.Count;

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _visualChildren.Count)
                throw new ArgumentOutOfRangeException("index");

            return _visualChildren[index];
        }
    }
}
