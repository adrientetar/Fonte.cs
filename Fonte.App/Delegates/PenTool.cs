/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Fonte.App.Utilities;
    using Fonte.Data.Utilities;

    using System;
    using System.Linq;
    using Windows.Foundation;
    using Windows.System;
    using Windows.UI.Core;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using Windows.UI.Xaml;
    using Fonte.Data.Interfaces;

    public class PenTool : BaseTool
    {
        private Data.Point _lastOn;
        private Data.Path _path;
        private Point? _screenOrigin;
        private bool _shouldMoveOnCurve;
        private ValueTuple<Data.Point, bool>? _stashedOffCurve;
        private IChangeGroup _undoGroup;

        public override void OnDisabled(DesignCanvas canvas)
        {
            ObserverList<Data.Point> points;
            try
            {
                points = canvas.Layer.Paths.Last().Points;
            }
            catch (InvalidOperationException)
            {
                return;
            }
            
            if (points.Last().Type == Data.PointType.None)
            {
                points.Pop();
                points.Last().Smooth = false;

                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnKeyDown(DesignCanvas canvas, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Menu)
            {
                _updateOnCurveSmoothness(canvas, false);
            }
            else if (e.Key == VirtualKey.Space && _path != null)
            {
                _shouldMoveOnCurve = true;
            }
            else
            {
                return;
            }

            e.Handled = true;
            //((App)Application.Current).InvalidateData();
        }

        public override void OnKeyUp(DesignCanvas canvas, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Menu)
            {
                _updateOnCurveSmoothness(canvas, true);
            }
            else if (e.Key == VirtualKey.Space && _path != null)
            {
                _shouldMoveOnCurve = false;
            }
            else
            {
                return;
            }

            e.Handled = true;
            //((App)Application.Current).InvalidateData();
        }

        public override void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(canvas, e);

            if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
            {
                var pos = canvas.GetLocalPosition(e);
                var mouseItem = canvas.ItemAt(pos);
                var selPoint = _getSelectedPoint(canvas);

                _undoGroup = canvas.Layer.CreateUndoGroup();
                if (mouseItem is Data.Point mousePoint)
                {
                    var mousePath = mousePoint.Parent;
                    // If we click an open path boundary and the point at the other boundary is selected, close the path.
                    if (Is.AtOpenBoundary(mousePoint))
                    {
                        if (selPoint != null && selPoint != mousePoint &&
                            Is.AtOpenBoundary(selPoint))
                        {
                            var selPath = selPoint.Parent;
                            var selPoints = selPath.Points;
                            if (selPoint.Type == Data.PointType.None)
                            {
                                selPoint.Selected = false;
                                selPoints.Pop();
                                _lastOn = selPoints.Last();
                                _stashedOffCurve = (selPoint, _lastOn.Smooth);
                                _lastOn.Smooth = false;
                            }
                            Outline.JoinPaths(selPath, selPoints[0] == selPoint,
                                              mousePath, mousePath.Points[0] == mousePoint);
                            _path = selPath;
                            _screenOrigin = e.GetCurrentPoint(canvas).Position;
                        }

                    }
                    // If we just clicked on a closed point, break the path open.
                    else
                    {
                        Outline.BreakPath(mousePath, mousePath.Points.IndexOf(mousePoint));
                    }
                    
                    if (selPoint != null)
                    {
                        selPoint.Selected = false;
                    }
                    mousePoint.Selected = true;
                }
                else
                {
                    ObserverList<Data.Point> points;
                    Data.PointType type;
                    // Otherwise, add a point to current path if applicable
                    if (selPoint != null && Is.AtOpenBoundary(selPoint))
                    {
                        _path = selPoint.Parent;
                        points = _path.Points;
                        var lastPoint = points.Last();
                        lastPoint.Selected = false;
                        if (lastPoint.Type == Data.PointType.None)
                        {
                            points.Pop();
                            _lastOn = points.Last();
                            _stashedOffCurve = (lastPoint, _lastOn.Smooth);
                            _lastOn.Smooth = false;
                            // For shift origin, always use an onCurve
                            lastPoint = _lastOn;
                        }
                        var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
                        if (shift.HasFlag(CoreVirtualKeyStates.Down))
                        {
                            pos = ClampToOrigin(new Point(lastPoint.X, lastPoint.Y), pos);
                        }
                        type = Data.PointType.Line;
                    }
                    // Or create a new one
                    else
                    {
                        _path = new Data.Path();
                        points = _path.Points;
                        canvas.Layer.Paths.Add(_path);
                        type = Data.PointType.Move;
                    }
                    // In any case, unselect all points (*click*) and enable new point
                    canvas.Layer.ClearSelection();
                    var point = new Data.Point((float)pos.X, (float)pos.Y, type)
                    {
                        Selected = true
                    };
                    points.Add(point);
                    _screenOrigin = e.GetCurrentPoint(canvas).Position;
                }

                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerMoved(canvas, e);

            if (_path != null && e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
            {
                var pos = canvas.GetLocalPosition(e);
                var selPoint = _getMovingPoint();
                if (selPoint.Type != Data.PointType.None && !_shouldMoveOnCurve)
                {
                    if (!CanStartDragging(_screenOrigin.Value, e.GetCurrentPoint(canvas).Position))
                    {
                        return;
                    }
                    var makeOnSmooth = !Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
                    selPoint.Selected = false;
                    selPoint.Smooth = _path.Points.Count > 1 && makeOnSmooth;

                    if (selPoint.Type == Data.PointType.Line && makeOnSmooth)
                    {
                        _coerceSegmentToCurve(_path, selPoint, pos);
                    }
                    else if (selPoint.Smooth && _path.IsOpen)
                    {
                        // If there's a curve segment behind, we need to update the
                        // offCurve's position to inverse
                        if (_path.Points.Count > 1)
                        {
                            var onCurveBefore = _path.Points[_path.Points.Count - 2];
                            onCurveBefore.X = 2 * selPoint.X - (float)pos.X;
                            onCurveBefore.Y = 2 * selPoint.Y - (float)pos.Y;
                        }
                    }

                    if (_path.IsOpen)
                    {
                        var point = new Data.Point((float)pos.X, (float)pos.Y)
                        {
                            Selected = true
                        };
                        _path.Points.Add(point);
                    }
                    else
                    {
                        selPoint.Selected = false;
                        _path.Points[_path.Points.Count - 2].Selected = true;
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
                    var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
                    if (shift.HasFlag(CoreVirtualKeyStates.Down))
                    {
                        pos = ClampToOrigin(new Point(onCurve.X, onCurve.Y), pos);
                    }
                    if (_shouldMoveOnCurve)
                    {
                        var dx = (float)(pos.X - selPoint.X);
                        var dy = (float)(pos.Y - selPoint.Y);
                        onCurve.X += dx;
                        onCurve.Y += dy;
                        if (_path.Points.Count >= 3)
                        {
                            var prev = _path.Points[onCurveIndex - 1];
                            if (prev.Type == Data.PointType.None)
                            {
                                prev.X += dx;
                                prev.Y += dy;
                            }
                        }
                        var next = _path.Points[onCurveIndex + 1];
                        if (next.Type == Data.PointType.None)
                        {
                            next.X += dx;
                            next.Y += dy;
                        }
                    }
                    else
                    {
                        selPoint.X = (float)pos.X;
                        selPoint.Y = (float)pos.Y;
                        if (_path.IsOpen && _path.Points.Count >= 3 && onCurve.Smooth)
                        {
                            if (onCurve.Type == Data.PointType.Line)
                            {
                                _coerceSegmentToCurve(_path, onCurve, pos);
                            }
                            var otherSidePoint = _path.Points[_path.Points.Count - 3];
                            otherSidePoint.X = 2 * onCurve.X - (float)pos.X;
                            otherSidePoint.Y = 2 * onCurve.Y - (float)pos.Y;
                        }
                    }
                }
                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(canvas, e);

            if (_path != null)
            {
                _path = null;
                _screenOrigin = null;
                _shouldMoveOnCurve = false;
                _stashedOffCurve = null;
            }
            if (_undoGroup != null)
            {
                _undoGroup.Dispose();
                _undoGroup = null;
            }
        }

        private void _coerceSegmentToCurve(Data.Path path, Data.Point onCurve, Point pos)
        {
            var index = _path.Points.IndexOf(onCurve);
            var prevOn = _path.Points[index - 1];
            
            // Add an offCurve before onCurve
            Data.Point newPoint;
            bool smooth;
            if (_path.IsOpen)
            {
                // Inverse point
                newPoint = new Data.Point(
                        2 * onCurve.X - (float)pos.X,
                        2 * onCurve.Y - (float)pos.Y
                    );
                smooth = true;
            }
            else
            {
                // Closed path, we put the point under the mouse
                newPoint = new Data.Point(
                        (float)pos.X,
                        (float)pos.Y
                    );
                smooth = false;
            }
            _path.Points.Insert(index, newPoint);

            // Add the first of two offCurves
            if (_stashedOffCurve != null)
            {
                var (offCurve, onSmooth) = _stashedOffCurve.Value;
                prevOn.Smooth = index - 1 > 0 && onSmooth;
                _path.Points.Insert(index, offCurve);
                _stashedOffCurve = null;
            }
            else
            {
                var ierpPoint = new Data.Point(
                        prevOn.X + (float)Math.Round(.35 * (onCurve.X - prevOn.X)),
                        prevOn.Y + (float)Math.Round(.35 * (onCurve.Y - prevOn.Y))
                    );
                _path.Points.Insert(index, ierpPoint);
            }

            // Now flag onCurve as curve segment
            onCurve.Type = Data.PointType.Curve;
            onCurve.Smooth = smooth;
        }

        private Data.Point _getMovingPoint()
        {
            var point = _path.Points[_path.Points.Count - 1];
            if (!_path.IsOpen && point.Type == Data.PointType.Curve)
            {
                var pt = _path.Points[_path.Points.Count - 2];
                if (pt.Selected)
                {
                    point = pt;
                }
            }
            return point;
        }

        private Data.Point _getSelectedPoint(DesignCanvas canvas)
        {
            var selection = canvas.Layer.Selection;
            if (selection.Count == 1)
            {
                var element = selection.First();
                if (element is Data.Point point)
                {
                    return point;
                }
            }
            return null;
        }

        private void _updateOnCurveSmoothness(DesignCanvas canvas, bool value)
        {
            if (_path != null && _path.Points.Count >= 2)
            {
                var point = _path.Points.Last();
                if (point.Selected)
                {
                    if (point.Type == Data.PointType.None)
                    {
                        point = _path.Points[_path.Points.Count - 2];
                        if (point.Type == Data.PointType.None || point == _path.Points[0])
                        {
                            return;
                        }
                    }
                    point.Smooth = value;
                    ((App)Application.Current).InvalidateData();
                }
            }
        }

#region IToolBarEntry implementation

        public override IconElement Icon { get; } = new FontIcon() { Glyph = "\uedfb" };

        public override string Name => "Pen";

        public override KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator() { Key = VirtualKey.P };

#endregion
    }
}
