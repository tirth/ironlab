// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace IronPlot
{
    public abstract class DirectControl : Panel
    {
        ImageBrush _sceneImage;
        protected DirectImage DirectImage;
        protected Rectangle Rectangle;
        readonly TextBlock _messageBlock;

        readonly bool _initializationFailed;
        public bool InitializationFailed => _initializationFailed;

        // The D3DImage seems, under some cirmstances, not to reaquire its front buffer after
        // loss due to a ctrl-alt-del, screen saver etc. For robustness, a timer checks for a
        // lost front buffer.
        protected DispatcherTimer FrontBufferCheckTimer = new DispatcherTimer();

        public DirectControl()
        {
            Rectangle = new Rectangle();
            try
            {
                CreateDirectImage();
                //throw new Exception("Artificial exception");
            }
            catch (Exception e)
            {
                // In case of error, just display message on Control
                _messageBlock = new TextBlock { Margin = new Thickness(5), Foreground = Brushes.Red };
                _messageBlock.Text = "Error initializing DirectX control: " +  e.Message;
                _messageBlock.Text += "\rTry installing latest DirectX End-User Runtime";
                Children.Add(_messageBlock);
                DirectImage = null;
                _initializationFailed = true;
                return;
            }
            DirectImage.RenderRequested += directImage_RenderRequested;
            Children.Add(Rectangle);
            RecreateImage();
            IsVisibleChanged += DirectControl_IsVisibleChanged;
            FrontBufferCheckTimer.Tick += frontBufferCheckTimer_Tick;
            FrontBufferCheckTimer.Interval = TimeSpan.FromSeconds(0.5);
            FrontBufferCheckTimer.Start();
        }

        void directImage_RenderRequested(object sender, EventArgs e)
        {
            InvalidateVisual();
        }

        protected abstract void CreateDirectImage();

        void frontBufferCheckTimer_Tick(object sender, EventArgs e)
        {
            CheckImage();
        }

        void CheckImage()
        {
            if (!_initializationFailed && (DirectImage.D3DImage == null || !DirectImage.D3DImage.IsFrontBufferAvailable))
            {
                RecreateImage();
                DirectImage.RenderScene();
                InvalidateMeasure();
            }
        }

        public void RecreateImage()
        {
            DirectImage.RecreateD3DImage(true);
            _sceneImage = DirectImage.ImageBrush;
            Rectangle.Fill = _sceneImage;
            _sceneImage.TileMode = TileMode.None;
        }

        public void RequestRender()
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (!_initializationFailed)
            {
                CheckImage();
                DirectImage.RenderScene();
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var random = new Random();
            Rectangle.Measure(availableSize);
            if (DirectImage == null) _messageBlock.Measure(availableSize);
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var width = finalSize.Width;
            var height = finalSize.Height;
            Rectangle.Arrange(new Rect(0, 0, width, height));
            if (DirectImage != null)
            {
                if (DirectImage.D3DImage != null) DirectImage.SetImageSize((int)width, (int)height, 96);
            }
            else _messageBlock.Arrange(new Rect(0, 0, width, height));
            return finalSize;
        }

        void DirectControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            DirectImage.Visible = (bool)e.NewValue;
            if (DirectImage.Visible)
            {
                OnVisibleChanged_Visible();
                if (Parent is FrameworkElement) (Parent as FrameworkElement).InvalidateMeasure();
                FrontBufferCheckTimer.Start();
                DirectImage.RegisterWithService();
            }
            else
            {
                OnVisibleChanged_NotVisible();
                FrontBufferCheckTimer.Stop();
                DirectImage.UnregisterWithService();
            }
        }

        protected abstract void OnVisibleChanged_Visible();

        protected abstract void OnVisibleChanged_NotVisible();
    }
}

