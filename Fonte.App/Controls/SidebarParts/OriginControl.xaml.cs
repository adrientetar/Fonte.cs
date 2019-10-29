// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Controls.SidebarParts
{
    using System;
    using System.Numerics;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public partial class OriginControl : UserControl
    {
        public event EventHandler SelectedIndexChanged;

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

        void OnButtonClicked(object sender, RoutedEventArgs args)
        {
            SelectedIndex = int.Parse((string)((OriginButton)sender).Tag);
        }

        static void OnSelectedIndexChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var value = (int)args.NewValue;

            if (value < 0 || value > 8)
                throw new ArgumentOutOfRangeException(nameof(value), $"Value must be between 0 and 8 inclusive ('{value}').");

            ((OriginControl)sender).SelectedIndexChanged?.Invoke(sender, EventArgs.Empty);
        }
    }
}
