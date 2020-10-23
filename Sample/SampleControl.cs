using SharpDX.Direct2D1;
using SharpDX.Mathematics.Interop;
using System;

namespace Sample
{
    internal class SampleControl : D2dControl.D2dControl
    {
        private float _x;
        private float _y;
        private const float W = 10;
        private const float H = 10;
        private float _dx = 1;
        private float _dy = 1;

        private readonly Random _rnd = new Random();

        public SampleControl()
        {
            ResourceCache.Add("RedBrush", t => new SolidColorBrush(t, new RawColor4(1.0f, 0.0f, 0.0f, 1.0f)));
            ResourceCache.Add("GreenBrush", t => new SolidColorBrush(t, new RawColor4(0.0f, 1.0f, 0.0f, 1.0f)));
            ResourceCache.Add("BlueBrush", t => new SolidColorBrush(t, new RawColor4(0.0f, 0.0f, 1.0f, 1.0f)));
        }

        public override void Render(RenderTarget target)
        {
            target.Clear(new RawColor4(1.0f, 1.0f, 1.0f, 1.0f));
            Brush? brush = null;
            switch (_rnd.Next(3))
            {
                case 0:
                    brush = ResourceCache["RedBrush"] as Brush;
                    break;
                case 1:
                    brush = ResourceCache["GreenBrush"] as Brush;
                    break;
                case 2:
                    brush = ResourceCache["BlueBrush"] as Brush;
                    break;
            }

            target.DrawRectangle(new RawRectangleF(_x, _y, _x + W, _y + H), brush);

            _x += _dx;
            _y += _dy;
            if (_x >= ActualWidth - W || _x <= 0)
            {
                _dx = -_dx;
            }

            if (_y >= ActualHeight - H || _y <= 0)
            {
                _dy = -_dy;
            }
        }
    }
}