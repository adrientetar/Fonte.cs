/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Fonte.App.Utilities;
    using Fonte.Data.Interfaces;
    using Fonte.Data.Utilities;
    using Microsoft.Graphics.Canvas;

    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
    using Windows.Foundation;
    using Windows.System;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public class KnifeTool : BaseTool
    {
        private Point? _origin;
        private Point _anchor;
        private Vector2[] _points;
        private bool _shouldMoveOrigin;

        private IChangeGroup _undoGroup;

        protected override CoreCursor DefaultCursor { get; } = Cursors.Knife;

        public override void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
            if (_points != null)
            {
                var color = Color.FromArgb(120, 38, 38, 38);
                var radius = 3.5f * rescale;

                foreach (var point in _points)
                {
                    ds.FillCircle(point, radius, color);
                }
                ds.DrawLine(_origin.Value.ToVector2(), _anchor.ToVector2(), Color.FromArgb(120, 60, 60, 60), strokeWidth: rescale);
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
                _points = null;
                _undoGroup.Dispose();
                _undoGroup = null;
                _origin = null;

                ((App)Application.Current).InvalidateData();
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
                var layer = canvas.Layer;

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

                _points = IntersectPaths(canvas.Layer, _origin.Value.ToVector2(), _anchor.ToVector2());
                canvas.Invalidate();
            }
        }

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerReleased(canvas, args);

            if (_origin != null)
            {
                if (_points != null)
                {
                    var layer = canvas.Layer;

                    layer.ClearSelection();
                    Slicing.SlicePaths(layer, _origin.Value.ToVector2(), _anchor.ToVector2());

                    ((App)Application.Current).InvalidateData();
                }
                _undoGroup.Dispose();
                _undoGroup = null;
            }
            _origin = null;
            _points = null;

            canvas.Invalidate();
        }

        static Vector2[] IntersectPaths(Data.Layer layer, Vector2 p0, Vector2 p1)
        {
            var points = new List<Vector2>();

            foreach (var path in layer.Paths)
            {
                foreach (var segment in path.Segments)
                {
                    foreach (var loc in segment.IntersectLine(p0, p1))
                    {
                        points.Add(loc.Item1);
                    }
                }
            }
            return points.ToArray();
        }

        #region IToolBarEntry implementation

        public override IconSource Icon { get; } = new FontIconSource() { FontSize = 16, Glyph = "\ue7e6" };

        public override string Name => "Knife";

        public override KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator() { Key = VirtualKey.E };

        #endregion
    }
}
