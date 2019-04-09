/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Microsoft.Graphics.Canvas;

    using System.Collections.Generic;
    using Windows.Foundation;
    using Windows.System;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public class ShapesTool : BaseTool
    {
        private Point? _origin;
        private Point _anchor;

        private bool _drawRectangle;

        public override CoreCursor Cursor { get; } = new CoreCursor(CoreCursorType.Cross, 0);

        public ShapesTool()
        {
        }

        public override void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
            if (_origin.HasValue)
            {
                // XXX: need to fetch the strokeColor here
                var color = Color.FromArgb(255, 34, 34, 34);
                var rect = new Rect(_origin.Value, _anchor);
                if (_drawRectangle)
                {
                    ds.DrawRectangle(rect, color, strokeWidth: rescale);
                }
                else
                {
                    ds.DrawEllipse(
                        .5f * (float)(rect.Left + rect.Right),
                        .5f * (float)(rect.Top + rect.Bottom),
                        .5f * (float)rect.Width,
                        .5f * (float)rect.Height,
                        color,
                        strokeWidth: rescale
                    );
                }
            }
        }

        public override void OnKeyDown(DesignCanvas canvas, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Menu)
            {
                _drawRectangle = true;
            }
            else
            {
                return;
            }

            e.Handled = true;
            canvas.Invalidate();
        }

        public override void OnKeyUp(DesignCanvas canvas, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Menu)
            {
                _drawRectangle = false;
            }
            else
            {
                return;
            }

            e.Handled = true;
            canvas.Invalidate();
        }

        public override void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(canvas, e);

            if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
            {
                _origin = _anchor = canvas.GetLocalPosition(e);
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerMoved(canvas, e);

            if (_origin.HasValue)
            {
                _anchor = canvas.GetLocalPosition(e);
                canvas.Invalidate();
            }
        }

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(canvas, e);

            if (_origin.HasValue && _anchor != _origin.Value)
            {
                var rect = new Rect(_origin.Value, _anchor);
                var x1 = (float)rect.Left;
                var y1 = (float)rect.Top;
                var x2 = (float)rect.Right;
                var y2 = (float)rect.Bottom;

                Data.Path path;
                if (_drawRectangle)
                {
                    path = new Data.Path(
                        new List<Data.Point>() {
                            new Data.Point(x1, y1, Data.PointType.Line),
                            new Data.Point(x2, y1, Data.PointType.Line),
                            new Data.Point(x2, y2, Data.PointType.Line),
                            new Data.Point(x1, y2, Data.PointType.Line),
                        }
                    );
                }
                else
                {
                    var dx = x2 - x1;
                    var dy = y2 - y1;
                    path = new Data.Path(
                        new List<Data.Point>() {
                            new Data.Point(x1 + .225f * dx, y2),
                            new Data.Point(x1, y1 + .775f * dy),
                            new Data.Point(x1, y1 + .5f * dy, Data.PointType.Curve, smooth: true),
                            new Data.Point(x1, y1 + .225f * dy),
                            new Data.Point(x1 + .225f * dx, y1),
                            new Data.Point(x1 + .5f * dx, y1, Data.PointType.Curve, smooth: true),
                            new Data.Point(x1 + .775f * dx, y1),
                            new Data.Point(x2, y1 + .225f * dy),
                            new Data.Point(x2, y1 + .5f * dy, Data.PointType.Curve, smooth: true),
                            new Data.Point(x2, y1 + .775f * dy),
                            new Data.Point(x1 + .775f * dx, y2),
                            new Data.Point(x1 + .5f * dx, y2, Data.PointType.Curve, smooth: true),
                        }
                    );
                }
                canvas.Layer.Paths.Add(path);

                canvas.Invalidate();
            }
            _origin = null;
        }

        #region IToolBarEntry implementation

        public override IconElement Icon { get; } = new FontIcon() { Glyph = "\uf158" };

        public override string Name => "Shapes";

        public override KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator() { Key = VirtualKey.S };

        #endregion
    }
}
