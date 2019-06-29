/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App
{
    using Fonte.App.Interfaces;
    using Fonte.Data.Utilities;
    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Windows.ApplicationModel;
    using Windows.Storage;
    using Windows.Storage.Pickers;
    using Windows.System;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public sealed partial class CanvasPage : Page
    {
        private StorageFile _file;

        public static DependencyProperty FontProperty = DependencyProperty.Register(
            "Font", typeof(Data.Font), typeof(CanvasPage), new PropertyMetadata(null, OnFontChanged));

        public Data.Font Font
        {
            get => (Data.Font)GetValue(FontProperty);
            set { SetValue(FontProperty, value); }
        }

        public CanvasPage()
        {
            InitializeComponent();

            // Maybe have a standard glyphset?
            Font = new Data.Font(
                    glyphs: new List<Data.Glyph>()
                    {
                        new Data.Glyph("a", new List<string>() { "0061" }, layers: new List<Data.Layer>()
                        {
                            new Data.Layer("Regular")
                        })
                    }
                );

            OnDataRefreshing();

            Loaded += OnPageLoaded;
            Unloaded += OnPageUnloaded;
        }

        void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            if (!DesignMode.DesignMode2Enabled)
            {
                ((App)Application.Current).DataRefreshing += OnDataRefreshing;
            }

            Window.Current.CoreWindow.KeyDown += OnWindowKeyDown;
            Window.Current.CoreWindow.KeyUp += OnWindowKeyUp;
        }

        void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            if (!DesignMode.DesignMode2Enabled)
            {
                ((App)Application.Current).DataRefreshing -= OnDataRefreshing;
            }

            Window.Current.CoreWindow.KeyUp -= OnWindowKeyUp;
        }

        void OnDataRefreshing()
        {
            if (Font.IsModified)
            {
                TitleBar.UserTitle = $"*{Font.FamilyName}";
            }
            else
            {
                TitleBar.UserTitle = Font.FamilyName;
            }
        }

        void OnWindowKeyDown(CoreWindow sender, KeyEventArgs e)
        {
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            if (e.VirtualKey == VirtualKey.Space)
            {
                Canvas.IsInPreview = true;
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
            else
            {
                return;
            }

            e.Handled = true;
        }

        void OnWindowKeyUp(CoreWindow sender, KeyEventArgs e)
        {
            if (e.VirtualKey == VirtualKey.Space)
            {
                Canvas.IsInPreview = false;
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
                    anchor.IsSelected = true;
                }
                foreach (var component in Canvas.Layer.Components)
                {
                    component.IsSelected = true;
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

        void OnNextGlyphInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
        {
            var index = Font.Glyphs.IndexOf(Canvas.Layer.Parent);
            Canvas.Layer = Sequence.NextItem(Font.Glyphs, index).Layers.First();

            e.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        void OnPreviousGlyphInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs e)
        {
            var index = Font.Glyphs.IndexOf(Canvas.Layer.Parent);
            Canvas.Layer = Sequence.PreviousItem(Font.Glyphs, index).Layers.First();

            e.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        /**/

        void OnFontChanged()
        {
            try
            {
                Sidebar.Layer = Font.Glyphs[0].Layers[0];
            }
            catch (ArgumentOutOfRangeException)
            {
                Sidebar.Layer = null;
            }
        }

        static void OnFontChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((CanvasPage)sender).OnFontChanged();
        }

        async void OnOpenItemClicked(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".tfont");

            if (await picker.PickSingleFileAsync() is StorageFile file)
            {

                var json = await FileIO.ReadTextAsync(file);

                Font = JsonConvert.DeserializeObject<Data.Font>(json);

                _file = file;
                OnDataRefreshing();
            }
        }

        async void OnSaveItemClicked(object sender, RoutedEventArgs e)
        {
            var file = _file;
            if (file is null)
            {
                var picker = new FileSavePicker
                {
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                };
                picker.FileTypeChoices.Add("Font file", new List<string>() { ".tfont" });
                picker.SuggestedFileName = Font.FamilyName;

                file = await picker.PickSaveFileAsync();
            }
            if (file is StorageFile)
            {
                var json = JsonConvert.SerializeObject(Font);

                await FileIO.WriteTextAsync(file, json);

                _file = file;
                OnDataRefreshing();
            }
        }

        /**/

        void OnToolbarItemChanged(object sender, EventArgs e)
        {
            Canvas.Tool = (ICanvasDelegate)Toolbar.SelectedItem;
        }
    }
}
