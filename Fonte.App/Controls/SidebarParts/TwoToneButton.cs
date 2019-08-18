/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Controls.SidebarParts
{
    using System.Collections.Generic;
    using System.Linq;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;
    using Path = Windows.UI.Xaml.Shapes.Path;

    public partial class TwoToneButton : Button
    {
        public static DependencyProperty AlphaScaleProperty = DependencyProperty.Register(
            "AlphaScale", typeof(float), typeof(TwoToneButton), new PropertyMetadata(.45f, OnAlphaScaleChanged));

        public float AlphaScale
        {
            get => (float)GetValue(AlphaScaleProperty);
            set { SetValue(AlphaScaleProperty, value); }
        }

        public static DependencyProperty DisabledFillProperty = DependencyProperty.Register(
            "DisabledFill", typeof(Brush), typeof(TwoToneButton), null);

        public Brush DisabledFill
        {
            get => (Brush)GetValue(DisabledFillProperty);
            set { SetValue(DisabledFillProperty, value); }
        }

        public static DependencyProperty DisabledStrokeProperty = DependencyProperty.Register(
            "DisabledStroke", typeof(Brush), typeof(TwoToneButton), null);

        public Brush DisabledStroke
        {
            get => (Brush)GetValue(DisabledStrokeProperty);
            set { SetValue(DisabledStrokeProperty, value); }
        }

        public static DependencyProperty FillProperty = DependencyProperty.Register(
            "Fill", typeof(Brush), typeof(TwoToneButton), new PropertyMetadata(null, OnFillChanged));

        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set { SetValue(FillProperty, value); }
        }

        public static DependencyProperty StrokeProperty = DependencyProperty.Register(
            "Stroke", typeof(Brush), typeof(TwoToneButton), new PropertyMetadata(null, OnStrokeChanged));

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set { SetValue(StrokeProperty, value); }
        }

        public TwoToneButton()
        {
            //DefaultStyleKey = typeof(TwoToneButton);

            RegisterPropertyChangedCallback(IsEnabledProperty, OnIsEnabledChanged);
        }

        void OnIsEnabledChanged(DependencyObject sender, DependencyProperty dp)
        {
            OnUIChanged();
        }

        void OnFillChanged()
        {
            DisabledFill = Fill is SolidColorBrush fill ?
                           new SolidColorBrush(ToGreyscale(fill.Color)) :
                           Fill;
        }

        void OnStrokeChanged()
        {
            DisabledStroke = Stroke is SolidColorBrush stroke ?
                             new SolidColorBrush(ToGreyscale(stroke.Color)) :
                             Stroke;
        }

        void OnUIChanged()
        {
            var fill = IsEnabled ? Fill : DisabledFill;
            var stroke = IsEnabled ? Stroke : DisabledStroke;

            foreach (var path in GetChildrenPaths())
            {
                if (!IsTransparent(path.Fill)) path.Fill = fill;
                if (!IsTransparent(path.Stroke)) path.Stroke = stroke;
            }
        }

        static void OnAlphaScaleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((TwoToneButton)sender).OnFillChanged();
            ((TwoToneButton)sender).OnStrokeChanged();
            ((TwoToneButton)sender).OnUIChanged();
        }

        static void OnFillChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((TwoToneButton)sender).OnFillChanged();
            ((TwoToneButton)sender).OnUIChanged();
        }

        static void OnStrokeChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((TwoToneButton)sender).OnStrokeChanged();
            ((TwoToneButton)sender).OnUIChanged();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            OnFillChanged();
            OnStrokeChanged();
            OnUIChanged();
        }

        IEnumerable<Path> GetChildrenPaths()
        {
            var content = Content;
            if (content is Path path)
            {
                yield return path;
            }
            else if (content is Grid grid)
            {
                foreach (var p in grid.Children.OfType<Path>())
                {
                    yield return p;
                }
            }
        }

        bool IsTransparent(Brush brush)
        {
            return brush is SolidColorBrush b ? b.Color == Colors.Transparent : false;
        }

        Color ToGreyscale(Color color)
        {
            var lum = (byte)(0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B);
            return Color.FromArgb((byte)(color.A * AlphaScale), lum, lum, lum);
        }
    }
}
