using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IronPlot
{
    public class AxesFrame : ContentControl
    {
        private readonly AxisCanvas _canvas;

        private readonly Path _frame;
        public Path Frame => _frame;
        readonly StreamGeometry _geometry = new StreamGeometry(); 

        public AxesFrame()
        {
            _canvas = new AxisCanvas();
            Content = _canvas;
            _frame = new Path { Stroke = Brushes.Black, StrokeThickness = 1, StrokeLineJoin = PenLineJoin.Miter, Data = _geometry };
            _canvas.Children.Add(_frame);
        }

        internal void Render(Rect position)
        {
            var context = _geometry.Open();
            var contextPoint = new Point(position.X, position.Y);
            context.BeginFigure(contextPoint, false, true);
            contextPoint.Y = contextPoint.Y + position.Height; context.LineTo(contextPoint, true, false);
            contextPoint.X = contextPoint.X + position.Width; context.LineTo(contextPoint, true, false);
            contextPoint.Y = contextPoint.Y - position.Height; context.LineTo(contextPoint, true, false);
            context.Close();
        }

    }
}
