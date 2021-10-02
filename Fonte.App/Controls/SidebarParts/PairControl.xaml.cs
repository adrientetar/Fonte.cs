// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace Fonte.App.Controls.SidebarParts
{
    [TemplatePart(Name = "Left", Type = typeof(Control))]
    [TemplatePart(Name = "Right", Type = typeof(Control))]
    public partial class PairControl : UserControl
    {
        public static DependencyProperty LeftProperty = DependencyProperty.Register(
            "Left", typeof(Control), typeof(PairControl), new PropertyMetadata(null, OnLeftChanged));

        public Control Left
        {
            get => (Control)GetValue(LeftProperty);
            set { SetValue(LeftProperty, value); }
        }

        public static DependencyProperty RightProperty = DependencyProperty.Register(
            "Right", typeof(Control), typeof(PairControl), new PropertyMetadata(null, OnRightChanged));

        public Control Right
        {
            get => (Control)GetValue(RightProperty);
            set { SetValue(RightProperty, value); }
        }

        public PairControl()
        {
            InitializeComponent();

            RegisterPropertyChangedCallback(BackgroundProperty, OnPropertyChanged);
            RegisterPropertyChangedCallback(BorderBrushProperty, OnPropertyChanged);
            RegisterPropertyChangedCallback(CornerRadiusProperty, OnPropertyChanged);
        }

        static void OnLeftChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((PairControl)sender).UpdateLeft();
        }

        static void OnRightChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((PairControl)sender).UpdateRight();
        }

        void OnPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            UpdateLeft();
            UpdateRight();
        }

        void UpdateLeft()
        {
            if (Left != null)
            {
                Left.Background = Background;
                Left.BorderBrush = BorderBrush;
                Left.BorderThickness = new Thickness(1, 1, 0, 1);
                Left.CornerRadius = new CornerRadius(CornerRadius.TopLeft, 0, 0, CornerRadius.BottomLeft);
                Left.HorizontalAlignment = HorizontalAlignment.Stretch;
                Left.VerticalAlignment = VerticalAlignment.Stretch;
            }
        }

        void UpdateRight()
        {
            if (Right != null)
            {
                Right.Background = Background;
                Right.BorderBrush = BorderBrush;
                Right.BorderThickness = new Thickness(0, 1, 1, 1);
                Right.CornerRadius = new CornerRadius(0, CornerRadius.TopRight, CornerRadius.BottomRight, 0);
                Right.HorizontalAlignment = HorizontalAlignment.Stretch;
                Right.VerticalAlignment = VerticalAlignment.Stretch;
            }
        }
    }
}
