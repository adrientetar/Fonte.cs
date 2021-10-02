// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Fonte.App.Controls;
using Fonte.App.Interfaces;
using Fonte.App.Utilities;
using Fonte.Data.Utilities;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.System;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;


namespace Fonte.App.Delegates
{
    using Commands = Utilities.Commands;

    public class BaseTool : ICanvasDelegate, IToolbarItem
    {
        private Point? _previousPoint;
        private SnapResult _snapResult;
        private ThreadPoolTimer _timer;

        public CoreCursor Cursor { get; protected set; }

        protected virtual CoreCursor DefaultCursor { get; } = Cursors.SystemArrow;

        public BaseTool()
        {
            Cursor = DefaultCursor;
        }

        public virtual object FindResource(DesignCanvas canvas, string resourceKey)
        {
            return canvas.Resources[resourceKey];
        }

        public virtual void OnActivated(DesignCanvas canvas, ActivationEventArgs args)
        {
        }

        public virtual void OnDisabled(DesignCanvas canvas, ActivationEventArgs args)
        {
            CompleteMove(canvas);

            Cursor = DefaultCursor;
        }

        public virtual void OnDraw(DesignCanvas canvas, DrawEventArgs args)
        {
        }

        public virtual void OnDrawCompleted(DesignCanvas canvas, DrawEventArgs args)
        {
            if (_snapResult != null)
            {
                var ds = args.DrawingSession;
                var rescale = args.InverseScale;

                var color = (Color)FindResource(canvas, DesignCanvas.SnapLineColorKey);
                var halfSize = 2.5f * rescale;
                var refPos = _snapResult.Position;

                if (_snapResult.IsHighlightPosition)
                {
                    ds.DrawCircle(refPos, 5.5f * rescale, color, strokeWidth: rescale);
                }
                else
                {
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
        }

        public virtual void OnKeyDown(DesignCanvas canvas, KeyRoutedEventArgs args)
        {
            var control = KeyboardInput.GetKeyStateForCurrentThread(VirtualKey.Control);

            // TODO: batch and use the undoGroup reset thing on close enough keyboard moves
            if (args.Key == VirtualKey.Left ||
                args.Key == VirtualKey.Up ||
                args.Key == VirtualKey.Right ||
                args.Key == VirtualKey.Down)
            {
                int dx = 0, dy = 0;
                if (args.Key == VirtualKey.Left)
                {
                    dx = -1;
                }
                else if (args.Key == VirtualKey.Up)
                {
                    dy = 1;
                }
                else if (args.Key == VirtualKey.Right)
                {
                    dx = 1;
                }
                else if (args.Key == VirtualKey.Down)
                {
                    dy = -1;
                }
                var shift = KeyboardInput.GetKeyStateForCurrentThread(VirtualKey.Shift);
                if (shift.HasFlag(CoreVirtualKeyStates.Down))
                {
                    dx *= 10;
                    dy *= 10;
                    if (control.HasFlag(CoreVirtualKeyStates.Down))
                    {
                        dx *= 10;
                        dy *= 10;
                    }
                }

                var layer = canvas.Layer;
                using (var group = layer.CreateUndoGroup())
                {
                    Outline.MoveSelection(layer, dx, dy, GetMoveMode());

                    var selection = layer.Selection;
                    if (selection.Count == 1 && selection.First() is Data.Point point && Outline.TryJoinPath(layer, point))
                    {
                        DisplaySnapResult(
                            canvas, UIBroker.GetSnapHighlight(point));
                    }
                    else if (selection.OfType<Data.Point>().LastOrDefault() is Data.Point focusPoint)
                    {
                        DisplaySnapResult(
                            canvas, UIBroker.GetSnapLines(layer, focusPoint.ToVector2().ToPoint()));
                    }
                }
            }
            else if (args.Key == VirtualKey.Back ||
                     args.Key == VirtualKey.Delete)
            {
                var alt = KeyboardInput.GetKeyStateForCurrentThread(VirtualKey.Menu);

                Outline.DeleteSelection(canvas.Layer, breakPaths: alt.HasFlag(CoreVirtualKeyStates.Down));
            }
            else if (args.Key == VirtualKey.Enter)
            {
                var selection = canvas.Layer.Selection;
                if (selection.Count == 1 && selection.First() is Data.Anchor anchor)
                {
                    canvas.EditAnchorName(anchor);
                }
                else
                {
                    using var group = canvas.Layer.CreateUndoGroup();

                    foreach (var point in selection.OfType<Data.Point>())
                    {
                        Outline.TryTogglePointSmoothness(point);
                    }
                }
            }
            else if (control.HasFlag(CoreVirtualKeyStates.Down) && (
                         args.Key == VirtualKey.PageUp ||
                         args.Key == VirtualKey.PageDown))
            {
                var focusPoint = canvas.Layer.Selection.OfType<Data.Point>().LastOrDefault();

                if (focusPoint != null)
                {
                    var path = focusPoint.Parent;
                    var index = path.Points.IndexOf(focusPoint);

                    var point = args.Key == VirtualKey.PageDown ?
                                Sequence.NextItem(path.Points, index) :
                                Sequence.PreviousItem(path.Points, index);

                    canvas.Layer.ClearSelection();
                    point.IsSelected = true;
                }
            }
            else
            {
                return;
            }

            args.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        public virtual void OnKeyUp(DesignCanvas canvas, KeyRoutedEventArgs args)
        {
        }

        public virtual void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            if (args.GetCurrentPoint(canvas).Properties.IsMiddleButtonPressed)
            {
                _previousPoint = args.GetCurrentPoint(canvas).Position;

                Cursor = Cursors.HandGrab;
                canvas.InvalidateCursor();
            }

            canvas.Focus(FocusState.Pointer);
            args.Handled = true;
        }

        public virtual void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            if (_previousPoint.HasValue)
            {
                var point = args.GetCurrentPoint(canvas).Position;

                canvas.ScrollBy(
                    _previousPoint.Value.X - point.X,
                    _previousPoint.Value.Y - point.Y);

                _previousPoint = point;
            }

            args.Handled = true;
        }

        public virtual void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            CompleteMove(canvas);

            args.Handled = true;
        }

