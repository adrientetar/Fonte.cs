/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Fonte.App.Interfaces;
    using Fonte.App.Utilities;
    using Fonte.Data.Utilities;
    using Microsoft.Graphics.Canvas;

    using System;
    using Windows.Devices.Input;
    using Windows.Foundation;
    using Windows.System;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public class BaseTool : ICanvasDelegate, IToolbarItem
    {
        private Point? _previousPoint;

        public virtual CoreCursor Cursor { get; } = new CoreCursor(CoreCursorType.Arrow, 0);

        public virtual object FindResource(DesignCanvas canvas, object resourceKey)
        {
            return canvas.Resources[resourceKey];
        }

        public virtual bool HandlePointerEvent(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            return e.Pointer.PointerDeviceType == PointerDeviceType.Mouse;
        }

        public virtual void OnActivated(DesignCanvas canvas)
        {
        }

        public virtual void OnDisabled(DesignCanvas canvas)
        {
        }

        public virtual void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
        }

        public virtual void OnDrawCompleted(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
        }

        public virtual void OnKeyDown(DesignCanvas canvas, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Left ||
                e.Key == VirtualKey.Up ||
                e.Key == VirtualKey.Right ||
                e.Key == VirtualKey.Down)
            {
                int dx = 0, dy = 0;
                if (e.Key == VirtualKey.Left)
                {
                    dx = -1;
                }
                else if (e.Key == VirtualKey.Up)
                {
                    dy = 1;
                }
                else if (e.Key == VirtualKey.Right)
                {
                    dx = 1;
                }
                else if (e.Key == VirtualKey.Down)
                {
                    dy = -1;
                }
                var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
                if (shift.HasFlag(CoreVirtualKeyStates.Down))
                {
                    dx *= 10;
                    dy *= 10;
                    var control = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
                    if (control.HasFlag(CoreVirtualKeyStates.Down))
                    {
                        dx *= 10;
                        dy *= 10;
                    }
                }

                Outline.MoveSelection(canvas.Layer, dx, dy, GetMoveMode());
            }
            else if (e.Key == VirtualKey.Back ||
                     e.Key == VirtualKey.Delete)
            {
                var alt = Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu);
                Outline.DeleteSelection(canvas.Layer, breakPaths: alt.HasFlag(CoreVirtualKeyStates.Down));
            }
            else if (e.Key == VirtualKey.Enter)
            {
                using (var group = canvas.Layer.CreateUndoGroup())
                {
                    foreach (var path in canvas.Layer.Paths)
                    {
                        var index = -1;
                        foreach (var point in path.Points)
                        {
                            ++index;

                            if (point.Selected && point.Type != Data.PointType.None)
                            {
                                var before = path.Points[Sequence.PreviousIndex(path.Points, index)];
                                if (before.Type != Data.PointType.None)
                                {
                                    var after = path.Points[Sequence.NextIndex(path.Points, index)];
                                    if (after.Type != Data.PointType.None)
                                    {
                                        continue;
                                    }
                                }

                                point.Smooth = !point.Smooth;
                            }
                        }
                    }
                }
            }
            else if (e.Key == VirtualKey.Tab)
            {
                Data.Point focusPoint = null;
                foreach (var item in canvas.Layer.Selection)
                {
                    if (item is Data.Point point)
                    {
                        focusPoint = point;
                    }
                }

                if (focusPoint != null)
                {
                    var path = focusPoint.Parent;
                    var index = path.Points.IndexOf(focusPoint);

                    var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
                    int ix;
                    if (shift.HasFlag(CoreVirtualKeyStates.Down))
                    {
                        ix = Sequence.PreviousIndex(path.Points, index);
                    }
                    else
                    {
                        ix = Sequence.NextIndex(path.Points, index);
                    }

                    canvas.Layer.ClearSelection();
                    path.Points[ix].Selected = true;
                }
            }
            else
            {
                return;
            }

            e.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        public virtual void OnKeyUp(DesignCanvas canvas, KeyRoutedEventArgs e)
        {
        }

        public virtual void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(canvas).Properties.IsMiddleButtonPressed)
            {
                _previousPoint = e.GetCurrentPoint(canvas).Position;
            }

            canvas.Focus(FocusState.Pointer);
            e.Handled = true;
        }

        public virtual void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs e)
        {            
            if (_previousPoint.HasValue)
            {
                var point = e.GetCurrentPoint(canvas).Position;

                canvas.ScrollBy(
                    _previousPoint.Value.X - point.X,
                    _previousPoint.Value.Y - point.Y);

                _previousPoint = point;
            }

            e.Handled = true;
        }

        public virtual void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            _previousPoint = null;

            e.Handled = true;
        }

        public virtual void OnDoubleTapped(DesignCanvas canvas, DoubleTappedRoutedEventArgs e)
        {
        }

        public virtual void OnRightTapped(DesignCanvas canvas, RightTappedRoutedEventArgs e)
        {
        }

        // origin and pos should be in screen coordinates
        // XXX: should we convert internally? tools need to maintain screen pos only for this check ;(
        public bool CanStartDragging(Point origin, Point pos)
        {
            var dx = pos.X - origin.X;
            var dy = pos.Y - origin.Y;

            return Math.Abs(dx) >= 8 || Math.Abs(dy) >= 9;
        }

        public Point ClampToOrigin(Point origin, Point pos)
        {
            var dx = pos.X - origin.X;
            var dy = pos.Y - origin.Y;

            if (Math.Abs(dy) >= Math.Abs(dx))
            {
                return new Point(origin.X, pos.Y);
            }
            return new Point(pos.X, origin.Y);
        }

        public MoveMode GetMoveMode()
        {
            var alt = Window.Current.CoreWindow.GetKeyState(VirtualKey.LeftMenu);
            var windows = Window.Current.CoreWindow.GetKeyState(VirtualKey.LeftWindows);

            MoveMode mode;
            if (windows.HasFlag(CoreVirtualKeyStates.Down) &&
                alt.HasFlag(CoreVirtualKeyStates.Down))
            {
                mode = MoveMode.InterpolateCurve;
            }
            else if (alt.HasFlag(CoreVirtualKeyStates.Down))
            {
                mode = MoveMode.StaticHandles;
            }
            else
            {
                mode = MoveMode.Normal;
            }
            return mode;
        }

        protected static XamlUICommand MakeUICommand(string label, KeyboardAccelerator accelerator)
        {
            var command = new XamlUICommand()
            {
                Label = label,
            };
            command.KeyboardAccelerators.Add(accelerator);

            return command;
        }

        #region IToolBarEntry implementation

        public virtual IconElement Icon { get; } = new SymbolIcon() { Symbol = Symbol.Add };

        public virtual string Name => string.Empty;

        public virtual KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator();

        #endregion
    }
}
