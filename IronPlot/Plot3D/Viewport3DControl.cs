using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace IronPlot.Plotting3D
{
    public class Viewport3DControl : DirectControl
    {
        public ViewportImage Viewport3DImage => DirectImage as ViewportImage;

        readonly Canvas _canvas;
        public Canvas Canvas => _canvas;

        public Viewport3DControl()
        {
            _canvas = new Canvas { ClipToBounds = true, Background = Brushes.Transparent };
            if (DirectImage == null) return; // in the event of an exception.
            Children.Add(_canvas);
            DirectImage.Canvas = _canvas;
            // TODO change this!
            Rectangle.SizeChanged += DirectImage.OnSizeChanged;
            (DirectImage as ViewportImage).RenderRequested += directImage_RenderRequested;
        }

        protected override void CreateDirectImage()
        {
            DirectImage = new ViewportImage();
        }

        void directImage_RenderRequested(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Transparent, null,
                new Rect(0, 0, RenderSize.Width, RenderSize.Height));
            if (DirectImage != null) DirectImage.RenderScene();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            base.MeasureOverride(availableSize);
            _canvas.Measure(availableSize);
            return _canvas.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var size = base.ArrangeOverride(finalSize);
            var width = size.Width;
            var height = size.Height;
            _canvas.Arrange(new Rect(0, 0, width, height));
            return size;
        }

        protected override void OnVisibleChanged_Visible()
        {
        }

        protected override void OnVisibleChanged_NotVisible()
        {
        }
    }
}

