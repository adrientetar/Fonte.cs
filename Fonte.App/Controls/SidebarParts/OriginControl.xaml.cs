/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Controls.SidebarParts
{
    using System;
    using System.Numerics;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public partial class OriginControl : UserControl
    {
        public static DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
            "SelectedIndex", typeof(int), typeof(OriginControl),
            new PropertyMetadata(4, new PropertyChangedCallback(OnSelectedIndexChanged)));

        public int SelectedIndex
        {
            get => (int)GetValue(SelectedIndexProperty);
            set { SetValue(SelectedIndexProperty, value); }
        }

        public OriginControl()
        {
            InitializeComponent();
        }

        public Vector2 GetOrigin(Data.Layer layer)
        {
            var bounds = layer.SelectionBounds;
            if (bounds.IsEmpty)
            {
                bounds = layer.Bounds;
            }
            if (!bounds.IsEmpty)
            {
                return new Vector2(
                    bounds.Left + bounds.Width * .5f * (SelectedIndex % 3),
                    bounds.Bottom + bounds.Height * .5f * (2 - SelectedIndex / 3));
            }
            return Vector2.Zero;
        }

        void OnButtonClicked(object sender, RoutedEventArgs e)
        {
            SelectedIndex = int.Parse((string)((OriginButton)sender).Tag);
        }

        static void OnSelectedIndexChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var value = (int)e.NewValue;

            if (value < 0 || value > 8)
                throw new ArgumentOutOfRangeException($"{value}");
        }
    }
}
