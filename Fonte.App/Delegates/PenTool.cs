﻿// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Fonte.App.Controls;
using Fonte.App.Interfaces;
using Fonte.App.Utilities;
using Fonte.Data.Collections;
using Fonte.Data.Interfaces;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;


namespace Fonte.App.Delegates
{
    public class PenTool : BaseTool
    {
        private Data.Path _path;
        private Point _screenOrigin;
        private bool _shouldMoveOnCurve;
        private ValueTuple<Data.Point, bool>? _stashedOffCurve;
        private Data.Point _point;
        private IChangeGroup _undoGroup;

        protected override CoreCursor DefaultCursor { get; } = Cursors.Pen;

        public override void OnDisabled(DesignCanvas canvas, ActivationEventArgs args)
        {
            base.OnDisabled(canvas, args);

            if (args.ActivationKind != ActivationKind.TemporarySwitch &&
                TryRemoveTrailingOffCurve(canvas.Layer))
            {
                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnKeyDown(DesignCanvas canvas, KeyRoutedEventArgs args)
        {
            if (args.Key == VirtualKey.Menu)
            {
                if (TrySetLastOnCurveSmoothness(_path, false))
                {
                    ((App)Application.Current).InvalidateData();
                }
            }
            else if (args.Key == VirtualKey.Space && _path != null)
            {
                _shouldMoveOnCurve = true;
            }
            else if (args.Key == VirtualKey.Escape)
            {
                var layer = canvas.Layer;
                TryRemoveTrailingOffCurve(layer);
                layer.ClearSelection();

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
            if (args.Key == VirtualKey.Menu)
            {
                if (TrySetLastOnCurveSmoothness(_path, true))
                {
                    ((App)Application.Current).InvalidateData();
                }
            }
            else if (args.Key == VirtualKey.Space)
            {
                _shouldMoveOnCurve = false;
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
            if (ptPoint.Properties.IsLeftButtonPressed && canvas.Layer is Data.Layer layer)
            {
                var pos = canvas.FromClientPosition(ptPoint.Position);
                // TODO: should we ignore Anchor/Component etc. here?
                var tappedItem = canvas.HitTest(pos, testSegments: true);
                var selPoint = GetSelectedPoint(layer);

                _screenOrigin = ptPoint.Position;
                _undoGroup = layer.CreateUndoGroup();
                if (tappedItem is Data.Point tappedPoint && tappedPoint.Type != Data.PointType.None)
                {
                    var tappedPath = tappedPoint.Parent;

                    if (Is.AtOpenBoundary(tappedPoint))
                    {
                        // If we click a boundary from another boundary, join the paths
                        if (selPoint != null && Is.AtOpenBoundary(selPoint) && AreVisiblyDistinct(canvas, selPoint, tappedPoint))
                        {
                            var selPath = selPoint.Parent;
                            var selPoints = selPath.Points;
                            if (selPoint.Type == Data.PointType.None)
                            {
                                selPoint.IsSelected = false;
                                selPoints.Pop();
                                var lastOn = selPoints.Last();
                                _stashedOffCurve = (selPoint, lastOn.IsSmooth);
                                lastOn.IsSmooth = false;
                            }
                            Outline.JoinPaths(selPath, selPoints[0] == selPoint,
                                              tappedPath, tappedPath.Points[0] == tappedPoint);
                            // Drag a control point, except if we're joining a different path (as we're not at boundary
                            // of the resulting path)
                            if (selPath == tappedPath)
                            {
                                _path = selPath;
                            }
                        }
                        // Otherwise reverse the path if needed and we'll drag the boundary point
                        else
                        {
                            if (tappedPoint == tappedPath.Points.First())
                            {
                                tappedPath.Reverse();
                            }

                            _path = tappedPath;
                        }
                    }
                    // If we clicked on an inside point, just break its path open.
                    else
                    {
                        TryRemoveTrailingOffCurve(layer);
                        layer.ClearSelection();

                        Outline.BreakPath(tappedPath, tappedPath.Points.IndexOf(tappedPoint));
                    }
                    
                    if (selPoint != null)
                    {
                        selPoint.IsSelected = false;
                    }
                    tappedPoint.IsSelected = true;
                }
                else if (tappedItem is Data.Segment segment)
                {
                    var result = segment.ProjectPoint(pos.ToVector2());

                    if (result.HasValue)
                    {
                        var t = result.Value.Item2;

                        if (!args.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
                        {
                            layer.ClearSelection();
                        }
                        var otherSegment = segment.SplitAt(t);

                        foreach (var point in Enumerable.Concat(segment.Points, otherSegment.OffCurves))
                        {
                            point.X = Outline.RoundToGrid(point.X);
                            point.Y = Outline.RoundToGrid(point.Y);
                        }

                        _point = segment.OnCurve;
                        _point.IsSelected = true;

                        _undoGroup.Dispose();
                        _undoGroup = layer.CreateUndoGroup();
                    }
                }
                else
                {
                    ObserverList<Data.Point> points;
                    Data.PointType type;
                    // Add a point to the current path, if any
                    if (selPoint != null && Is.AtOpenBoundary(selPoint))
                    {
                        _path = selPoint.Parent;
                        points = _path.Points;
                        var lastPoint = points.Last();
                        lastPoint.IsSelected = false;
                        if (lastPoint.Type == Data.PointType.None)
                        {
                            points.Pop();
                            var lastOn = points.Last();
                            _stashedOffCurve = (lastPoint, lastOn.IsSmooth);
                            lastOn.IsSmooth = false;
                            // For shift origin, always use an onCurve
                            lastPoint = lastOn;
                        }
                        if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
                        {
                            pos = ClampToOrigin(pos, new Point(lastPoint.X, lastPoint.Y));
                        }
                        type = Data.PointType.Line;
                    }
                    // Else just create a new one
                    else
                    {
                        _path = new Data.Path();
                        points = _path.Points;
                        layer.Paths.Add(_path);
                        type = Data.PointType.Move;
                    }

                    // In any case, unselect all points (*click*) and enable new point
                    layer.ClearSelection();
                    var x = Outline.RoundToGrid((float)pos.X);
                    var y = Outline.RoundToGrid((float)pos.Y);
                    points.Add(new Data.Point(x, y, type)
                    {
                        IsSelected = true
                    });
                }

                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerMoved(canvas, args);

            var ptPoint = args.GetCurrentPoint(canvas);
            var isLeftButtonPressed = ptPoint.Properties.IsLeftButtonPressed;
            if (isLeftButtonPressed && _point != null)
            {
                var pos = canvas.FromClientPosition(ptPoint.Position);

                _undoGroup.Reset();

                Outline.MoveSelection(canvas.Layer, (float)pos.X - _point.X,
                                                    (float)pos.Y - _point.Y, GetMoveMode());

                ((App)Application.Current).InvalidateData();
            }
            else if (isLeftButtonPressed && _path != null)
            {
                var pos = canvas.FromClientPosition(ptPoint.Position);
                var selPoint = GetMovingPoint(_path);

                if (selPoint.Type != Data.PointType.None && !_shouldMoveOnCurve)
                {
                    if (!CanStartDragging(ptPoint.Position, _screenOrigin))
                    {
                        return;
                    }
                    var makeOnSmooth = !args.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu);
                    selPoint.IsSelected = false;
                    selPoint.IsSmooth = _path.Points.Count > 1 && makeOnSmooth;

                    if (selPoint.Type == Data.PointType.Line && makeOnSmooth)
                    {
                        CoerceSegmentToCurve(selPoint, pos);
                    }
                    else if (selPoint.IsSmooth && _path.IsOpen)
                    {
                        // If there's a curve segment behind, we need to update the
                        // offCurve's position to inverse
                        if (_path.Points.Count > 1)
                        {
                            var onCurveBefore = _path.Points[_path.Points.Count - 2];
                            onCurveBefore.X = Outline.RoundToGrid(2 * selPoint.X - (float)pos.X);
                            onCurveBefore.Y = Outline.RoundToGrid(2 * selPoint.Y - (float)pos.Y);
                        }
                    }

                    if (_path.IsOpen)
                    {
                        var x = Outline.RoundToGrid((float)pos.X);
                        var y = Outline.RoundToGrid((float)pos.Y);
                        var point = new Data.Point(x, y)
                        {
                            IsSelected = true
                        };
                        _path.Points.Add(point);
                    }
                    else
                    {
                        selPoint.IsSelected = false;
                        _path.Points[_path.Points.Count - 2].IsSelected = true;
                    }
                }
                else
                {
                    Data.Point onCurve;
                    var onCurveIndex = _path.Points.Count - 1;
                    if (selPoint.Type != Data.PointType.None)
                    {
                        /* onCurveIndex = Count - 1 */
                        onCurve = selPoint;
                    }
                    else if (_path.IsOpen)
                    {
                        onCurveIndex -= 1;
                        onCurve = _path.Points[onCurveIndex];
                    }
                    else
                    {
                        /* onCurveIndex = Count - 1 */
                        onCurve = _path.Points[onCurveIndex];
                    }
                    if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
                    {
                        pos = ClampToOrigin(pos, new Point(onCurve.X, onCurve.Y));
                    }
                    if (_shouldMoveOnCurve)
                    {
                        var dx = (float)(pos.X - selPoint.X);
                        var dy = (float)(pos.Y - selPoint.Y);
                        onCurve.X = Outline.RoundToGrid(onCurve.X + dx);
                        onCurve.Y = Outline.RoundToGrid(onCurve.Y + dy);
                        if (onCurveIndex - 1 >= 0)
                        {
                            var prev = _path.Points[onCurveIndex - 1];
                            if (prev.Type == Data.PointType.None)
                            {
                                prev.X = Outline.RoundToGrid(prev.X + dx);
                                prev.Y = Outline.RoundToGrid(prev.Y + dy);
                            }
                        }
                        if (onCurveIndex + 1 < _path.Points.Count)
                        {
                            var next = _path.Points[onCurveIndex + 1];
                            if (next.Type == Data.PointType.None)
                            {
                                next.X = Outline.RoundToGrid(next.X + dx);
                                next.Y = Outline.RoundToGrid(next.Y + dy);
                            }
                        }
                    }
                    else
                    {
                        selPoint.X = Outline.RoundToGrid((float)pos.X);
                        selPoint.Y = Outline.RoundToGrid((float)pos.Y);
                        if (_path.IsOpen && _path.Points.Count >= 3 && onCurve.IsSmooth)
                        {
                            if (onCurve.Type == Data.PointType.Line)
                            {
                                CoerceSegmentToCurve(onCurve, pos);
                            }
                            var otherSidePoint = _path.Points[_path.Points.Count - 3];
                            otherSidePoint.X = Outline.RoundToGrid(2 * onCurve.X - (float)pos.X);
                            otherSidePoint.Y = Outline.RoundToGrid(2 * onCurve.Y - (float)pos.Y);
                        }
                    }
                }
                ((App)Application.Current).InvalidateData();
            }

            if (!isLeftButtonPressed &&
                !ptPoint.Properties.IsMiddleButtonPressed &&
                !ptPoint.Properties.IsRightButtonPressed)
            {
                var pos = canvas.FromClientPosition(ptPoint.Position);

                Cursor = GetItemCursor(canvas.HitTest(pos, testSegments: true));
                canvas.InvalidateCursor();
            }
        }

        /**/

        protected override void CompleteMove(DesignCanvas canvas)
        {
            base.CompleteMove(canvas);

            if (_undoGroup != null)
            {
                _undoGroup.Dispose();
                _undoGroup = null;

                ((App)Application.Current).InvalidateData();
            }
            _path = null;
            _screenOrigin = default;
            _shouldMoveOnCurve = false;
            _stashedOffCurve = null;
            _point = null;
        }

        protected new CoreCursor GetItemCursor(object item)
        {
            if (item is Data.Point point && point.Type != Data.PointType.None)
            {
                return Cursors.PenWithSquare;
            }
            else if (item is Data.Segment)
            {
                return Cursors.PenWithPlus;
            }

            return DefaultCursor;
        }

        bool AreVisiblyDistinct(DesignCanvas canvas, Data.Point point, Data.Point other)
        {
            var pos = canvas.FromCanvasPosition(point.ToVector2().ToPoint());
            var otherPos = canvas.FromCanvasPosition(other.ToVector2().ToPoint());

            return CanStartDragging(pos, otherPos);
        }

        void CoerceSegmentToCurve(Data.Point onCurve, Point pos)
        {
            var index = _path.Points.IndexOf(onCurve);
            var prevOn = _path.Points[index - 1];
            
            // Add an offCurve before onCurve
            Data.Point newPoint;
            bool smooth;
            if (_path.IsOpen)
            {
                // Inverse point
                var x = Outline.RoundToGrid(2 * onCurve.X - (float)pos.X);
                var y = Outline.RoundToGrid(2 * onCurve.Y - (float)pos.Y);
                newPoint = new Data.Point(x, y);
                smooth = true;
            }
            else
            {
                // Closed path, we put the point under the mouse
                var x = Outline.RoundToGrid((float)pos.X);
                var y = Outline.RoundToGrid((float)pos.Y);
                newPoint = new Data.Point(x, y);
                smooth = false;
            }
            _path.Points.Insert(index, newPoint);

            // Add the first of two offCurves
            if (_stashedOffCurve != null)
            {
                var (offCurve, onSmooth) = _stashedOffCurve.Value;
                prevOn.IsSmooth = index - 1 > 0 && onSmooth;
                _path.Points.Insert(index, offCurve);
                _stashedOffCurve = null;
            }
            else
            {
                var x = Outline.RoundToGrid(prevOn.X + .35f * (onCurve.X - prevOn.X));
                var y = Outline.RoundToGrid(prevOn.Y + .35f * (onCurve.Y - prevOn.Y));
                _path.Points.Insert(index, new Data.Point(x, y));
            }

            // Now flag onCurve as curve segment
            onCurve.Type = Data.PointType.Curve;
            onCurve.IsSmooth = smooth;
        }

        static Data.Point GetMovingPoint(Data.Path path)
        {
            var point = path.Points[path.Points.Count - 1];
            if (!path.IsOpen && point.Type == Data.PointType.Curve)
            {
                var pt = path.Points[path.Points.Count - 2];
                if (pt.IsSelected)
                {
                    point = pt;
                }
            }
            return point;
        }

        static Data.Point GetSelectedPoint(Data.Layer layer)
        {
            var selection = layer.Selection;

            if (selection.Count == 1 && selection.First() is Data.Point point)
            {
                return point;
            }
            return null;
        }

        static bool TryRemoveTrailingOffCurve(Data.Layer layer)
        {
            if (GetSelectedPoint(layer) is Data.Point selPoint)
            {
                var selPath = selPoint.Parent;
                var selPoints = selPath.Points;
                if (selPoint.Type == Data.PointType.None && selPath.IsOpen && selPoints.Last() == selPoint)
                {
                    // XXX: this should be !IsShallow change group
                    using (var group = layer.CreateUndoGroup())
                    {
                        selPoints.Pop();
                        var last = selPoints.Last();
                        last.IsSelected = selPoint.IsSelected;
                        last.IsSmooth = false;
                    }
                    return true;
                }
            }
            return false;
        }

        static bool TrySetLastOnCurveSmoothness(Data.Path path, bool value)
        {
            if (path?.Points.Count >= 2)
            {
                var point = path.Points.Last();
                if (point.IsSelected)
                {
                    if (point.Type == Data.PointType.None)
                    {
                        point = path.Points[path.Points.Count - 2];
                        if (point.Type == Data.PointType.None || point == path.Points[0])
                        {
                            return false;
                        }
                    }
                    point.IsSmooth = value;
                    return true;
                }
            }
            return false;
        }

#region IToolBarEntry implementation

        public override IconSource Icon { get; } = new FontIconSource() { FontSize = 16, Glyph = "\uedfb" };

        public override string Name => "Pen";

        public override KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator() { Key = VirtualKey.P };

#endregion
    }
}
