﻿// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;


namespace Fonte.App.Controls.ToolbarParts
{
    public partial class IconButton : Button
    {
        public static DependencyProperty ActiveBrushProperty = DependencyProperty.Register(
            "ActiveBrush", typeof(Brush), typeof(IconButton), new PropertyMetadata(null, OnUIChanged));

        public Brush ActiveBrush
        {
            get => (Brush)GetValue(ActiveBrushProperty);
            set { SetValue(ActiveBrushProperty, value); }
        }

        public static DependencyProperty BackgroundBrushProperty = DependencyProperty.Register(
            "BackgroundBrush", typeof(Brush), typeof(IconButton), new PropertyMetadata(null, OnUIChanged));

        public Brush BackgroundBrush
        {
            get => (Brush)GetValue(BackgroundBrushProperty);
            set { SetValue(BackgroundBrushProperty, value); }
        }

        public static DependencyProperty IconProperty = DependencyProperty.Register(
            "Icon", typeof(IconSource), typeof(IconButton), null);

        public IconSource Icon
        {
            get => (IconSource)GetValue(IconProperty);
            set { SetValue(IconProperty, value); }
        }

        public static DependencyProperty IsCheckedProperty = DependencyProperty.Register(
            "IsChecked", typeof(bool), typeof(IconButton), new PropertyMetadata(null, OnUIChanged));

        public bool IsChecked
        {
            get => (bool)GetValue(IsCheckedProperty);
            set { SetValue(IsCheckedProperty, value); }
        }

        public static DependencyProperty LabelProperty = DependencyProperty.Register(
           "Label", typeof(string), typeof(IconButton), null);

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set { SetValue(LabelProperty, value); }
        }

        public IconButton()
        {
            InitializeComponent();
        }

        void OnUIChanged()
        {
            Background = IsChecked ? ActiveBrush : BackgroundBrush;
        }

        static void OnUIChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((IconButton)sender).OnUIChanged();
        }
    }
}
