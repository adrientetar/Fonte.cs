/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Controls.SidebarParts
{
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public partial class ButtonPair : UserControl
    {
        public static DependencyProperty LeftButtonProperty = DependencyProperty.Register(
            "LeftButton", typeof(Button), typeof(ButtonPair), new PropertyMetadata(null, OnButtonChanged));

        public Button LeftButton
        {
            get => (Button)GetValue(LeftButtonProperty);
            set { SetValue(LeftButtonProperty, value); }
        }

        public static DependencyProperty RightButtonProperty = DependencyProperty.Register(
            "RightButton", typeof(Button), typeof(ButtonPair), new PropertyMetadata(null, OnButtonChanged));

        public Button RightButton
        {
            get => (Button)GetValue(RightButtonProperty);
            set { SetValue(RightButtonProperty, value); }
        }

        public ButtonPair()
        {
            InitializeComponent();

            //RegisterPropertyChangedCallback(BackgroundProperty, OnBackgroundChanged);
            //RegisterPropertyChangedCallback(BorderBrushProperty, OnBorderBrushChanged);
            RegisterPropertyChangedCallback(CornerRadiusProperty, OnCornerRadiusChanged);
        }

        void UpdateUI()
        {
            if (LeftButton != null)
            {
                LeftButton.CornerRadius = new CornerRadius(CornerRadius.TopLeft, 0, 0, CornerRadius.BottomLeft);
                LeftButton.BorderThickness = new Thickness(1, 1, 0, 1);
            }
            if (RightButton != null)
            {
                RightButton.CornerRadius = new CornerRadius(0, CornerRadius.TopRight, CornerRadius.BottomRight, 0);
            }
        }

        static void OnButtonChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((ButtonPair)sender).UpdateUI();
        }

        void OnCornerRadiusChanged(DependencyObject sender, DependencyProperty dp)
        {
            ((ButtonPair)sender).UpdateUI();
        }
    }
}
