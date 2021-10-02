// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Fonte.App.Controls;
using Fonte.App.Dialogs;
using Fonte.App.Interfaces;
using Fonte.App.Serialization;
using Fonte.App.Utilities;
using Fonte.Data.Utilities;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Core;


namespace Fonte.App.UI
{
    public sealed partial class MainWindow : Window
    {
        private StorageFile _file;
        private Window _previewWindow;
        private Data.Font _font;

        const string JsonFormatId = "Fonte.App.Json";

        public StorageFile File
        {
            get => _file;
            set
            {
                if (value != _file)
                {
                    ReadFontAsync(value);
                    StorageApplicationPermissions.MostRecentlyUsedList.Add(value, string.Empty, RecentStorageItemVisibility.AppAndSystem);

                    _file = value;
                }
            }
        }

        //public static DependencyProperty FontProperty = DependencyProperty.Register(
        //    "Font", typeof(Data.Font), typeof(MainWindow), new PropertyMetadata(null, OnFontChanged));

        public Data.Font Font
        {
            get => _font;
            private set
            {
                _font = value;
                OnFontChanged();
                UpdateTitle();
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            Font = CreateNewFont();
        }

        public async Task OpenFontAsync()
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".tfont");

            if (await picker.PickSingleFileAsync() is StorageFile file)
            {
                File = file;
            }
        }

        public async Task<bool> SaveFontAsync()
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
                var json = JsonBroker.SerializeFont(Font);

                await FileIO.WriteTextAsync(file, json);
                _file = file;

                Font.IsModified = false;
                UpdateTitle();

                return true;
            }
            return false;
        }

        async Task<bool> CanDiscardAsync()
        {
            if (!Font.IsModified)
                return true;

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

            if (result == ContentDialogResult.Primary && await SaveFontAsync())
            {
                return true;
            }
            else if (result == ContentDialogResult.Secondary)
            {
                return true;
            }
            return false;
        }

        // TODO: move that stuff to a command
        void Copy()
        {
            var json = JsonConvert.SerializeObject(Canvas.Layer, new LayerSelectionConverter());
            var pkg = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            pkg.SetData(JsonFormatId, json);
            Clipboard.SetContent(pkg);
        }

        async void ReadFontAsync(StorageFile file)
        {
            var json = await FileIO.ReadTextAsync(file);

            Font = JsonConvert.DeserializeObject<Data.Font>(json);
        }

        void OnWindowActivated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
        {
            if (!DesignMode.DesignMode2Enabled)
            {
                ((App)Application.Current).DataChanged += OnDataChanged;
            }

            //Window.Current.CoreWindow.KeyDown += OnWindowKeyDown;
        }

        async void OnWindowClosed(object sender, WindowEventArgs args)
        {
            if (!DesignMode.DesignMode2Enabled)
            {
                ((App)Application.Current).DataChanged -= OnDataChanged;
            }

            //Window.Current.CoreWindow.KeyDown -= OnWindowKeyDown;

            if (!await CanDiscardAsync())
            {
                // Decline window close
                args.Handled = true;
            }
        }

        void OnDataChanged(object sender, EventArgs args)
        {
            UpdateTitle();
        }

        void OnWindowKeyDown(CoreWindow sender, KeyEventArgs args)
        {
#if DEBUG
            var ctrl = KeyboardInput.GetKeyStateForCurrentThread(VirtualKey.Control);
            if (ctrl.HasFlag(CoreVirtualKeyStates.Down) && args.VirtualKey == VirtualKey.D)
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

            args.Handled = true;
#endif
        }

        /**/

        void OnRedoInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            var undoStore = Canvas.Layer.Parent.UndoStore;
            if (undoStore.CanRedo)
            {
                undoStore.Redo();

                Canvas.Focus(FocusState.Programmatic);
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
            var layer = Canvas.Layer;
            var pathsSelected = true;
            foreach (var path in layer.Paths)
            {
                if (!path.IsSelected)
                {
                    pathsSelected = false;
                    path.IsSelected = true;
                }
            }
            if (pathsSelected)
            {
                foreach (var anchor in layer.Anchors)
                {
                    anchor.IsSelected = true;
                }
                foreach (var component in layer.Components)
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

                Canvas.Focus(FocusState.Programmatic);
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

        void OnCutInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;

            Copy();
            Outline.DeleteSelection(Canvas.Layer, true);

            ((App)Application.Current).InvalidateData();
        }

        void OnCopyInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;

            Copy();
        }

        async void OnPasteInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;

            var layer = Canvas.Layer;
            var pkgView = Clipboard.GetContent();
            if (pkgView.Contains(JsonFormatId))
            {
                var json = (string)await pkgView.GetDataAsync(JsonFormatId);

                var obj = JObject.Parse(json);
                if (obj.Count > 0)
                {
                    layer.ClearSelection();

                    if (obj.TryGetValue("anchors", out JToken tok))
                    {
                        var anchors = tok.ToObject<List<Data.Anchor>>();

                        layer.Anchors.AddRange(anchors);
                        foreach (var anchor in anchors) { anchor.IsSelected = true; }
                    }
                    if (obj.TryGetValue("components", out JToken tok_))
                    {
                        var components = tok_.ToObject<List<Data.Component>>();

                        layer.Components.AddRange(components);
                        foreach (var component in components) { component.IsSelected = true; }
                    }
                    if (obj.TryGetValue("guidelines", out JToken tok__))
                    {
                        var guidelines = tok__.ToObject<List<Data.Guideline>>();

                        layer.Guidelines.AddRange(guidelines);
                        foreach (var guideline in guidelines) { guideline.IsSelected = true; }
                    }
                    if (obj.TryGetValue("paths", out JToken tok___))
                    {
                        var paths = tok___.ToObject<List<Data.Path>>();

                        layer.Paths.AddRange(paths);
                        foreach (var path in paths) { path.IsSelected = true; }
                    }

                    ((App)Application.Current).InvalidateData();
                }
            }
        }

        void OnShowSidebarInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            Sidebar.Visibility = Sidebar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

            args.Handled = true;
        }

        void OnShowPreviewWindowInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;

            if (_previewWindow == null)
            {
                //_previewWindow = new PreviewWindow();
                //_previewWindow.Font = Font;
            }

            //_previewWindow.Activate();
        }

        /**/

        async void OnOpenItemClicked(object sender, RoutedEventArgs args)
        {
            if (await CanDiscardAsync())
            {
                await OpenFontAsync();
            }
        }

        async void OnSaveItemClicked(object sender, RoutedEventArgs args)
        {
            await SaveFontAsync();
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
            ((MainWindow)sender).OnFontChanged();
        }

        void OnToolbarItemChanged(Toolbar sender, object args)
        {
            Canvas.Tool = (ICanvasDelegate)sender.SelectedItem;

            Canvas.Focus(FocusState.Programmatic);
        }

        /**/

        const string BaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz ";

        static Data.Font CreateNewFont()
        {
            var glyphs = new List<Data.Glyph>();

            foreach (var ch in BaseLetters)
            {
                var str = ch.ToString();
                var name = ch == ' ' ? "space" : str;
                var unicode = Conversion.ToUnicode(str);

                glyphs.Add(new Data.Glyph(name, new List<string>() { unicode }, layers: new List<Data.Layer>()
                {
                    new Data.Layer("Regular")
                }));
            }

            return new Data.Font(glyphs: glyphs);
        }

        void UpdateTitle()
        {
            if (Font.IsModified)
            {
                Title = $"*{Font.FamilyName}";
            }
            else
            {
                Title = Font.FamilyName;
            }
        }
    }
}
