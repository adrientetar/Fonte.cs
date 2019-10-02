/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Fonte.App.Utilities;
    using Fonte.Data.Utilities;
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Text;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Windows.Foundation;
    using Windows.System;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public class RulerTool : BaseTool
    {
        private Point? _origin;
        private Point _anchor;
        private IList<Vector2> _points;
        private bool _shouldMoveOrigin;

        const float PI_1_2 = .5f * MathF.PI;

        protected override CoreCursor DefaultCursor { get; } = Cursors.Ruler;

        public override object FindResource(DesignCanvas canvas, object resourceKey)
        {
            var key = (string)resourceKey;
            if (key == DesignCanvas.DrawCoordinatesKey)
            {
                return true;
            }

            return base.FindResource(canvas, resourceKey);
        }

        public override void OnActivated(DesignCanvas canvas)
        {
            base.OnActivated(canvas);

            canvas.Invalidate();
        }

        public override void OnDisabled(DesignCanvas canvas)
        {
            base.OnDisabled(canvas);

            canvas.Invalidate();
        }

        public override void OnDrawCompleted(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
            if (_points != null)
            {
                var backplateColor = Color.FromArgb(210, 60, 121, 100);
                var color = Color.FromArgb(170, 60, 121, 100);
                var radius = 3.5f * rescale;

                var origin = _origin.Value.ToVector2();
                var anchor = _anchor.ToVector2();

                foreach (var point in _points)
                {
                    ds.FillCircle(point, radius, color);
                }
                ds.DrawLine(origin, anchor, Color.FromArgb(170, 100, 161, 140), strokeWidth: rescale);

                foreach (var (p1, p2) in _points.Zip(_points.Skip(1), (one, two) => (one, two)))
                {
                    var delta = p2 - p1;
                    var length = MathF.Round(delta.Length(), 1);

                    if (length > 0)
                    {
                        Drawing.DrawText(ds, length.ToString(), p1 + .5f * delta, Colors.White, rescale: rescale, fontSize: 11,
                                         backplateColor: backplateColor);
                    }
                }

                var vector = anchor - origin;
                var angle = Conversion.ToRadians(vector);
                var deg = Math.Round(Conversion.ToDegrees(angle), 1);

                CanvasHorizontalAlignment hAlignment;
                if (angle > PI_1_2 || angle < -PI_1_2)
                {
                    hAlignment = CanvasHorizontalAlignment.Right;
                }
                else
                {
                    hAlignment = CanvasHorizontalAlignment.Left;
                }

                CanvasVerticalAlignment vAlignment;
                if (angle < -PI_1_2)
                {
                    vAlignment = CanvasVerticalAlignment.Top;
                }
                else
                {
                    if (angle < 0) vector.Y = -vector.Y;
                    vAlignment = CanvasVerticalAlignment.Bottom;
                }

                Drawing.DrawText(ds, $"{deg}º", anchor + 20 * Vector2.Normalize(vector), Colors.White, rescale: rescale, fontSize: 11,
                                 hAlignment: hAlignment, vAlignment: vAlignment, backplateColor: backplateColor);
            }
        }

        public override void OnKeyDown(DesignCanvas canvas, KeyRoutedEventArgs args)
        {
            if (args.Key == VirtualKey.Space && _origin != null)
            {
                _shouldMoveOrigin = true;
            }
            else if (args.Key == VirtualKey.Escape && _origin != null)
            {
                _origin = null;
                _points = null;

                canvas.Invalidate();
            }
            else
            {
                base.OnKeyDown(canvas, args);
                return;
            }

            args.Handled = true;
        }

        public override void OnKeyUp(DesignCanvas canvas, KeyRoutedEventArgs args)
        {
            if (args.Key == VirtualKey.Space)
            {
                _shouldMoveOrigin = false;
            }
            else
            {
                base.OnKeyUp(canvas, args);
                return;
            }

            args.Handled = true;
        }

        public override void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerPressed(canvas, args);

            var ptPoint = args.GetCurrentPoint(canvas);
            if (ptPoint.Properties.IsLeftButtonPressed)
            {
                _origin = _anchor = canvas.FromClientPosition(ptPoint.Position);

                canvas.Invalidate();
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerMoved(canvas, args);

            if (_origin.HasValue)
            {
                var pos = canvas.FromClientPosition(args.GetCurrentPoint(canvas).Position);
                if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
                {
                    pos = ClampToOrigin(pos, _origin.Value);
                }

                if (_shouldMoveOrigin)
                {
                    _origin = new Point(
                        _origin.Value.X + pos.X - _anchor.X,
                        _origin.Value.Y + pos.Y - _anchor.Y);
                }
                _anchor = pos;

                _points = PointsWithEndpoints(canvas.Layer, addIntersections: !args.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu));
                canvas.Invalidate();
            }
        }

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerReleased(canvas, args);

            _origin = null;
            _points = null;

            canvas.Invalidate();
        }

        IList<Vector2> PointsWithEndpoints(Data.Layer layer, bool addIntersections = true)
        {
            var origin = _origin.Value.ToVector2();
            var anchor = _anchor.ToVector2();

            return addIntersections switch
            {
                true  => Enumerable.Concat(Slicing.IntersectPaths(layer, origin, anchor), new Vector2[] { origin, anchor })
                                   .OrderBy(point => (point - origin).LengthSquared())
                                   .ToArray(),
                false => new Vector2[] { origin, anchor }
            };
        }

        #region IToolBarEntry implementation

        public override IconSource Icon { get; } = new FontIconSource() { FontSize = 16, Glyph = "\uecc6" };

        public override string Name => "Ruler";

        public override KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator() { Key = VirtualKey.R };

        #endregion
    }
}
