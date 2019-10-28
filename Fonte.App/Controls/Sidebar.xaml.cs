// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Controls
{
    using Fonte.App.Commands;
    using Fonte.App.Utilities;
    using Fonte.Data.Utilities;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Numerics;
    using System.Windows.Input;
    using Windows.ApplicationModel;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public partial class Sidebar : UserControl
    {
        private bool _shouldIgnoreNextRefresh;

        public static DependencyProperty LayerProperty = DependencyProperty.Register(
            "Layer", typeof(Data.Layer), typeof(Sidebar), new PropertyMetadata(null, OnLayerChanged));

        public Data.Layer Layer
        {
            get => (Data.Layer)GetValue(LayerProperty);
            set { SetValue(LayerProperty, value); }
        }

        public static DependencyProperty LayersProperty = DependencyProperty.Register(
            "Layers", typeof(ObservableCollection<Data.Layer>), typeof(Sidebar), null);

        public ObservableCollection<Data.Layer> Layers
        {
            get => (ObservableCollection<Data.Layer>)GetValue(LayersProperty);
        }

        public static DependencyProperty XPositionProperty = DependencyProperty.Register(
            "XPosition", typeof(string), typeof(Sidebar), null);

        public string XPosition
        {
            get => (string)GetValue(XPositionProperty);
            set { SetValue(XPositionProperty, value); }
        }

        public static DependencyProperty YPositionProperty = DependencyProperty.Register(
            "YPosition", typeof(string), typeof(Sidebar), null);

        public string YPosition
        {
            get => (string)GetValue(YPositionProperty);
            set { SetValue(YPositionProperty, value); }
        }

        public static DependencyProperty XSizeProperty = DependencyProperty.Register(
            "XSize", typeof(string), typeof(Sidebar), null);

        public string XSize
        {
            get => (string)GetValue(XSizeProperty);
            set { SetValue(XSizeProperty, value); }
        }

        public static DependencyProperty YSizeProperty = DependencyProperty.Register(
            "YSize", typeof(string), typeof(Sidebar), null);

        public string YSize
        {
            get => (string)GetValue(YSizeProperty);
            set { SetValue(YSizeProperty, value); }
        }

        public ICommand AlignLeftCommand { get; } = new AlignLeftCommand();
        public ICommand CenterHorizontallyCommand { get; } = new CenterHorizontallyCommand();
        public ICommand AlignRightCommand { get; } = new AlignRightCommand();
        public ICommand AlignTopCommand { get; } = new AlignTopCommand();
        public ICommand CenterVerticallyCommand { get; } = new CenterVerticallyCommand();
        public ICommand AlignBottomCommand { get; } = new AlignBottomCommand();

        public ICommand UnitePathsCommand { get; } = new UnitePathsCommand();
        public ICommand SubtractPathsCommand { get; } = new SubtractPathsCommand();
        public ICommand IntersectPathsCommand { get; } = new IntersectPathsCommand();
        public ICommand XorPathsCommand { get; } = new XorPathsCommand();

        public Sidebar()
        {
            InitializeComponent();

            SetValue(LayersProperty, new ObservableCollection<Data.Layer>());
        }

        void OnControlLoaded(object sender, RoutedEventArgs args)
        {
            if (DesignMode.DesignMode2Enabled)
                return;

            ((App)Application.Current).DataChanged += OnDataChanged;

            Origin.SelectedIndexChanged += OnDataChanged;
        }

        void OnControlUnloaded(object sender, RoutedEventArgs args)
        {
            if (DesignMode.DesignMode2Enabled)
                return;

            Origin.SelectedIndexChanged -= OnDataChanged;

            ((App)Application.Current).DataChanged -= OnDataChanged;
        }

        void OnDataChanged(object sender, EventArgs args)
        {
            var layer = Layer;
            if (_shouldIgnoreNextRefresh || (layer != null && layer.IsEditing))
            {
                _shouldIgnoreNextRefresh = false;
                return;
            }

            if (layer != null && !layer.SelectionBounds.IsEmpty)
            {
                var culture = CultureInfo.CurrentUICulture;
                var origin = Origin.GetOrigin(layer);

                XPosition = Math.Round(origin.X, 2).ToString(culture);
                YPosition = Math.Round(origin.Y, 2).ToString(culture);
                XSize = Math.Round(layer.SelectionBounds.Width, 2).ToString(culture);
                YSize = Math.Round(layer.SelectionBounds.Height, 2).ToString(culture);
            }
            else
            {
                XPosition = YPosition = XSize = YSize = string.Empty;
            }

            if (layer?.Parent is Data.Glyph glyph)
            {
                // TODO: we could rewind this more selectively...
                var layers = Layers;
                layers.Clear();
                foreach (var l in GetSortedLayers(glyph)) { layers.Add(l); }

                LayersView.SelectedItem = layer;
            }
            else
            {
                Layers.Clear();
            }

            ((AlignLeftCommand)AlignLeftCommand).NotifyCanExecuteChanged();
            ((CenterHorizontallyCommand)CenterHorizontallyCommand).NotifyCanExecuteChanged();
            ((AlignRightCommand)AlignRightCommand).NotifyCanExecuteChanged();
            ((AlignTopCommand)AlignTopCommand).NotifyCanExecuteChanged();
            ((CenterVerticallyCommand)CenterVerticallyCommand).NotifyCanExecuteChanged();
            ((AlignBottomCommand)AlignBottomCommand).NotifyCanExecuteChanged();

            ((UnitePathsCommand)UnitePathsCommand).NotifyCanExecuteChanged();
            ((SubtractPathsCommand)SubtractPathsCommand).NotifyCanExecuteChanged();
            ((IntersectPathsCommand)IntersectPathsCommand).NotifyCanExecuteChanged();
            ((XorPathsCommand)XorPathsCommand).NotifyCanExecuteChanged();

            HorzMirrorButton.IsEnabled = VertMirrorButton.IsEnabled =
                ScaleButtons.IsEnabled = RotationButtons.IsEnabled = SkewButtons.IsEnabled = layer?.Paths.Count > 0;
        }

        static void OnLayerChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((Sidebar)sender).OnDataChanged(sender, EventArgs.Empty);
        }

        /**/

        void OnAddLayerButtonClick(object sender, RoutedEventArgs args)
        {
            var layer = (Data.Layer)LayersView.SelectedItem;
            var layers = layer.Parent.Layers;

            layers.Add(layer.Clone());
            ((App)Application.Current).InvalidateData();
        }

        void OnHorzMirrorButtonClick(object sender, RoutedEventArgs args)
        {
            var matrix = Matrix3x2.CreateScale(-1, 1, Origin.GetOrigin(Layer));
            Layer.Transform(matrix, Layer.Selection.Count > 0);

            ((App)Application.Current).InvalidateData();
        }

        void OnVertMirrorButtonClick(object sender, RoutedEventArgs args)
        {
            var matrix = Matrix3x2.CreateScale(1, -1, Origin.GetOrigin(Layer));
            Layer.Transform(matrix, Layer.Selection.Count > 0);

            ((App)Application.Current).InvalidateData();
        }

        void OnXPositionChanged(object sender, LosingFocusEventArgs args)
        {
            var textBox = (TextBox)sender;
            if (float.TryParse(textBox.Text, out float result))
            {
                var dx = Outline.RoundToGrid(result) - Origin.GetOrigin(Layer).X;
                Layer.Transform(Matrix3x2.CreateTranslation(dx, 0), selectionOnly: true);

                ((App)Application.Current).InvalidateData();
            }
            else
            {
                OnDataChanged(this, EventArgs.Empty);
            }
        }

        void OnYPositionChanged(object sender, LosingFocusEventArgs args)
        {
            var textBox = (TextBox)sender;
            if (float.TryParse(textBox.Text, out float result))
            {
                var dy = Outline.RoundToGrid(result) - Origin.GetOrigin(Layer).Y;
                Layer.Transform(Matrix3x2.CreateTranslation(0, dy), selectionOnly: true);

                ((App)Application.Current).InvalidateData();
            }
            else
            {
                OnDataChanged(this, EventArgs.Empty);
            }
        }

        void OnXSizeChanged(object sender, LosingFocusEventArgs args)
        {
            var layer = Layer;
            var textBox = (TextBox)sender;
            if (layer.SelectionBounds.Width > 0 && float.TryParse(textBox.Text, out float result))
            {
                var wr = result / layer.SelectionBounds.Width;
                using (var group = layer.CreateUndoGroup())
                {
                    Layer.Transform(Matrix3x2.CreateScale(wr, 1, Origin.GetOrigin(Layer)),
                                    selectionOnly: true);
                    Outline.RoundSelection(Layer);
                }

                ((App)Application.Current).InvalidateData();
            }
            else
            {
                OnDataChanged(this, EventArgs.Empty);
            }
        }

        void OnYSizeChanged(object sender, LosingFocusEventArgs args)
        {
            var layer = Layer;
            var textBox = (TextBox)sender;
            if (layer.SelectionBounds.Height > 0 && float.TryParse(textBox.Text, out float result))
            {
                var hr = Outline.RoundToGrid(result) / layer.SelectionBounds.Height;
                using (var group = layer.CreateUndoGroup())
                {
                    Layer.Transform(Matrix3x2.CreateScale(1, hr, Origin.GetOrigin(Layer)),
                                    selectionOnly: true);
                    Outline.RoundSelection(Layer);
                }

                ((App)Application.Current).InvalidateData();
            }
            else
            {
                OnDataChanged(this, EventArgs.Empty);
            }
        }

        void OnRotationButtonClick(object sender, RoutedEventArgs args)
        {
            var result = float.Parse(RotationTextBox.Text);
            // TODO: if incorrect restore oldValue
            // -- actually the value should be validated/restored on textbox input, not here

            if (result != 0f)
            {
                var rad = GetControlSign(sender) * Conversion.FromDegrees(result);
                Layer.Transform(Matrix3x2.CreateRotation(rad, Origin.GetOrigin(Layer)),
                                selectionOnly: Layer.Selection.Count > 0);

                ((App)Application.Current).InvalidateData();
            }
        }

        void OnScaleButtonClick(object sender, RoutedEventArgs args)
        {
            var sign = GetControlSign(sender);
            var xScale = 1f / (1 - sign * .01f * float.Parse(XScaleTextBox.Text));
            var yScale = YScaleTextBox.IsEnabled ?
                         1f / (1 - sign * .01f * float.Parse(YScaleTextBox.Text)) :
                         xScale;

            if (xScale != 1f || yScale != 1f)
            {
                Layer.Transform(Matrix3x2.CreateScale(xScale, yScale, Origin.GetOrigin(Layer)),
                                selectionOnly: Layer.Selection.Count > 0);

                ((App)Application.Current).InvalidateData();
            }
        }

        void OnSkewButtonClick(object sender, RoutedEventArgs args)
        {
            var result = float.Parse(SkewTextBox.Text);

            if (result != 0)
            {
                var rad = GetControlSign(sender) * Conversion.FromDegrees(result);
                Layer.Transform(Matrix3x2.CreateSkew(rad, 0, Origin.GetOrigin(Layer)),
                                selectionOnly: Layer.Selection.Count > 0);

                ((App)Application.Current).InvalidateData();
            }
        }

        void OnLayersItemClick(object sender, ItemClickEventArgs e)
        {
            var layer = (Data.Layer)e.ClickedItem;

            // When switching to that layer we actually add it to the model
            if (layer.Parent == null)
            {
                Layer.Parent.Layers.Add(layer);
            }
            Layer = layer;
        }

        void OnLayerVisibilityChanged(object sender, RoutedEventArgs args)
        {
            _shouldIgnoreNextRefresh = true;

            ((App)Application.Current).InvalidateData();
        }

        static float GetControlSign(object control)
        {
            return ((string)((FrameworkElement)control).Tag) == "!" ? -1f : 1f;
        }

        static IEnumerable<Data.Layer> GetSortedLayers(Data.Glyph glyph)
        {
            var font = glyph.Parent;
            var layers = glyph.Layers;

            foreach (var master in font.Masters)
            {
                if (!glyph.TryGetLayer(master.Name, out Data.Layer layer))
                {
                    layers.Add(layer);
                }
            }

            return glyph.Layers.OrderBy(e => e);
        }
    }
}
