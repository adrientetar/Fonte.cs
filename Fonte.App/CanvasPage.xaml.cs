/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App
{
    using Fonte.App.Interfaces;

    using System;
    using System.Diagnostics;
    using Windows.System;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public sealed partial class CanvasPage : Page
    {
        public CanvasPage()
        {
            InitializeComponent();
        }

        void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown += OnWindowKeyDown;
        }

        void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.CoreWindow.KeyDown -= OnWindowKeyDown;
        }

        void OnWindowKeyDown(CoreWindow sender, KeyEventArgs e)
        {
            var undoStore = Canvas.Layer.Parent.UndoStore;
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            var shift = Window.Current.CoreWindow.GetKeyState(VirtualKey.Shift);
            if (ctrl.HasFlag(CoreVirtualKeyStates.Down) && (
                    shift.HasFlag(CoreVirtualKeyStates.Down) &&
                        e.VirtualKey == VirtualKey.Z ||
                    e.VirtualKey == VirtualKey.Y))
            {
                if (undoStore.CanRedo)
                {
                    undoStore.Redo();
                    Canvas.Invalidate();
                }
            }
            else if (ctrl.HasFlag(CoreVirtualKeyStates.Down) && e.VirtualKey == VirtualKey.Z)
            {
                if (undoStore.CanUndo)
                {
                    undoStore.Undo();
                    Canvas.Invalidate();
                }
            }
            else if (ctrl.HasFlag(CoreVirtualKeyStates.Down) && e.VirtualKey == VirtualKey.P)
            {
                Sidebar.Visibility = Sidebar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (ctrl.HasFlag(CoreVirtualKeyStates.Down) && e.VirtualKey == VirtualKey.Number0)
            {
                Canvas.FitMetrics();
            }
#if DEBUG
            else if (ctrl.HasFlag(CoreVirtualKeyStates.Down) && e.VirtualKey == VirtualKey.D)
            {
                try
                {
                    var path = Canvas.Layer.Paths.Last();

                    foreach (var point in path.Points)
                    {
                        Debug.WriteLine(point);
                    }
                    Debug.WriteLine("");
                }
                catch (InvalidOperationException)
                {
                    return;
                }
            }
#endif
        }

        void OnSelectAllInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            foreach (var path in Canvas.Layer.Paths)
            {
                path.Selected = true;
            }

            ((App)Application.Current).InvalidateData();
        }

        void OnToolbarItemChanged(object sender, EventArgs e)
        {
            Canvas.Tool = (ICanvasDelegate)Toolbar.SelectedItem;
        }
    }
}
