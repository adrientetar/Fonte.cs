/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App
{
    using Fonte.App.Interfaces;
    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Windows.System;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public sealed partial class CanvasPage : Page
    {
        public static DependencyProperty FontProperty = DependencyProperty.Register(
            "Font", typeof(Data.Font), typeof(CanvasPage), null);

        public Data.Font Font
        {
            get => (Data.Font)GetValue(FontProperty);
            set { SetValue(FontProperty, value); }
        }

        public static DependencyProperty CurrentLayerProperty = DependencyProperty.Register(
            "CurrentLayer", typeof(Data.Layer), typeof(CanvasPage), null);

        public Data.Layer CurrentLayer
        {
            get => (Data.Layer)GetValue(CurrentLayerProperty);
            set { SetValue(CurrentLayerProperty, value); }
        }

        public CanvasPage()
        {
            InitializeComponent();

            // TODO: create standard glyphset in font and foreground layer in glyph by default?
            var layer = new Data.Layer();

            Font = new Data.Font(
                glyphs: new List<Data.Glyph>()
                {
                    new Data.Glyph(layers: new List<Data.Layer>() { layer })
                });

            CurrentLayer = layer;

#if DEBUG
            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
#endif
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
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            if (ctrl.HasFlag(CoreVirtualKeyStates.Down) && e.VirtualKey == VirtualKey.D)
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
            else
            {
                return;
            }

            e.Handled = true;
        }

        void OnRedoInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
        {
            var undoStore = Canvas.Layer.Parent.UndoStore;
            if (undoStore.CanRedo)
            {
                undoStore.Redo();
                ((App)Application.Current).InvalidateData();
            }

            e.Handled = true;
        }

        void OnResetZoomInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
        {
            Canvas.CenterOnMetrics();

            e.Handled = true;
        }

        void OnShowSidebarInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
        {
            Sidebar.Visibility = Sidebar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            e.Handled = true;
        }

        void OnSelectAllInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
        {
            var pathsSelected = true;
            foreach (var path in Canvas.Layer.Paths)
            {
                if (!path.IsSelected)
                {
                    pathsSelected = false;
                    path.IsSelected = true;
                }
            }
            if (pathsSelected)
            {
                foreach (var anchor in Canvas.Layer.Anchors)
                {
                    anchor.Selected = true;
                }
                foreach (var component in Canvas.Layer.Components)
                {
                    component.Selected = true;
                }
            }

            e.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        void OnUndoInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
        {
            var undoStore = Canvas.Layer.Parent.UndoStore;
            if (undoStore.CanUndo)
            {
                undoStore.Undo();
                ((App)Application.Current).InvalidateData();
            }

            e.Handled = true;
        }

        /**/

        void OnToolbarItemChanged(object sender, EventArgs e)
        {
            Canvas.Tool = (ICanvasDelegate)Toolbar.SelectedItem;
        }
    }
}
