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
    using Microsoft.Graphics.Canvas.Geometry;

    using System;
    using System.Diagnostics;
    using System.Numerics;
    using Windows.Foundation;
    using Windows.System;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public class SelectionTool : BaseTool
    {
        private Point _origin = EmptyPoint;
        private Point _anchor;
        private Point _screenOrigin;
        private bool _canDrag = false;
        private object _tappedItem;
        private (CanvasGeometry, CanvasGeometry) _oldPaths;
        private IChangeGroup _undoGroup;

        static readonly Point EmptyPoint = new Point(double.PositiveInfinity, double.NegativeInfinity);

        enum ActionType
        {
            None = 0,
            DraggingItem,
            DraggingRect
        };

        ActionType CurrentAction
        {
            get
            {
                if (_undoGroup != null)
                {
                    return ActionType.DraggingItem;
                }
                else if (_origin != EmptyPoint)
                {
                    return ActionType.DraggingRect;
                }
                return ActionType.None;
            }
        }

        public override object FindResource(DesignCanvas canvas, object resourceKey)
        {
            var key = (string)resourceKey;
            if (key == DesignCanvas.DrawSelectionBoundsKey)
            {
                return true;
            }

            return base.FindResource(canvas, resourceKey);
        }

        public override void OnActivated(DesignCanvas canvas)
        {
            ((App)Application.Current).InvalidateData();
        }

        public override void OnDisabled(DesignCanvas canvas)
        {
            ((App)Application.Current).InvalidateData();
        }

        public override void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
            if (CurrentAction == ActionType.DraggingItem && _tappedItem is ISelectable)
            {
                // to parametrize this, could do a GetResource(key) that uses App.Resources[key]
                // or just use FindResource, given that DesignCanvas takes on App's MergedDictionaries
                var color = Color.FromArgb(255, 210, 210, 210);
                ds.DrawGeometry(_oldPaths.Item1, color, strokeWidth: rescale);
                ds.DrawGeometry(_oldPaths.Item2, color, strokeWidth: rescale);
            }
            else if (CurrentAction == ActionType.DraggingRect)
            {
                ds.FillRectangle(new Rect(_origin, _anchor), Color.FromArgb(51, 0, 120, 215));
            }
        }

        public override void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerPressed(canvas, args);

            var ptPoint = args.GetCurrentPoint(canvas);
            if (ptPoint.Properties.IsLeftButtonPressed && canvas.Layer is Data.Layer layer)
            {
                var canvasPos = canvas.FromClientPosition(ptPoint.Position);
                _tappedItem = canvas.HitTest(canvasPos);

                if (_tappedItem != null)
                {
                    var alt = Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu);
                    if (_tappedItem is UIBroker.BBoxHandle ||
                        _tappedItem is UIBroker.GuidelineRule)
                    {
                        _screenOrigin = ptPoint.Position;
                        _undoGroup = layer.CreateUndoGroup();

                        Debug.Assert(CurrentAction == ActionType.DraggingItem);
                    }
                    else if (_tappedItem is Data.Segment segment && alt.HasFlag(CoreVirtualKeyStates.Down))
                    {
                        segment.ConvertTo(Data.PointType.Curve);

                        Debug.Assert(CurrentAction == ActionType.None);
                    }
                    else if (_tappedItem is ISelectable isel)
                    {
                        _screenOrigin = ptPoint.Position;
                        _oldPaths = (layer.ClosedCanvasPath, layer.OpenCanvasPath);

                        _origin = GetOriginPoint(isel, canvasPos);

                        if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
                        {
                            isel.IsSelected = !isel.IsSelected;
                        }
                        else
                        {
                            if (!isel.IsSelected)
                            {
                                layer.ClearSelection();
                                isel.IsSelected = true;
                            }
                        }
                        _undoGroup = layer.CreateUndoGroup();

                        Debug.Assert(CurrentAction == ActionType.DraggingItem);
                    }
                }
                else
                {
                    _origin = _anchor = canvasPos;

                    var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
                    if (!ctrl.HasFlag(CoreVirtualKeyStates.Down))
                    {
                        layer.ClearSelection();
                    }

                    Debug.Assert(CurrentAction == ActionType.DraggingRect);
                }

                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerMoved(canvas, args);

            if (CurrentAction == ActionType.None)
                return;

            var ptPoint = args.GetCurrentPoint(canvas);
            if (ptPoint.Properties.IsLeftButtonPressed)
            {
                var pos = canvas.FromClientPosition(ptPoint.Position);

                // Doing this has two purposes:
                // - avoid piling up unused changes in the undo group
                // - perform transformations (e.g. scaling projecting etc) from OG coordinates, free of intermediate roundings
                _undoGroup?.Reset();

                if (_tappedItem is UIBroker.BBoxHandle handle)
                {
                    if (!_canDrag)
                    {
                        _canDrag = CanStartDragging(ptPoint.Position, _screenOrigin, 1);
                    }
                    if (_canDrag)
                    {
                        var layer = canvas.Layer;
                        var bounds = layer.SelectionBounds;

                        var delta = pos.ToVector2() - handle.Position;
                        if (bounds.Width > 0)
                        {
                            var origin = Vector2.Zero;

                            var hr = 1f;
                            if (handle.Kind.HasFlag(UIBroker.HandleKind.Top))
                            {
                                hr = 1 + delta.Y / bounds.Height;
                                origin.Y = bounds.Bottom;
                            }
                            else if (handle.Kind.HasFlag(UIBroker.HandleKind.Bottom))
                            {
                                hr = 1 - delta.Y / bounds.Height;
                                origin.Y = bounds.Top;
                            }
                            var wr = 1f;
                            if (handle.Kind.HasFlag(UIBroker.HandleKind.Right))
                            {
                                wr = 1 + delta.X / bounds.Width;
                                origin.X = bounds.Left;
                            }
                            else if (handle.Kind.HasFlag(UIBroker.HandleKind.Left))
                            {
                                wr = 1 - delta.X / bounds.Width;
                                origin.X = bounds.Right;
                            }

                            layer.Transform(Matrix3x2.CreateScale(wr, hr, origin),
                                            selectionOnly: true);
                            Outline.RoundSelection(layer);
                        }
                    }
                }
                else if (_tappedItem is UIBroker.GuidelineRule rule)
                {
                    var guideline = rule.Guideline;
                    var angle = Math.Atan2(pos.Y - guideline.Y, pos.X - guideline.X);
                    var deg = (float)Conversion.ToDegrees(angle);

                    // TODO: we could always modulo 180 to normalize since 0-180 and 180-360 are equivalent
                    var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
                    if (shift.HasFlag(CoreVirtualKeyStates.Down))
                    {
                        guideline.Angle = (int)(Conversion.ToDegrees(angle) + Math.Sign(deg) * 45) / 90 * 90;
                    }
                    else
                    {
                        guideline.Angle = .1f * Outline.RoundToGrid(10f * deg);
                    }
                }
                else if (_tappedItem is ISelectable isel)
                {
                    if (!_canDrag)
                    {
                        _canDrag = isel.IsSelected && CanStartDragging(ptPoint.Position, _screenOrigin, 1);
                    }
                    if (_canDrag)
                    {
                        var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
                        if (shift.HasFlag(CoreVirtualKeyStates.Down))
                        {
                            pos = ClampToNodeOrOrigin(canvas.Layer, pos, isel as Data.Point);
                        }

                        Outline.MoveSelection(
                            canvas.Layer,
                            (float)(pos.X - _origin.X),
                            (float)(pos.Y - _origin.Y),
                            GetMoveMode()
                        );
                    }
                }
                else
                {
                    _anchor = pos;
                }

                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerReleased(canvas, args);

            if (CurrentAction == ActionType.DraggingItem)
            {
                if (_tappedItem is Data.Point)
                {
                    TryJoinPath(canvas, canvas.FromClientPosition(args.GetCurrentPoint(canvas).Position));
                }
                _undoGroup.Dispose();
                _undoGroup = null;
            }
            else if (CurrentAction == ActionType.DraggingRect)
            {
                var rect = new Rect(_origin, _anchor);

                foreach (var path in canvas.Layer.Paths)
                {
                    foreach (var point in path.Points)
                    {
                        if (rect.Contains(point.ToVector2().ToPoint()))
                        {
                            point.IsSelected = !point.IsSelected;
                        }
                    }
                }
            }
            else
            {
                return;
            }

            _screenOrigin = default;
            _origin = EmptyPoint;
            _canDrag = false;
            _tappedItem = null;
            ((App)Application.Current).InvalidateData();

            Debug.Assert(CurrentAction == ActionType.None);
        }

        public override void OnDoubleTapped(DesignCanvas canvas, DoubleTappedRoutedEventArgs args)
        {
            if (_tappedItem is Data.Anchor anchor)
            {
                canvas.EditAnchorName(anchor);
            }
            else if (_tappedItem is Data.Guideline guideline)
            {
                guideline.Angle = (float)Math.Round(guideline.Angle + 90) % 360;

                ((App)Application.Current).InvalidateData();
            }
            else if (_tappedItem is Data.Point point)
            {
                if (Outline.TryTogglePointSmoothness(point))
                {
                    ((App)Application.Current).InvalidateData();
                }
            }
            else if (_tappedItem is Data.Segment segment)
            {
                var path = segment.Parent;
                path.IsSelected = true;

                ((App)Application.Current).InvalidateData();
            }
        }

        /**/

        Point ClampToNodeOrOrigin(Data.Layer layer, Point pos, Data.Point point)
        {
            // We clamp to the mousedown pos, unless we have a single offcurve
            // in which case we clamp it against its parent.
            if (point != null && point.Type == Data.PointType.None && layer.Selection.Count == 1)
            {
                var path = point.Parent;
                var index = path.Points.IndexOf(point);
                var otherPoint = Sequence.PreviousItem(path.Points, index);
                if (otherPoint.Type == Data.PointType.None)
                {
                    otherPoint = Sequence.NextItem(path.Points, index);
                }
                if (otherPoint.Type != Data.PointType.None)
                {
                    return ClampToOrigin(pos, new Point(otherPoint.X, otherPoint.Y));
                }
            }
            return ClampToOrigin(pos, _origin);
        }

        Point GetOriginPoint(ISelectable item, Point pos)
        {
            if (item is Data.Component)
            {
                return pos;
            }
            else if (item is Data.Segment segment)
            {
                // XXX: avoid reprojecting?
                return segment.ProjectPoint(pos.ToVector2()).Value.ToPoint();
            }
            else if (item is ILocatable iloc)
            {
                return iloc.ToVector2().ToPoint();
            }
            throw new ArgumentException($"{item} is not allowed here.");
        }

        // TODO: also need to join when moving with keyboard
        void TryJoinPath(DesignCanvas canvas, Point pos)
        {
            if (_tappedItem is Data.Point point && Is.AtOpenBoundary(point))
            {
                // TODO: we could only HitTest points here
                var otherItem = canvas.HitTest(pos, ignoreElement: point);
                if (otherItem is Data.Point otherPoint && Is.AtOpenBoundary(otherPoint))
                {
                    Outline.JoinPaths(point.Parent,
                                      point.Parent.Points.IndexOf(point) != 0,
                                      otherPoint.Parent,
                                      point.Parent.Points.IndexOf(otherPoint) != 0,
                                      true);
                    ((App)Application.Current).InvalidateData();
                }
            }
        }

        #region IToolBarEntry implementation

        public override IconSource Icon { get; } = new FontIconSource() { FontSize = 16, Glyph = "\ue8b0" };

        public override string Name => "Selection";

        public override KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator() { Key = VirtualKey.V };

        #endregion
    }
}
