/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Fonte.App.Utilities;
    using Fonte.Data.Interfaces;
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
        private Point _anchor;
        private object _mouseItem;
        private ValueTuple<CanvasGeometry, CanvasGeometry> _oldPaths;
        private Point _origin;
        private IChangeGroup _undoGroup;

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
                else if (_anchor != _origin)
                {
                    return ActionType.DraggingRect;
                }
                return ActionType.None;
            }
        }

        public SelectionTool()
        {
        }

        public override void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
            if (CurrentAction == ActionType.DraggingPoint)
            {
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

            if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
            {
                _origin = _anchor = canvas.GetLocalPosition(e);
                _mouseItem = canvas.ItemAt(_origin);

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
                }
                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerMoved(canvas, e);

            if (e.GetCurrentPoint(canvas).Properties.IsLeftButtonPressed)
            {
                var pos = canvas.GetLocalPosition(e);

                if (_mouseItem is Data.Point point)
                {
                    var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
                    if (shift.HasFlag(CoreVirtualKeyStates.Down))
                    {
                        pos = _clampToNodeOrOrigin(canvas, point, pos);
                    }

                    MoveSelection(
                        canvas,
                        (float)(pos.X - _origin.X),
                        (float)(pos.Y - _origin.Y)
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
                _tryJoinPath(canvas, canvas.GetLocalPosition(e));
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

                _anchor = _origin;
            }

            _mouseItem = null;
            ((App)Application.Current).InvalidateData();

            Debug.Assert(CurrentAction == ActionType.None);
        }

        public override void OnRightTapped(DesignCanvas canvas, RightTappedRoutedEventArgs e)
        {
#if DEBUG
            var flyout = new MenuFlyout();

            flyout.Items.Add(new MenuFlyoutItem
            {
                Text = "Hello World!",
                Icon = new FontIcon() { Glyph = "\ue76e" }
            });

            flyout.ShowAt(canvas, e.GetPosition(canvas));

            e.Handled = true;
#endif
        }

        private Point _clampToNodeOrOrigin(DesignCanvas canvas, Data.Point point, Point pos)
        {
            // We clamp to the mousedown pos, unless we have a single offcurve
            // in which case we clamp it against its parent.
            if (point.Type == Data.PointType.None && canvas.Layer.Selection.Count == 1)
            {
                var path = point.Parent;
                var index = path.Points.IndexOf(point);
                var otherPoint = path.Points[(index - 1) % path.Points.Count];
                if (otherPoint.Type == Data.PointType.None)
                {
                    otherPoint = path.Points[(index + 1) % path.Points.Count];
                }
                if (otherPoint.Type != Data.PointType.None)
                {
                    return ClampToOrigin(new Point(otherPoint.X, otherPoint.Y), pos);
                }
            }
            return ClampToOrigin(_origin, pos);
        }

        private void _tryJoinPath(DesignCanvas canvas, Point pos)
        {
            if (_mouseItem is Data.Point point && Is.AtOpenBoundary(point))
            {
                var otherItem = canvas.ItemAt(pos, ignoreItem: point);
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
