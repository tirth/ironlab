using System;
using System.Collections.Generic;
using System.Windows.Threading;
using SharpDX;
using SharpDX.Direct2D1;

namespace IronPlot
{
    public class Direct2DImage : DirectImage
    {
        internal List<DirectPath> Paths;

        readonly DispatcherTimer _timer = new DispatcherTimer(); 

        public Direct2DImage()
        {
            CreateDevice(SurfaceType.Direct2D);
            Paths = new List<DirectPath>();
            _timer.Interval = TimeSpan.FromSeconds(0.1);
            _timer.Tick += timer_Tick;
            _timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            //if (!this.d3dImage.IsFrontBufferAvailable)
            //{
            //
            //}
        }

        protected override void Initialize()
        {
            
        }

        protected override void Draw()
        {
            RenderTarget.BeginDraw();
            RenderTarget.Transform = Matrix3x2.Identity;
            var random = new Random();
            RenderTarget.Clear(new Color4(0.5f, 0.5f, 0.5f, 0.0f));
            RenderTarget.AntialiasMode = AntialiasMode.Aliased;
            var properties = new StrokeStyleProperties {LineJoin = LineJoin.MiterOrBevel};
            var strokeStyle = new StrokeStyle(RenderTarget.Factory, properties);
            foreach (var path in Paths)
            {
                if (path.Geometry != null && path.Brush != null)
                {
                    if (path is DirectPathScatter)
                    {
                        (path as DirectPathScatter).RenderScatterGeometry(RenderTarget);
                    }
                    else
                    {
                        if (path.QuickStrokeDash != QuickStrokeDash.None)
                        {
                            RenderTarget.DrawGeometry(path.Geometry, path.Brush, (float)path.StrokeThickness, strokeStyle);
                        }
                    }
                }
            }
            RenderTarget.EndDraw();
            GraphicsDeviceService10.CopyTextureAcross();
            GraphicsDeviceService10.Device.Flush();
        }
    }
}
