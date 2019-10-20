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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
    using Windows.Foundation;
    using Windows.System;
    using Windows.System.Threading;
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
        private Point _focusPoint = EmptyPoint;
        private List<Vector2> _points;
        private bool _canDrag = false;
        private object _tappedItem;
        private float? _tappedLocation;
        private (CanvasGeometry, CanvasGeometry)? _oldPaths;
        private SnapResult _snapResult;
        private ThreadPoolTimer _timer;
        private IChangeGroup _undoGroup;

        static readonly Point EmptyPoint = new Point(double.PositiveInfinity, double.NegativeInfinity);

        protected override CoreCursor DefaultCursor { get; } = Cursors.Arrow;

        enum ActionType
        {
            None = 0,
            DraggingItem,
            SelectingPoly,
            SelectingRect,
            ShapingCurve
        };

        ActionType CurrentAction
        {
            get
            {
                if (_tappedLocation != null)
                {
                    return ActionType.ShapingCurve;
                }
                else if (_undoGroup != null)
                {
                    return ActionType.DraggingItem;
                }
                else if (_points != null)
                {
                    return ActionType.SelectingPoly;
                }
                else if (_origin != EmptyPoint)
                {
                    return ActionType.SelectingRect;
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

        public override void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
            if (_oldPaths != null)
            {
                var color = Color.FromArgb(255, 210, 210, 210);

                ds.DrawGeometry(_oldPaths.Value.Item1, color, strokeWidth: rescale);
                ds.DrawGeometry(_oldPaths.Value.Item2, color, strokeWidth: rescale);
            }

            var action = CurrentAction;
            if (action == ActionType.SelectingPoly)
            {
                if (_points.Count > 1)
                {
                    var device = CanvasDevice.GetSharedDevice();
                    var builder = new CanvasPathBuilder(device);
                    builder.BeginFigure(_points[0]);
                    foreach (var point in _points.Skip(1))
                    {
                        builder.AddLine(point);
                    }
                    builder.EndFigure(CanvasFigureLoop.Open);

                    using (var path = CanvasGeometry.CreatePath(builder))
                    {
                        ds.DrawGeometry(path, Color.FromArgb(225, 45, 45, 45), strokeWidth: rescale);
                    }
                    builder.Dispose();
                }
            }
            else if (action == ActionType.SelectingRect)
            {
                ds.FillRectangle(new Rect(_origin, _anchor), Color.FromArgb(51, 0, 120, 215));
            }
            else if (action == ActionType.ShapingCurve)
            {
                if (_focusPoint != EmptyPoint)
                {
                    ds.FillCircle(_focusPoint.ToVector2(), 4 * rescale, Color.FromArgb(225, 172, 100, 68));
                }
            }
        }

        public override void OnDrawCompleted(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
            if (_snapResult != null)
            {
                var color = (Color)FindResource(canvas, DesignCanvas.SnapLineColorKey);
                var halfSize = 2.5f * rescale;

                if (_snapResult.NearPoint is Data.Point point)
                {
                    ds.DrawCircle(point.ToVector2(), 5.5f * rescale, color, strokeWidth: rescale);
                }

                var refPos = _snapResult.Position;
                foreach (var (p1, p2) in _snapResult.GetSnapLines())
                {
                    ds.DrawLine(p1, p2, color, strokeWidth: rescale);

                    if (p1 != refPos)
                    {
                        ds.DrawLine(p1.X - halfSize, p1.Y - halfSize, p1.X + halfSize, p1.Y + halfSize, color, strokeWidth: rescale);
                        ds.DrawLine(p1.X - halfSize, p1.Y + halfSize, p1.X + halfSize, p1.Y - halfSize, color, strokeWidth: rescale);
                    }
                    if (p2 != refPos)
                    {
                        ds.DrawLine(p2.X - halfSize, p2.Y - halfSize, p2.X + halfSize, p2.Y + halfSize, color, strokeWidth: rescale);
                        ds.DrawLine(p2.X - halfSize, p2.Y + halfSize, p2.X + halfSize, p2.Y - halfSize, color, strokeWidth: rescale);
                    }
                }
            }
        }

        public override void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerPressed(canvas, args);

            var ptPoint = args.GetCurrentPoint(canvas);
            if (ptPoint.Properties.IsLeftButtonPressed && canvas.Layer is Data.Layer layer)
            {
                var canvasPos = canvas.FromClientPosition(ptPoint.Position);
                _tappedItem = canvas.HitTest(canvasPos, testSegments: true);

                if (_tappedItem != null)
                {
                    if (_tappedItem is UIBroker.BBoxHandle ||
                        _tappedItem is UIBroker.GuidelineRule)
                    {
                        _screenOrigin = ptPoint.Position;
                        _undoGroup = layer.CreateUndoGroup();

                        Debug.Assert(CurrentAction == ActionType.DraggingItem);
                    }
                    else if (_tappedItem is Data.Segment segment && args.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu))
                    {
                        _screenOrigin = ptPoint.Position;
                        _oldPaths = (layer.ClosedCanvasPath, layer.OpenCanvasPath);

                        _origin = canvasPos;
                        // TODO: avoid reprojecting?
                        _tappedLocation = segment.ProjectPoint(canvasPos.ToVector2())?.Item2;
                        _tappedItem = segment.ConvertTo(Data.PointType.Curve);

                        _undoGroup = layer.CreateUndoGroup();

                        Debug.Assert(CurrentAction == ActionType.ShapingCurve);
                    }
                    else if (_tappedItem is ISelectable isel)
                    {
                        _screenOrigin = ptPoint.Position;
                        _oldPaths = (layer.ClosedCanvasPath, layer.OpenCanvasPath);

                        _origin = GetOriginPoint(isel, canvasPos);

                        if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
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
                    if (!args.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
                    {
                        layer.ClearSelection();
                    }

                    if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu))
                    {
                        _points = new List<Vector2>
                        {
                            canvasPos.ToVector2()
                        };

                        Debug.Assert(CurrentAction == ActionType.SelectingPoly);
                    }
                    else
                    {
                        _origin = _anchor = canvasPos;

                        Debug.Assert(CurrentAction == ActionType.SelectingRect);
                    }
                }

                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerMoved(canvas, args);

            var ptPoint = args.GetCurrentPoint(canvas);
            var isLeftButtonPressed = ptPoint.Properties.IsLeftButtonPressed;
            if (isLeftButtonPressed && CurrentAction != ActionType.None)
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

                        if (!_canDrag) return;
                    }

                    var layer = canvas.Layer;
                    var bounds = layer.SelectionBounds;

                    var delta = pos.ToVector2() - handle.Position;
                    if (bounds.Width > 0)
                    {
                        var origin = Vector2.Zero;

                        var hr = delta.Y / bounds.Height;
                        var wr = delta.X / bounds.Width;
                        if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
                        {
                            var lo = Math.Min(
                                    Math.Abs(hr),
                                    Math.Abs(wr)
                                );
                            hr = Math.Sign(hr) * lo;
                            wr = Math.Sign(wr) * lo;
                        }

                        if (handle.Kind.HasFlag(UIBroker.HandleKind.Top))
                        {
                            hr = 1 + hr;
                            origin.Y = bounds.Bottom;
                        }
                        else if (handle.Kind.HasFlag(UIBroker.HandleKind.Bottom))
                        {
                            hr = 1 - hr;
                            origin.Y = bounds.Top;
                        }
                        else
                        {
                            hr = 1f;
                        }

                        if (handle.Kind.HasFlag(UIBroker.HandleKind.Right))
                        {
                            wr = 1 + wr;
                            origin.X = bounds.Left;
                        }
                        else if (handle.Kind.HasFlag(UIBroker.HandleKind.Left))
                        {
                            wr = 1 - wr;
                            origin.X = bounds.Right;
                        }
                        else
                        {
                            wr = 1f;
                        }

                        layer.Transform(Matrix3x2.CreateScale(wr, hr, origin),
                                        selectionOnly: true);
                        Outline.RoundSelection(layer);
                    }
                }
                else if (_tappedItem is UIBroker.GuidelineRule rule)
                {
                    var guideline = rule.Guideline;
                    var deg = Conversion.ToDegrees(
                        Conversion.FromVector(pos.ToVector2() - guideline.ToVector2()));

                    // TODO: we could always modulo 180 to normalize since 0-180 and 180-360 are equivalent
                    if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
                    {
                        guideline.Angle = (int)(deg + MathF.Sign(deg) * 45) / 90 * 90;
                    }
                    else
                    {
                        guideline.Angle = .1f * Outline.RoundToGrid(10f * deg);
                    }
                }
                else if (_tappedItem is Data.Segment segment && _tappedLocation.HasValue)
                {
                    if (!_canDrag)
                    {
                        _canDrag = CanStartDragging(ptPoint.Position, _screenOrigin, 1);

                        if (!_canDrag) return;
                    }

                    var curve = segment.PointsInclusive;
                    Outline.NudgeCurve(curve, (float)(pos.X - _origin.X),
                                              (float)(pos.Y - _origin.Y));

                    _focusPoint = Utilities.BezierMath.Q(curve.Select(p => p.ToVector2())
                                                              .ToArray(), _tappedLocation.Value)
                                                      .ToPoint();
                }
                else if (_tappedItem is ISelectable isel)
                {
                    var layer = canvas.Layer;

                    if (_tappedItem is ILocatable iloc)
                    {
                        // No need for the _canDrag guard here because we're snapping to _tappedItem.
                        var snapResult = canvas.SnapPoint(pos, iloc, GetClampTarget(layer, iloc as Data.Point));
                        if (snapResult.Position != _snapResult?.Position)
                        {
                            _timer?.Cancel();

                            _snapResult = snapResult;
                            _timer = ThreadPoolTimer.CreateTimer(async timer =>
                            {
                                await canvas.Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                                {
                                    var snapResult = _snapResult;
                                    if (snapResult != null)
                                    {
                                        snapResult.Hide();

                                        canvas.Invalidate();
                                    }
                                });
                            }, TimeSpan.FromMilliseconds(600));
                        }

                        pos = _snapResult.Position.ToPoint();
                    }
                    else
                    {
                        if (!_canDrag)
                        {
                            _canDrag = isel.IsSelected && CanStartDragging(ptPoint.Position, _screenOrigin, 1);

                            if (!_canDrag) return;
                        }

                        if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
                        {
                            pos = ClampToOrigin(pos, _origin);
                        }
                    }

                    Outline.MoveSelection(
                        layer,
                        (float)(pos.X - _origin.X),
                        (float)(pos.Y - _origin.Y),
                        GetMoveMode()
                    );
                }
                else
                {
                    if (CurrentAction == ActionType.SelectingPoly)
                    {
                        _points.Add(pos.ToVector2());
                    }
                    else
                    {
                        Debug.Assert(CurrentAction == ActionType.SelectingRect);

                        _anchor = pos;
                    }
                }

                ((App)Application.Current).InvalidateData();
            }

            if (!isLeftButtonPressed &&
                !ptPoint.Properties.IsMiddleButtonPressed &&
                !ptPoint.Properties.IsRightButtonPressed)
            {
                var pos = canvas.FromClientPosition(ptPoint.Position);

                Cursor = GetItemCursor(canvas.HitTest(pos));
                canvas.InvalidateCursor();
            }
        }

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            if (CurrentAction == ActionType.DraggingItem)
            {
                if (_tappedItem is Data.Point point && _snapResult?.NearPoint is Data.Point otherPoint)
                {
                    Outline.TryJoinPath(canvas.Layer, point, otherPoint);
                }
            }
            else if (CurrentAction == ActionType.SelectingPoly)
            {
                using var poly = CanvasGeometry.CreatePolygon(CanvasDevice.GetSharedDevice(), _points.ToArray());

                foreach (var path in canvas.Layer.Paths)
                {
                    foreach (var point in path.Points)
                    {
                        if (poly.FillContainsPoint(point.ToVector2()))
                        {
                            point.IsSelected = !point.IsSelected;
                        }
                    }
                }
            }
            else if (CurrentAction == ActionType.SelectingRect)
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
            else if (CurrentAction == ActionType.None)
            {
                return;
            }

            base.OnPointerReleased(canvas, args);
        }

        public override void OnDoubleTapped(DesignCanvas canvas, DoubleTappedRoutedEventArgs args)
        {
            if (_tappedItem is Data.Anchor anchor)
            {
                canvas.EditAnchorName(anchor);
            }
            else if (_tappedItem is Data.Guideline guideline)
            {
                guideline.Angle = MathF.Round(guideline.Angle + 90) % 360;

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

        protected override void CompleteMove(DesignCanvas canvas)
        {
            base.CompleteMove(canvas);

            if (CurrentAction != ActionType.None)
            {
                if (_timer != null)
                {
                    _timer?.Cancel();
                    _timer = null;
                }
                if (_undoGroup != null)
                {
                    _undoGroup.Dispose();
                    _undoGroup = null;
                }
                _points = null;
                _screenOrigin = default;
                _snapResult = null;
                _origin = EmptyPoint;
                _canDrag = false;
                _focusPoint = EmptyPoint;
                _tappedItem = _tappedLocation = null;
                _oldPaths = null;

                ((App)Application.Current).InvalidateData();
            }

            Debug.Assert(CurrentAction == ActionType.None);
        }

        protected new CoreCursor GetItemCursor(object item)
        {
            if (item is Data.Anchor || item is Data.Component || item is Data.Guideline || item is Data.Point)
            {
                return Cursors.ArrowWithSquare;
            }

            return base.GetItemCursor(item);
        }

        Point? GetClampTarget(Data.Layer layer, Data.Point point)
        {
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
            if (shift.HasFlag(CoreVirtualKeyStates.Down))
            {
                // If the target point is an offcurve, we clamp against its oncurve sibling.
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
                        return otherPoint.ToVector2().ToPoint();
                    }
                }
                return _origin;
            }
            return null;
        }

        Point GetOriginPoint(ISelectable item, Point pos)
        {
            if (item is Data.Component)
            {
                return pos;
            }
            else if (item is Data.Segment segment)
            {
                // TODO: avoid reprojecting?
                return segment.ProjectPoint(pos.ToVector2()).Value.Item1.ToPoint();
            }
            else if (item is ILocatable iloc)
            {
                return iloc.ToVector2().ToPoint();
            }
            throw new ArgumentException($"{item} is not allowed here.");
        }

        #region IToolBarEntry implementation

        public override IconSource Icon { get; } = new FontIconSource() { FontSize = 16, Glyph = "\ue8b0" };

        public override string Name => "Selection";

        public override KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator() { Key = VirtualKey.V };

        #endregion
    }
}
