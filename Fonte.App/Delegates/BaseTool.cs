﻿/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Commands;
    using Fonte.App.Controls;
    using Fonte.App.Interfaces;
    using Fonte.App.Utilities;
    using Fonte.Data.Utilities;
    using Microsoft.Graphics.Canvas;

    using System;
    using System.Linq;
    using System.Windows.Input;
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

        public CoreCursor Cursor { get; protected set; }

        protected virtual CoreCursor DefaultCursor { get; } = Cursors.SystemArrow;

        public BaseTool()
        {
            Cursor = DefaultCursor;
        }

        public virtual object FindResource(DesignCanvas canvas, object resourceKey)
        {
            return canvas.Resources[resourceKey];
        }

        public virtual bool HandlePointerEvent(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            return args.Pointer.PointerDeviceType == PointerDeviceType.Mouse;
        }

        public virtual void OnActivated(DesignCanvas canvas)
        {
        }

        public virtual void OnDisabled(DesignCanvas canvas)
        {
            Cursor = DefaultCursor;
        }

        public virtual void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
        }

        public virtual void OnDrawCompleted(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
        }

        public virtual void OnKeyDown(DesignCanvas canvas, KeyRoutedEventArgs args)
        {
            var control = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            var alt = Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu);

            if (!alt.HasFlag(CoreVirtualKeyStates.Down) && (
                    args.Key == VirtualKey.Left ||
                    args.Key == VirtualKey.Up ||
                    args.Key == VirtualKey.Right ||
                    args.Key == VirtualKey.Down))
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
                var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
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
                    if (selection.Count == 1 && selection.First() is Data.Point point)
                    {
                        // TODO: add some kind of visual feedback with a timer?
                        Outline.TryJoinPath(layer, point);
                    }
                }
            }
            else if (args.Key == VirtualKey.Back ||
                     args.Key == VirtualKey.Delete)
            {
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
                    using (var group = canvas.Layer.CreateUndoGroup())
                    {
                        foreach (var point in selection.OfType<Data.Point>())
                        {
                            Outline.TryTogglePointSmoothness(point);
                        }
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
            if (_previousPoint.HasValue)
            {
                _previousPoint = null;

                Cursor = DefaultCursor;
                canvas.InvalidateCursor();
            }

            args.Handled = true;
        }

        public virtual void OnDoubleTapped(DesignCanvas canvas, DoubleTappedRoutedEventArgs args)
        {
        }

        // TODO: might want to do a model where we return the flyout so subclasses can take base class flyout and change things
        public virtual void OnRightTapped(DesignCanvas canvas, RightTappedRoutedEventArgs args)
        {
            var flyout = new MenuFlyout()
            {
                AreOpenCloseAnimationsEnabled = false
            };

            var clientPos = args.GetPosition(canvas);
            var pos = canvas.FromClientPosition(clientPos);
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
                Command = AddComponentCommand,
                CommandParameter = canvas.Layer
            });
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
            args.Handled = true;
        }

        /**/

        // TODO: maybe take this static library to a separate file?
        static XamlUICommand AddAnchorCommand { get; } = MakeUICommand("Add Anchor", new AddAnchorCommand());
        static XamlUICommand AddComponentCommand { get; } = MakeUICommand("Add Component", new AddComponentCommand());
        static XamlUICommand AddGuidelineCommand { get; } = MakeUICommand("Add Guideline", new AddGuidelineCommand());
        static XamlUICommand DecomposeComponentCommand { get; } = MakeUICommand("Decompose", new DecomposeComponentCommand());
        static XamlUICommand MakeGuidelineGlobalCommand { get; } = MakeUICommand("Make Guideline Global", new MakeGuidelineGlobalCommand());
        static XamlUICommand MakeGuidelineLocalCommand { get; } = MakeUICommand("Make Guideline Local", new MakeGuidelineLocalCommand());
        static XamlUICommand ReverseAllPathsCommand { get; } = MakeUICommand("Reverse All Paths", new ReverseAllPathsCommand());
        static XamlUICommand ReversePathCommand { get; } = MakeUICommand("Reverse Path", new ReversePathCommand());
        static XamlUICommand SetStartPointCommand { get; } = MakeUICommand("Set As Start Point", new SetStartPointCommand());

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

        protected MoveMode GetMoveMode()
        {
            var alt = Window.Current.CoreWindow.GetKeyState(VirtualKey.Menu);
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);

            MoveMode mode;
            if (shift.HasFlag(CoreVirtualKeyStates.Down) &&
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

        protected static XamlUICommand MakeUICommand(string label, ICommand command, KeyboardAccelerator accelerator = null)
        {
            var uiCommand = new XamlUICommand()
            {
                Command = command,
                Label = label,
            };
            if (accelerator != null)
            {
                uiCommand.KeyboardAccelerators.Add(accelerator);
            }

            return uiCommand;
        }

        #region IToolBarEntry implementation

        public virtual IconSource Icon { get; } = new FontIconSource() { FontSize = 16, Glyph = "\ue8b0" };

        public virtual string Name => string.Empty;

        public virtual KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator();

        #endregion
    }
}
