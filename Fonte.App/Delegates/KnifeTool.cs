﻿// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Fonte.App.Controls;
using Fonte.App.Interfaces;
using Fonte.App.Utilities;
using Fonte.Data.Interfaces;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System.Numerics;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;


namespace Fonte.App.Delegates
{

    public class KnifeTool : BaseTool
    {
        private Point? _origin;
        private Point _anchor;
        private Vector2[] _points;
        private bool _shouldMoveOrigin;

        private IChangeGroup _undoGroup;

        protected override CoreCursor DefaultCursor { get; } = Cursors.Knife;

        public override void OnDraw(DesignCanvas canvas, DrawEventArgs args)
        {
            base.OnDraw(canvas, args);

            if (_points != null)
            {
                var ds = args.DrawingSession;
                var rescale = args.InverseScale;

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
                _undoGroup.Dispose();
                _undoGroup = null;
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

                _points = Slicing.IntersectPaths(canvas.Layer, _origin.Value.ToVector2(), _anchor.ToVector2());
                canvas.Invalidate();
            }

            Cursor = args.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu) ? Cursors.KnifeWithPlus : DefaultCursor;
            canvas.InvalidateCursor();
        }

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            if (_points != null)
            {
                var layer = canvas.Layer;

                layer.ClearSelection();
                Slicing.SlicePaths(layer, _origin.Value.ToVector2(), _anchor.ToVector2(),
                                    breakPaths: !args.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu));
            }

            base.OnPointerReleased(canvas, args);
        }

        protected override void CompleteMove(DesignCanvas canvas)
        {
            base.CompleteMove(canvas);

            if (_undoGroup != null)
            {
                _undoGroup.Dispose();

                ((App)Application.Current).InvalidateData();
            }
            _origin = null;
            _points = null;
        }

        #region IToolBarEntry implementation

        public override IconSource Icon { get; } = new FontIconSource() { FontSize = 16, Glyph = "\uf406" };

        public override string Name => "Knife";

        public override KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator() { Key = VirtualKey.E };

        #endregion
    }
}
