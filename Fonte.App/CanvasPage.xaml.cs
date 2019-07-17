﻿/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App
{
    using Fonte.App.Dialogs;
    using Fonte.App.Interfaces;
    using Fonte.Data.Utilities;
    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.ApplicationModel;
    using Windows.Storage;
    using Windows.Storage.Pickers;
    using Windows.System;
    using Windows.UI.Core;
    using Windows.UI.Core.Preview;
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
            Font = new Data.Font(glyphs: new List<Data.Glyph>()
                {
                    new Data.Glyph("a", new List<string>() { "0061" }, layers: new List<Data.Layer>()
                    {
                        new Data.Layer("Regular")
                    })
                }
            );

            OnDataRefreshing();
        }

        public async Task<bool> SaveAsync()
        {
            var file = _file;
            if (file is null)
            {
                var picker = new FileSavePicker
                {
                    SuggestedFileName = Font.FamilyName,
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary
                };
                picker.FileTypeChoices.Add("Font file", new List<string>() { ".tfont" });

                file = await picker.PickSaveFileAsync();
            }
            if (file is StorageFile)
            {
                var json = JsonConvert.SerializeObject(Font);

                await FileIO.WriteTextAsync(file, json);

                _file = file;
                OnDataRefreshing();

                return true;
            }
            return false;
        }

        void OnPageLoaded(object sender, RoutedEventArgs args)
        {
            if (!DesignMode.DesignMode2Enabled)
            {
                ((App)Application.Current).DataRefreshing += OnDataRefreshing;
            }

            Window.Current.CoreWindow.KeyDown += OnWindowKeyDown;
            Window.Current.CoreWindow.KeyUp += OnWindowKeyUp;

            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += OnCloseRequested;
        }

        void OnPageUnloaded(object sender, RoutedEventArgs args)
        {
            if (!DesignMode.DesignMode2Enabled)
            {
                ((App)Application.Current).DataRefreshing -= OnDataRefreshing;
            }

            Window.Current.CoreWindow.KeyDown -= OnWindowKeyDown;
            Window.Current.CoreWindow.KeyUp -= OnWindowKeyUp;

            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested -= OnCloseRequested;
        }

        async void OnCloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs args)
        {
            if (Font.IsModified)
            {
                var deferral = args.GetDeferral();

                ContentDialogResult result;
                {
                    var dialog = new ContentDialog()
                    {
                        Title = $"Do you want to save your changes to “{Font.FamilyName}” before closing?",
                        PrimaryButtonText = "Save",
                        SecondaryButtonText = "Don’t save",
                        CloseButtonText = "Cancel"
                    };

                    result = await dialog.ShowAsync();
                }

                if (result.HasFlag(ContentDialogResult.Primary) && await SaveAsync())
                {
                }
                else if (result.HasFlag(ContentDialogResult.Secondary))
                {
                }
                else
                {
                    // Decline window close
                    args.Handled = true;
                }
                deferral.Complete();
            }
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

        void OnWindowKeyDown(CoreWindow sender, KeyEventArgs args)
        {
            var ctrl = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            if (args.VirtualKey == VirtualKey.Space)
            {
                Canvas.IsInPreview = true;
            }
#if DEBUG
            else if (ctrl.HasFlag(CoreVirtualKeyStates.Down) && args.VirtualKey == VirtualKey.D)
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

            args.Handled = true;
        }

        void OnWindowKeyUp(CoreWindow sender, KeyEventArgs args)
        {
            if (args.VirtualKey == VirtualKey.Space)
            {
                Canvas.IsInPreview = false;
            }
            else
            {
                return;
            }

            args.Handled = true;
        }

        /**/

        void OnRedoInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var undoStore = Canvas.Layer.Parent.UndoStore;
            if (undoStore.CanRedo)
            {
                undoStore.Redo();
                ((App)Application.Current).InvalidateData();
            }

            args.Handled = true;
        }

        void OnResetZoomInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            Canvas.CenterOnMetrics();

            args.Handled = true;
        }

        void OnSelectAllInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
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

            args.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        void OnUndoInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var undoStore = Canvas.Layer.Parent.UndoStore;
            if (undoStore.CanUndo)
            {
                undoStore.Undo();
                ((App)Application.Current).InvalidateData();
            }

            args.Handled = true;
        }

        void OnNextGlyphInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var index = Font.Glyphs.IndexOf(Canvas.Layer.Parent);
            Sidebar.Layer = Sequence.NextItem(Font.Glyphs, index).Layers.First();

            args.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        void OnPreviousGlyphInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var index = Font.Glyphs.IndexOf(Canvas.Layer.Parent);
            Sidebar.Layer = Sequence.PreviousItem(Font.Glyphs, index).Layers.First();

            args.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        async void OnFindGlyphInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var glyph = Canvas.Layer.Parent;
            var glyphList = Font.Glyphs
                                .Where(g => g.Name != glyph.Name)
                                .ToList();
            if (glyphList.Count > 0)
            {
                args.Handled = true;

                var glyphDialog = new GlyphDialog(glyphList);
                await glyphDialog.ShowAsync();

                if (glyphDialog.Glyph is Data.Glyph newGlyph)
                {
                    Sidebar.Layer = newGlyph.Layers[0];
                }
            }
        }

        void OnShowSidebarInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            Sidebar.Visibility = Sidebar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            args.Handled = true;
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

        static void OnFontChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((CanvasPage)sender).OnFontChanged();
        }

        async void OnOpenItemClicked(object sender, RoutedEventArgs args)
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

        async void OnSaveItemClicked(object sender, RoutedEventArgs args) => await SaveAsync();

        /**/

        void OnToolbarItemChanged(object sender, EventArgs args)
        {
            Canvas.Tool = (ICanvasDelegate)Toolbar.SelectedItem;
        }
    }
}
