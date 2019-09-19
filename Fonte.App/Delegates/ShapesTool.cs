/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Fonte.App.Utilities;
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

        protected override CoreCursor DefaultCursor { get; } = Cursors.CrossWithEllipse;

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
                        y2 = y1 + Math.Sign(dy) * dx;
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
                return new Rect(new Point(
                        Outline.RoundToGrid((float)x1),
                        Outline.RoundToGrid((float)y1)
                    ), new Point(
                        Outline.RoundToGrid((float)x2),
                        Outline.RoundToGrid((float)y2)
                    ));
            }
        }

        public override void OnDisabled(DesignCanvas canvas)
        {
            base.OnDisabled(canvas);

            _origin = null;
            canvas.Invalidate();
        }

        public override void OnDrawCompleted(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
            if (_origin.HasValue && _anchor != _origin.Value)
            {
                var color = (Color)FindResource(canvas, DesignCanvas.StrokeColorKey);
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

        public override void OnKeyDown(DesignCanvas canvas, KeyRoutedEventArgs args)
        {
            if (args.Key == VirtualKey.Menu)
            {
                _drawRectangle = true;

                Cursor = Cursors.CrossWithRectangle;
                canvas.InvalidateCursor();
            }
            else if (args.Key == VirtualKey.Shift)
            {
                _linkAxes = true;
            }
            else if (args.Key == VirtualKey.Control)
            {
                _originAtCenter = true;
            }
            else if (args.Key == VirtualKey.Space && _origin.HasValue)
            {
                _shouldMoveOrigin = true;
            }
            else if (args.Key == VirtualKey.Escape && _origin.HasValue)
            {
                _undoGroup.Reset();
                _undoGroup.Dispose();
                _undoGroup = null;
                _origin = null;
            }
            else
            {
                base.OnKeyDown(canvas, args);
                return;
            }

            args.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        public override void OnKeyUp(DesignCanvas canvas, KeyRoutedEventArgs args)
        {
            if (args.Key == VirtualKey.Menu)
            {
                _drawRectangle = false;

                Cursor = DefaultCursor;
                canvas.InvalidateCursor();
            }
            else if (args.Key == VirtualKey.Shift)
            {
                _linkAxes = false;
            }
            else if (args.Key == VirtualKey.Control)
            {
                _originAtCenter = false;
            }
            else if (args.Key == VirtualKey.Space)
            {
                _shouldMoveOrigin = false;
            }
            else
            {
                base.OnKeyUp(canvas, args);
                return;
            }

            args.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        public override void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerPressed(canvas, args);

            var ptPoint = args.GetCurrentPoint(canvas);
            if (ptPoint.Properties.IsLeftButtonPressed && canvas.Layer is Data.Layer layer)
            {
                _undoGroup = layer.CreateUndoGroup();
                _origin = _anchor = canvas.FromClientPosition(ptPoint.Position);

                layer.ClearSelection();
                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerMoved(canvas, args);

            if (_origin.HasValue)
            {
                var pos = canvas.FromClientPosition(args.GetCurrentPoint(canvas).Position);
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

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerReleased(canvas, args);

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

                    var dot225dx = Outline.RoundToGrid(.225f * dx);
                    var dot5dx = Outline.RoundToGrid(.5f * dx);
                    var dot775dx = Outline.RoundToGrid(.775f * dx);
                    var dot225dy = Outline.RoundToGrid(.225f * dy);
                    var dot5dy = Outline.RoundToGrid(.5f * dy);
                    var dot775dy = Outline.RoundToGrid(.775f * dy);

                    path = new Data.Path(
                        new List<Data.Point>() {
                            new Data.Point(x1 + dot225dx, y2),
                            new Data.Point(x1, y1 + dot775dy),
                            new Data.Point(x1, y1 + dot5dy, Data.PointType.Curve, isSmooth: true),
                            new Data.Point(x1, y1 + dot225dy),
                            new Data.Point(x1 + dot225dx, y1),
                            new Data.Point(x1 + dot5dx, y1, Data.PointType.Curve, isSmooth: true),
                            new Data.Point(x1 + dot775dx, y1),
                            new Data.Point(x2, y1 + dot225dy),
                            new Data.Point(x2, y1 + dot5dy, Data.PointType.Curve, isSmooth: true),
                            new Data.Point(x2, y1 + dot775dy),
                            new Data.Point(x1 + dot775dx, y2),
                            new Data.Point(x1 + dot5dx, y2, Data.PointType.Curve, isSmooth: true),
                        }
                    );
                }
                canvas.Layer.Paths.Add(path);
                path.IsSelected = true;

                _undoGroup.Dispose();
                _undoGroup = null;
                ((App)Application.Current).InvalidateData();
            }
            _origin = null;
        }

        #region IToolBarEntry implementation

        public override IconSource Icon { get; } = new FontIconSource() { FontSize = 16, Glyph = "\uf158" };

        public override string Name => "Shapes";

        public override KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator() { Key = VirtualKey.S };

        #endregion
    }
}
