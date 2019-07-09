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
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
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
            var alt = Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu);
            if (alt.HasFlag(CoreVirtualKeyStates.Down) && (
                e.Key == VirtualKey.Left ||
                e.Key == VirtualKey.Right))
            {
                var focusPoint = canvas.Layer.Selection.OfType<Data.Point>().LastOrDefault();

                if (focusPoint != null)
                {
                    var path = focusPoint.Parent;
                    var index = path.Points.IndexOf(focusPoint);

                    var point = e.Key == VirtualKey.Right ?
                                Sequence.NextItem(path.Points, index) :
                                Sequence.PreviousItem(path.Points, index);

                    canvas.Layer.ClearSelection();
                    point.IsSelected = true;
                }
            }
            else if (e.Key == VirtualKey.Left ||
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
                Outline.DeleteSelection(canvas.Layer, breakPaths: alt.HasFlag(CoreVirtualKeyStates.Down));
            }
            else if (e.Key == VirtualKey.Enter)
            {
                var selection = canvas.Layer.Selection;
                if (selection.Count == 1 && selection.First() is Data.Anchor anchor)
                {
                    canvas.EditAnchorName(anchor);
                }
                else
                {
                    using (var group = canvas.Layer.CreateUndoGroup())
                    {
                        foreach (var point in selection.OfType<Data.Point>())
                        {
                            Outline.TryTogglePointSmoothness(point);
                        }
                    }
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

        // TODO: might want to do a model where we return the flyout so subclasses can take base class flyout and change things
        public virtual void OnRightTapped(DesignCanvas canvas, RightTappedRoutedEventArgs e)
        {
            var flyout = new MenuFlyout()
            {
                AreOpenCloseAnimationsEnabled = false
            };

            var clientPos = e.GetPosition(canvas);
            var pos = canvas.GetCanvasPosition(clientPos);
            var tappedItem = canvas.HitTest(pos);

            if (tappedItem is Data.Component component)
            {
                flyout.Items.Add(new MenuFlyoutItem()
                {
                    Command = DecomposeComponentCommand,
                    CommandParameter = component
                });
            }
            else if (tappedItem is Data.Guideline guideline)
            {
                if (guideline.Parent is Data.Layer)
                {
                    flyout.Items.Add(new MenuFlyoutItem()
                    {
                        Command = MakeGuidelineGlobalCommand,
                        CommandParameter = guideline
                    });
                }
                else
                {
                    flyout.Items.Add(new MenuFlyoutItem()
                    {
                        Command = MakeGuidelineLocalCommand,
                        CommandParameter = (guideline, canvas.Layer)
                    });
                }
            }
            else if (tappedItem is Data.Point point)
            {
                flyout.Items.Add(new MenuFlyoutItem()
                {
                    Command = SetStartPointCommand,
                    CommandParameter = point
                });
                flyout.Items.Add(new MenuFlyoutItem()
                {
                    Command = ReversePathCommand,
                    CommandParameter = point.Parent
                });
            }
            flyout.Items.Add(new MenuFlyoutItem()
            {
                Command = ReverseAllPathsCommand,
                CommandParameter = canvas.Layer
            });

            flyout.Items.Add(new MenuFlyoutSeparator());

            flyout.Items.Add(new MenuFlyoutItem()
            {
                Command = AddAnchorCommand,
                CommandParameter = (canvas, pos)
            });
            flyout.Items.Add(new MenuFlyoutItem()
            {
                Command = AddGuidelineCommand,
                CommandParameter = (canvas, pos)
            });

            flyout.ShowAt(canvas, clientPos);
            e.Handled = true;
        }

        /**/

        static XamlUICommand AddAnchorCommand { get; } = MakeUICommand(
            "Add Anchor",
            (s, e) => {
                var (canvas, pos) = (ValueTuple<DesignCanvas, Point>)e.Parameter;
                var layer = canvas.Layer;

                var anchor = new Data.Anchor(
                    Outline.RoundToGrid((float)pos.X),
                    Outline.RoundToGrid((float)pos.Y),
                    "new anchor"
                );
                layer.Anchors.Add(anchor);
                layer.ClearSelection();
                anchor.IsSelected = true;
                ((App)Application.Current).InvalidateData();

                canvas.EditAnchorName(anchor);
            });

        static XamlUICommand AddGuidelineCommand { get; } = MakeUICommand(
            "Add Guideline",
            (s, e) => {
                var (canvas, pos) = (ValueTuple<DesignCanvas, Point>)e.Parameter;
                var layer = canvas.Layer;

                var guideline = new Data.Guideline(
                    Outline.RoundToGrid((float)pos.X),
                    Outline.RoundToGrid((float)pos.Y),
                    0
                );
                layer.Guidelines.Add(guideline);
                layer.ClearSelection();
                guideline.IsSelected = true;
                ((App)Application.Current).InvalidateData();
            });

        static XamlUICommand DecomposeComponentCommand { get; } = MakeUICommand(
            "Decompose",
            (s, e) => {
                var component = (Data.Component)e.Parameter;

                component.Decompose();
                ((App)Application.Current).InvalidateData();
            });

        static XamlUICommand MakeGuidelineGlobalCommand { get; } = MakeUICommand(
            "Make Guideline Global",
            (s, e) => {
                var guideline = (Data.Guideline)e.Parameter;
                var layer = (Data.Layer)guideline.Parent;
                var master = layer.Master;

                layer.Guidelines.Remove(guideline);
                master.Guidelines.Add(guideline);

                ((App)Application.Current).InvalidateData();
            });

        static XamlUICommand MakeGuidelineLocalCommand { get; } = MakeUICommand(
            "Make Guideline Local",
            (s, e) => {
                var (guideline, layer) = (ValueTuple<Data.Guideline, Data.Layer>)e.Parameter;
                var master = (Data.Master)guideline.Parent;

                master.Guidelines.Remove(guideline);
                layer.Guidelines.Add(guideline);

                ((App)Application.Current).InvalidateData();
            });

        static XamlUICommand ReverseAllPathsCommand { get; } = MakeUICommand(
            "Reverse All Paths",
            (s, e) => {
                var layer = (Data.Layer)e.Parameter;

                foreach (var path in layer.Paths)
                {
                    path.Reverse();
                }
                ((App)Application.Current).InvalidateData();
            });

        static XamlUICommand ReversePathCommand { get; } = MakeUICommand(
            "Reverse Path",
            (s, e) => {
                var path = (Data.Path)e.Parameter;

                path.Reverse();
                ((App)Application.Current).InvalidateData();
            });

        static XamlUICommand SetStartPointCommand { get; } = MakeUICommand(
            "Set As Start Point",
            (s, e) => {
                var point = (Data.Point)e.Parameter;
                var path = point.Parent;

                path.StartAt(path.Points.IndexOf(point));
                ((App)Application.Current).InvalidateData();
            });

        /**/

        // origin and pos should be in screen coordinates
        protected bool CanStartDragging(Point pos, Point origin, int px = 7)
        {
            var dx = pos.X - origin.X;
            var dy = pos.Y - origin.Y;

            return Math.Abs(dx) >= px || Math.Abs(dy) >= 1.2f * px;
        }

        protected Point ClampToOrigin(Point pos, Point origin)
        {
            var dx = pos.X - origin.X;
            var dy = pos.Y - origin.Y;

            if (Math.Abs(dy) >= Math.Abs(dx))
            {
                return new Point(origin.X, pos.Y);
            }
            return new Point(pos.X, origin.Y);
        }

        protected MoveMode GetMoveMode()
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

        protected static XamlUICommand MakeUICommand(string label,
                                                     TypedEventHandler<XamlUICommand, ExecuteRequestedEventArgs> executeHandler,
                                                     KeyboardAccelerator accelerator = null)
        {
            var command = new XamlUICommand()
            {
                Label = label,
            };
            //command.CanExecuteRequested += canExecuteHandler;
            command.ExecuteRequested += executeHandler;
            if (accelerator != null)
            {
                command.KeyboardAccelerators.Add(accelerator);
            }

            return command;
        }

        #region IToolBarEntry implementation

        public virtual IconSource Icon { get; } = new FontIconSource() { FontSize = 16, Glyph = "\ue8b0" };

        public virtual string Name => string.Empty;

        public virtual KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator();

        #endregion
    }
}