        public virtual void OnDoubleTapped(DesignCanvas canvas, DoubleTappedRoutedEventArgs args)
        {
        }

        // TODO: might want to do a model where we return the flyout so subclasses can take base class flyout and change things
        public virtual void OnRightTapped(DesignCanvas canvas, RightTappedRoutedEventArgs args)
        {
            var clientPos = args.GetPosition(canvas);
            var pos = canvas.FromClientPosition(clientPos);
            var tappedItem = canvas.HitTest(pos);

            var builder = new MenuFlyoutBuilder();
            if (tappedItem is Data.Component component)
            {
                builder.TryAddItem(Commands.DecomposeComponentCommand, component);
            }
            else if (tappedItem is Data.Guideline guideline)
            {
                if (guideline.Parent is Data.Layer)
                {
                    builder.TryAddItem(Commands.MakeGuidelineGlobalCommand, guideline);
                }
                else
                {
                    builder.TryAddItem(Commands.MakeGuidelineLocalCommand, (guideline, canvas.Layer));
                }
            }
            else if (tappedItem is Data.Point point)
            {
                builder.TryAddItem(Commands.SetStartPointCommand, point);
                builder.TryAddItem(Commands.ReversePathCommand, point.Parent);
            }
            builder.TryAddItem(Commands.ReverseAllPathsCommand, canvas.Layer);

            builder.TryAddSeparator();

            builder.TryAddItem(Commands.AlignSelectionCommand, canvas.Layer);

            builder.TryAddSeparator();

            builder.TryAddItem(Commands.AddComponentCommand, canvas.Layer);
            builder.TryAddItem(Commands.AddAnchorCommand, (canvas, pos));
            builder.TryAddItem(Commands.AddGuidelineCommand, (canvas, pos));

            builder.MenuFlyout.ShowAt(canvas, clientPos);
            args.Handled = true;
        }

        /**/

        protected virtual void CompleteMove(DesignCanvas canvas)
        {
            if (_previousPoint.HasValue)
            {
                _previousPoint = null;

                Cursor = DefaultCursor;
                canvas.InvalidateCursor();
            }
            if (_timer != null)
            {
                _timer?.Cancel();
                _timer = null;
            }
            _snapResult = null;
        }

        protected Point DisplaySnapResult(DesignCanvas canvas, SnapResult snapResult, double msec = 600)
        {
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
                }, TimeSpan.FromMilliseconds(msec));

                canvas.Invalidate();
            }

            return snapResult.Position.ToPoint();
        }

        protected CoreCursor GetItemCursor(object item)
        {
            if (item is UIBroker.BBoxHandle handle)
            {
                if (handle.Kind.HasFlag(UIBroker.HandleKind.TopLeft) ||
                    handle.Kind.HasFlag(UIBroker.HandleKind.BottomRight))
                {
                    return Cursors.SizeNWSE;
                }
                else if (handle.Kind.HasFlag(UIBroker.HandleKind.TopRight) ||
                         handle.Kind.HasFlag(UIBroker.HandleKind.BottomLeft))
                {
                    return Cursors.SizeNESW;
                }
                else if (handle.Kind.HasFlag(UIBroker.HandleKind.Top) ||
                         handle.Kind.HasFlag(UIBroker.HandleKind.Bottom))
                {
                    return Cursors.SizeNS;
                }
                else
                {
                    return Cursors.SizeWE;
                }
            }
            return DefaultCursor;
        }

        // origin and pos should be in screen coordinates
        protected static bool CanStartDragging(Point pos, Point origin, int px = 7)
        {
            var dx = pos.X - origin.X;
            var dy = pos.Y - origin.Y;

            return Math.Abs(dx) >= px || Math.Abs(dy) >= 1.2f * px;
        }

        protected static Point ClampToOrigin(Point pos, Point origin)
        {
            ClampToOrigin(pos, origin, out Point result);
            return result;
        }
        protected static UIBroker.Axis ClampToOrigin(Point pos, Point origin, out Point result)
        {
            var dx = pos.X - origin.X;
            var dy = pos.Y - origin.Y;

            if (Math.Abs(dy) >= Math.Abs(dx))
            {
                result = new Point(origin.X, pos.Y);
                return UIBroker.Axis.Y;
            }
            result = new Point(pos.X, origin.Y);
            return UIBroker.Axis.X;
        }

        protected static MoveMode GetMoveMode()
        {
            var alt = KeyboardInput.GetKeyStateForCurrentThread(VirtualKey.Menu);
            var windows = KeyboardInput.GetKeyStateForCurrentThread(VirtualKey.LeftWindows);

            return (windows.HasFlag(CoreVirtualKeyStates.Down), alt.HasFlag(CoreVirtualKeyStates.Down)) switch
            {
                (true, true) => MoveMode.InterpolateCurve,
                (_, true) => MoveMode.StaticHandles,
                _ => MoveMode.Normal
            };
        }

        #region IToolBarEntry implementation

        public virtual IconSource Icon { get; } = new FontIconSource() { FontSize = 16, Glyph = "\ue8b0" };

        public virtual string Name => string.Empty;

        public virtual KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator();

        #endregion
    }
}
