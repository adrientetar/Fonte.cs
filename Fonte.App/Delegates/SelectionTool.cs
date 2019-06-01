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
        private object _mouseItem;
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
                _origin = _anchor = canvas.GetLocalPosition(ptPoint.Position);
                _mouseItem = canvas.FindItemAt(_origin);

                if (_mouseItem is Data.Point point)
                {
                    // Fix the origin to be the point coordinates, in case we start clamping against it with Shift.
                    _origin = _anchor = new Point(point.X, point.Y);

                    _oldPaths = (canvas.Layer.ClosedCanvasPath, canvas.Layer.OpenCanvasPath);
                    _undoGroup = canvas.Layer.CreateUndoGroup();

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

                    Debug.Assert(CurrentAction == ActionType.DraggingPoint);
                }
                else
                {
                    canvas.Layer.ClearSelection();

                    Debug.Assert(CurrentAction == ActionType.DraggingRect);
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
                var pos = canvas.GetLocalPosition(ptPoint.Position);

                if (_mouseItem is Data.Point point)
                {
                    var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
                    if (shift.HasFlag(CoreVirtualKeyStates.Down))
                    {
                        pos = ClampToNodeOrOrigin(canvas, point, pos);
                    }

                    Outline.MoveSelection(
                        canvas.Layer,
                        (float)(pos.X - _origin.X),
                        (float)(pos.Y - _origin.Y),
                        GetMoveMode()
                    );
                    _origin = _anchor = pos;
                }
                else
                {
                    _anchor = pos;

                    //Debug.Assert(CurrentAction == ActionType.DraggingRect);
                }

                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(canvas, e);

            if (CurrentAction == ActionType.DraggingPoint)
            {
                TryJoinPath(canvas, canvas.GetLocalPosition(e.GetCurrentPoint(canvas).Position));
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

            _mouseItem = null;
            _origin = EmptyPoint;
            ((App)Application.Current).InvalidateData();

            Debug.Assert(CurrentAction == ActionType.None);
        }

        /**/

        Point ClampToNodeOrOrigin(DesignCanvas canvas, Data.Point point, Point pos)
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
                    return ClampToOrigin(new Point(otherPoint.X, otherPoint.Y), pos);
                }
            }
            return ClampToOrigin(_origin, pos);
        }

        void TryJoinPath(DesignCanvas canvas, Point pos)
        {
            if (_mouseItem is Data.Point point && Is.AtOpenBoundary(point))
            {
                var otherItem = canvas.FindItemAt(pos, ignoreItem: point);
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
