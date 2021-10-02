// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.ApplicationModel;


namespace Fonte.App.Controls
{

    public partial class PreviewCanvas : UserControl
    {
        public IList<Data.Layer> Layers
        {
            get { return (IList<Data.Layer>)GetValue(LayersProperty); }
            set { SetValue(LayersProperty, value); }
        }

        public static readonly DependencyProperty LayersProperty =
            DependencyProperty.Register("Layers", typeof(IList<Data.Layer>), typeof(PreviewCanvas), new PropertyMetadata(null, OnLayersChanged));

        public PreviewCanvas()
        {
            InitializeComponent();
        }

        public void Invalidate()
        {
            Canvas.Invalidate();
        }

        void OnControlLoaded(object sender, RoutedEventArgs args)
        {
            if (!DesignMode.DesignMode2Enabled)
            {
                ((App)Application.Current).DataChanged += OnDataChanged;
            }
        }

        void OnControlUnloaded(object sender, RoutedEventArgs args)
        {
            if (!DesignMode.DesignMode2Enabled)
            {
                ((App)Application.Current).DataChanged -= OnDataChanged;
            }
        }

        void OnDataChanged(object sender, EventArgs args)
        {
            Canvas.Invalidate();
        }

        void OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var layers = Layers;

            if (layers != null && layers.Count > 0)
            {
                var ds = args.DrawingSession;
                var size = sender.ActualSize;

                var height = size.Y;
                var marginTop = .175f * height;
                var marginBottom = .175f * height;
                height -= marginTop + marginBottom;
                var (ascender, upm) = GetLayerMetrics(layers.First());
                var scale = height / upm;

                ds.Transform = Matrix3x2.CreateScale(scale, -scale) * Matrix3x2.CreateTranslation(0, marginTop + (ascender / upm) * height);

                foreach (var layer in layers)
                {
                    ds.FillGeometry(layer.ClosedCanvasPath, Colors.Black);
                    ds.DrawGeometry(layer.OpenCanvasPath, Colors.Black);

                    ds.Transform *= Matrix3x2.CreateTranslation(layer.Width * scale, 0);
                }
            }
        }

        static void OnLayersChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((PreviewCanvas)sender).Invalidate();
        }

        static (float, float) GetLayerMetrics(Data.Layer layer) => layer.Master is Data.Master master ?
                                                                   (master.Ascender, master.Parent.UnitsPerEm) :
                                                                   (750, 1000);
    }
}
