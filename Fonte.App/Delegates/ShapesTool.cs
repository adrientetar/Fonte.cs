/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Fonte.Data.Interfaces;
    using Microsoft.Graphics.Canvas;

    using System;
    using System.Collections.Generic;
    using Windows.Foundation;
    using Windows.System;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public class ShapesTool : BaseTool
    {
        private Point? _origin;
        private Point _anchor;

        private bool _drawRectangle;
        private bool _linkAxes;
        private bool _originAtCenter;
        private bool _shouldMoveOrigin;

        private IChangeGroup _undoGroup;

        public override CoreCursor Cursor { get; } = new CoreCursor(CoreCursorType.Cross, 0);

        Rect Rectangle
        {
            get
            {
                var origin = _origin.Value;
                var x1 = origin.X;
                var y1 = origin.Y;
                var x2 = _anchor.X;
                var y2 = _anchor.Y;
                if (_linkAxes)
                {
                    var dx = x2 - x1;
                    var dy = y2 - y1;
                    if (Math.Abs(dx) > Math.Abs(dy))
                    {
                        y2 = y1 + Math.Sign(dy) * dx;  //Math.CopySign(x2 - x1, y2 - y1); // .NET Core 3.0
                    }
                    else
                    {
                        x2 = x1 + Math.Sign(dx) * dy;
                    }
                }
                if (_originAtCenter)
                {
                    x1 = 2 * x1 - x2;
                    y1 = 2 * y1 - y2;
                }
                if (x1 > x2)
                    (x1, x2) = (x2, x1);
                if (y1 > y2)
                    (y1, y2) = (y2, y1);
                return new Rect(new Point(x1, y1), new Point(x2, y2));
            }
        }

        public ShapesTool()
        {
        }

        public override void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
            if (_origin.HasValue)
            {
                // XXX: need to fetch the strokeColor here
                var color = Color.FromArgb(255, 34, 34, 34);
                var rect = Rectangle;
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
            else if (e.Key == VirtualKey.Shift)
            {
                _linkAxes = true;
            }
            else if (e.Key == VirtualKey.Control)
            {
                _originAtCenter = true;
            }
            else if (e.Key == VirtualKey.Space)
            {
                _shouldMoveOrigin = true;
            }
            else if (e.Key == VirtualKey.Escape)
            {
                _origin = null;
            }
            else
            {
                return;
            }

            e.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        public override void OnKeyUp(DesignCanvas canvas, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Menu)
            {
                _drawRectangle = false;
            }
            else if (e.Key == VirtualKey.Shift)
            {
                _linkAxes = false;
            }
            else if (e.Key == VirtualKey.Control)
            {
                _originAtCenter = false;
            }
            else if (e.Key == VirtualKey.Space)
            {
                _shouldMoveOrigin = false;
            }
            else
            {
                return;
            }

            e.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        public override void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(canvas, e);

            if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
            {
                _undoGroup = canvas.Layer.CreateUndoGroup();
                _origin = _anchor = canvas.GetLocalPosition(e);

                canvas.Layer.ClearSelection();
                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerMoved(canvas, e);

            if (_origin.HasValue)
            {
                var pos = canvas.GetLocalPosition(e);
                if (_shouldMoveOrigin)
                {
                    var origin = _origin.Value;
                    origin.X += pos.X - _anchor.X;
                    origin.Y += pos.Y - _anchor.Y;
                    _origin = origin;
                }
                _anchor = pos;

                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(canvas, e);

            if (_origin.HasValue && _anchor != _origin.Value)
            {
                var rect = Rectangle;
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
                path.Select();

                _undoGroup.Dispose();
                _undoGroup = null;
                ((App)Application.Current).InvalidateData();
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
