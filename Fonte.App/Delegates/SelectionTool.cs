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
        private Point? _screenOrigin;
        private bool _canDrag = false;
        private object _tappedItem;
        private (CanvasGeometry, CanvasGeometry) _oldPaths;
        private IChangeGroup _undoGroup;

        static readonly Point EmptyPoint = new Point(double.PositiveInfinity, double.NegativeInfinity);

        enum ActionType
        {
            None = 0,
            DraggingPoint,
            DraggingRect
        };

        ActionType CurrentAction
        {
            get
            {
                if (_undoGroup != null)
                {
                    return ActionType.DraggingPoint;
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
            if (CurrentAction == ActionType.DraggingPoint)
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

        public override void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(canvas, e);

            var ptPoint = e.GetCurrentPoint(canvas);
            if (ptPoint.Properties.IsLeftButtonPressed)
            {
                var canvasPos = canvas.GetCanvasPosition(ptPoint.Position);
                _tappedItem = canvas.HitTest(canvasPos);

                if (_tappedItem == null)
                {
                    _origin = _anchor = canvasPos;

                    canvas.Layer.ClearSelection();

                    Debug.Assert(CurrentAction == ActionType.DraggingRect);
                }
                else
                {
                    if (_tappedItem is Data.Point ||
                        _tappedItem is Data.Segment)
                    {
                        _screenOrigin = ptPoint.Position;
                        _oldPaths = (canvas.Layer.ClosedCanvasPath, canvas.Layer.OpenCanvasPath);
                        _undoGroup = canvas.Layer.CreateUndoGroup();

                        if (_tappedItem is Data.Point point)
                        {
                            // Fix the origin to be the point coordinates, in case we start clamping against it with Shift.
                            _origin = _anchor = new Point(point.X, point.Y);

                            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
                            {
                                point.Selected = !point.Selected;
                            }
                            else
                            {
                                if (!point.Selected)
                                {
                                    canvas.Layer.ClearSelection();
                                    point.Selected = true;
                                }
                            }
                        }
                        else if (_tappedItem is Data.Segment segment)
                        {
                            // XXX: avoid reprojecting?
                            var proj = segment.ProjectPoint(canvasPos.ToVector2()).Value;
                            _origin = proj.ToPoint();
                            // We need to track our projected point as the segment moves, so make it relative to the OnCurve.
                            _anchor = (proj - segment.OnCurve.ToVector2()).ToPoint();

                            if (e.KeyModifiers.HasFlag(VirtualKeyModifiers.Control))
                            {
                                segment.Selected = !segment.Selected;
                            }
                            else
                            {
                                if (!segment.Selected)
                                {
                                    canvas.Layer.ClearSelection();
                                    segment.Selected = true;
                                }
                            }
                        }

                        Debug.Assert(CurrentAction == ActionType.DraggingPoint);
                    }
                }
                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerMoved(canvas, e);

            if (CurrentAction == ActionType.None)
                return;

            var ptPoint = e.GetCurrentPoint(canvas);
            if (ptPoint.Properties.IsLeftButtonPressed)
            {
                var pos = canvas.GetCanvasPosition(ptPoint.Position);

                if (_tappedItem is Data.Point point)
                {
                    if (!_canDrag)
                    {
                        _canDrag = point.Selected && CanStartDragging(ptPoint.Position, _screenOrigin.Value, 1);
                    }
                    if (_canDrag)
                    {
                        var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
                        if (shift.HasFlag(CoreVirtualKeyStates.Down))
                        {
                            pos = ClampToNodeOrOrigin(canvas, pos, point);
                        }

                        Outline.MoveSelection(
                            canvas.Layer,
                            (float)(pos.X - _anchor.X),
                            (float)(pos.Y - _anchor.Y),
                            GetMoveMode()
                        );
                        _anchor = pos;
                    }
                }
                else if (_tappedItem is Data.Segment segment)
                {
                    if (!_canDrag)
                    {
                        _canDrag = segment.Selected && CanStartDragging(ptPoint.Position, _screenOrigin.Value, 1);
                    }
                    if (_canDrag)
                    {
                        var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
                        if (shift.HasFlag(CoreVirtualKeyStates.Down))
                        {
                            pos = ClampToOrigin(pos, _origin);
                        }

                        var actualAnchor = _anchor.ToVector2() + segment.OnCurve.ToVector2();
                        Outline.MoveSelection(
                            canvas.Layer,
                            (float)pos.X - actualAnchor.X,
                            (float)pos.Y - actualAnchor.Y,
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

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(canvas, e);

            if (CurrentAction == ActionType.DraggingPoint)
            {
                if (_tappedItem is Data.Point)
                {
                    TryJoinPath(canvas, canvas.GetCanvasPosition(e.GetCurrentPoint(canvas).Position));
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
                        point.Selected = rect.Contains(point.ToVector2().ToPoint());
                    }
                }
            }
            else
            {
                return;
            }

            _screenOrigin = null;
            _origin = EmptyPoint;
            _canDrag = false;
            _tappedItem = null;
            ((App)Application.Current).InvalidateData();

            Debug.Assert(CurrentAction == ActionType.None);
        }

        public override void OnDoubleTapped(DesignCanvas canvas, DoubleTappedRoutedEventArgs e)
        {
            if (_tappedItem is Data.Point point)
            {
                if (Outline.TryTogglePointSmoothness(point))
                {
                    ((App)Application.Current).InvalidateData();
                }
            }
            if (_tappedItem is Data.Segment segment)
            {
                var path = segment.Parent;
                path.IsSelected = true;

                ((App)Application.Current).InvalidateData();
            }
        }

        /**/

        Point ClampToNodeOrOrigin(DesignCanvas canvas, Point pos, Data.Point point)
        {
            // We clamp to the mousedown pos, unless we have a single offcurve
            // in which case we clamp it against its parent.
            if (point.Type == Data.PointType.None && canvas.Layer.Selection.Count == 1)
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

        void TryJoinPath(DesignCanvas canvas, Point pos)
        {
            if (_tappedItem is Data.Point point && Is.AtOpenBoundary(point))
            {
                var otherItem = canvas.HitTest(pos, ignoreItem: point);
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

        public override IconElement Icon { get; } = new FontIcon() { Glyph = "\ue8b0" };

        public override string Name => "Selection";

        public override KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator() { Key = VirtualKey.V };

        #endregion
    }
}
