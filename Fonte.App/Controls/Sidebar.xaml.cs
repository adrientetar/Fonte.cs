// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Controls
{
    using Fonte.App.Commands;
    using Fonte.App.Utilities;
    using Fonte.Data.Utilities;
    using Microsoft.UI.Xaml.Controls;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Numerics;
    using System.Windows.Input;
    using Windows.ApplicationModel;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public partial class Sidebar : UserControl
    {
        private bool _isEditing;
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
            "XPosition", typeof(double), typeof(Sidebar), null);

        public double XPosition
        {
            get => (double)GetValue(XPositionProperty);
            set { SetValue(XPositionProperty, value); }
        }

        public static DependencyProperty YPositionProperty = DependencyProperty.Register(
            "YPosition", typeof(double), typeof(Sidebar), null);

        public double YPosition
        {
            get => (double)GetValue(YPositionProperty);
            set { SetValue(YPositionProperty, value); }
        }

        public static DependencyProperty XSizeProperty = DependencyProperty.Register(
            "XSize", typeof(double), typeof(Sidebar), null);

        public double XSize
        {
            get => (double)GetValue(XSizeProperty);
            set { SetValue(XSizeProperty, value); }
        }

        public static DependencyProperty YSizeProperty = DependencyProperty.Register(
            "YSize", typeof(double), typeof(Sidebar), null);

        public double YSize
        {
            get => (double)GetValue(YSizeProperty);
            set { SetValue(YSizeProperty, value); }
        }

        public static DependencyProperty LLeftMarginProperty = DependencyProperty.Register(
            "LLeftMargin", typeof(double), typeof(Sidebar), null);

        public double LLeftMargin
        {
            get => (double)GetValue(LLeftMarginProperty);
            set { SetValue(LLeftMarginProperty, value); }
        }

        public static DependencyProperty LRightMarginProperty = DependencyProperty.Register(
            "LRightMargin", typeof(double), typeof(Sidebar), null);

        public double LRightMargin
        {
            get => (double)GetValue(LRightMarginProperty);
            set { SetValue(LRightMarginProperty, value); }
        }

        public static DependencyProperty XScaleFactorProperty = DependencyProperty.Register(
            "XScaleFactor", typeof(double), typeof(Sidebar), new PropertyMetadata(2.0));

        public double XScaleFactor
        {
            get => (double)GetValue(XScaleFactorProperty);
            set { SetValue(XScaleFactorProperty, value); }
        }

        public static DependencyProperty YScaleFactorProperty = DependencyProperty.Register(
            "YScaleFactor", typeof(double), typeof(Sidebar), new PropertyMetadata(2.0));

        public double YScaleFactor
        {
            get => (double)GetValue(YScaleFactorProperty);
            set { SetValue(YScaleFactorProperty, value); }
        }

        public static DependencyProperty RotationDegreeProperty = DependencyProperty.Register(
            "RotationDegree", typeof(double), typeof(Sidebar), new PropertyMetadata(40.0));

        public double RotationDegree
        {
            get => (double)GetValue(RotationDegreeProperty);
            set { SetValue(RotationDegreeProperty, value); }
        }

        public static DependencyProperty SkewDegreeProperty = DependencyProperty.Register(
            "SkewDegree", typeof(double), typeof(Sidebar), new PropertyMetadata(6.0));

        public double SkewDegree
        {
            get => (double)GetValue(SkewDegreeProperty);
            set { SetValue(SkewDegreeProperty, value); }
        }

        public ICommand AlignLeftCommand { get; } = new AlignLeftCommand();
        public ICommand CenterHorizontallyCommand { get; } = new CenterHorizontallyCommand();
        public ICommand AlignRightCommand { get; } = new AlignRightCommand();
        public ICommand AlignTopCommand { get; } = new AlignTopCommand();
        public ICommand CenterVerticallyCommand { get; } = new CenterVerticallyCommand();
        public ICommand AlignBottomCommand { get; } = new AlignBottomCommand();

        public ICommand DeleteLayerCommand { get; } = new DeleteLayerCommand();

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

        void OnDataChanged(object sender, EventArgs args) => UpdateUI();

        void UpdateUI()
        {
            var layer = Layer;
            if (_shouldIgnoreNextRefresh || (layer != null && layer.IsEditing))
            {
                _shouldIgnoreNextRefresh = false;
                return;
            }

            try
            {
                _isEditing = true;

                if (layer != null && !layer.SelectionBounds.IsEmpty)
                {
                    var origin = Origin.GetOrigin(layer);

                    XPosition = Math.Round(origin.X, 2);
                    YPosition = Math.Round(origin.Y, 2);
                    XSize = Math.Round(layer.SelectionBounds.Width, 2);
                    YSize = Math.Round(layer.SelectionBounds.Height, 2);
                }
                else
                {
                    XPosition = YPosition = XSize = YSize = double.NaN;
                }
                LLeftMargin = layer.LeftMargin != null ? Math.Round(layer.LeftMargin.Value, 2) : double.NaN;
                LRightMargin = layer.RightMargin != null ? Math.Round(layer.RightMargin.Value, 2) : double.NaN;
            }
            finally
            {
                _isEditing = false;
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
            ((Sidebar)sender).UpdateUI();
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

        void OnXPositionChanged(object sender, NumberBoxValueChangedEventArgs args)
        {
            if (!_isEditing)
            {
                var refDelta = .5f * (float)XSize * Origin.HorizontalIndex;
                var dx = Outline.RoundToGrid((float)args.NewValue - refDelta) + refDelta - Origin.GetOrigin(Layer).X;
                Layer.Transform(Matrix3x2.CreateTranslation(dx, 0), selectionOnly: true);

                ((App)Application.Current).InvalidateData();
            }
        }

        void OnYPositionChanged(object sender, NumberBoxValueChangedEventArgs args)
        {
            if (!_isEditing)
            {
                var refDelta = .5f * (float)YSize * Origin.VerticalIndex;
                var dy = Outline.RoundToGrid((float)args.NewValue - refDelta) + refDelta - Origin.GetOrigin(Layer).Y;
                Layer.Transform(Matrix3x2.CreateTranslation(0, dy), selectionOnly: true);

                ((App)Application.Current).InvalidateData();
            }
        }

        void OnXSizeChanged(object sender, NumberBoxValueChangedEventArgs args)
        {
            var layer = Layer;

            if (!_isEditing)
            {
                if (layer.SelectionBounds.Width > 0)
                {
                    var wr = (float)args.NewValue / layer.SelectionBounds.Width;
                    using (var group = layer.CreateUndoGroup())
                    {
                        layer.Transform(Matrix3x2.CreateScale(wr, 1, Origin.GetOrigin(layer)),
                                        selectionOnly: true);
                        Outline.RoundSelection(layer);
                    }

                    ((App)Application.Current).InvalidateData();
                }
                else
                {
                    UpdateUI();
                }
            }
        }

        void OnYSizeChanged(object sender, NumberBoxValueChangedEventArgs args)
        {
            var layer = Layer;

            if (!_isEditing)
            {
                if (layer.SelectionBounds.Height > 0)
                {
                    var hr = Outline.RoundToGrid((float)args.NewValue) / layer.SelectionBounds.Height;
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
                    UpdateUI();
                }
            }
        }

        void OnLLeftMarginChanged(object sender, NumberBoxValueChangedEventArgs args)
        {
            var value = args.NewValue;

            if (!_isEditing && !double.IsNaN(value))
            {
                Layer.LeftMargin = (float)value;

                ((App)Application.Current).InvalidateData();
            }
        }

        void OnLRightMarginChanged(object sender, NumberBoxValueChangedEventArgs args)
        {
            var value = args.NewValue;

            if (!_isEditing && !double.IsNaN(value))
            {
                Layer.RightMargin = (float)value;

                ((App)Application.Current).InvalidateData();
            }
        }

        void OnStretchButtonClick(object sender, RoutedEventArgs args)
        {
            var layer = Layer;
            var ok = false;
            var lo = .6f;
            var hi = .8f;
            var ix = (int)((Control)sender).Tag;
            var count = 6;
            var stretchFactor = lo + (hi - lo) * ((float)ix / count);

            foreach (var path in layer.Paths)
            {
                foreach (var segment in path.Segments)
                {
                    if (Outline.AnyOffCurveSelected(segment))
                    {
                        Outline.StretchCurve(layer, segment.PointsInclusive, stretchFactor);
                        ok = true;
                    }
                }
            }

            if (ok) ((App)Application.Current).InvalidateData();
        }

        void OnRotationButtonClick(object sender, RoutedEventArgs args)
        {
            var angle = RotationDegree;

            if (angle != 0.0)
            {
                var rad = GetControlSign(sender) * Conversion.FromDegrees((float)angle);
                Layer.Transform(Matrix3x2.CreateRotation(rad, Origin.GetOrigin(Layer)),
                                selectionOnly: Layer.Selection.Count > 0);

                ((App)Application.Current).InvalidateData();
            }
        }

        void OnScaleButtonClick(object sender, RoutedEventArgs args)
        {
            var xScaleFactor = XScaleFactor;
            var yScaleFactor = YScaleFactor;

            if (xScaleFactor != 0 || yScaleFactor != 0)
            {
                var sign = GetControlSign(sender);
                var xScale = 1f / (1 - sign * .01f * (float)xScaleFactor);
                var yScale = false ? //YScaleTextBox.IsEnabled ?
                             1f / (1 - sign * .01f * (float)yScaleFactor) :
                             xScale;

                Layer.Transform(Matrix3x2.CreateScale(xScale, yScale, Origin.GetOrigin(Layer)),
                                selectionOnly: Layer.Selection.Count > 0);

                ((App)Application.Current).InvalidateData();
            }
        }

        void OnSkewButtonClick(object sender, RoutedEventArgs args)
        {
            var angle = SkewDegree;

            if (angle != 0.0)
            {
                var rad = GetControlSign(sender) * Conversion.FromDegrees((float)angle);
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
