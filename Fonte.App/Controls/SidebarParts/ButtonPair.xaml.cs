/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Controls.SidebarParts
{
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public partial class ButtonPair : UserControl
    {
        public static DependencyProperty LeftContentProperty = DependencyProperty.Register(
            "LeftContent", typeof(object), typeof(ButtonPair), null);

        public object LeftContent
        {
            get => GetValue(LeftContentProperty);
            set { SetValue(LeftContentProperty, value); }
        }

        public static DependencyProperty RightContentProperty = DependencyProperty.Register(
            "RightContent", typeof(object), typeof(ButtonPair), null);

        public object RightContent
        {
            get => GetValue(RightContentProperty);
            set { SetValue(RightContentProperty, value); }
        }

        public CornerRadius LeftCornerRadius => new CornerRadius(CornerRadius.TopLeft, 0, 0, CornerRadius.BottomLeft);

        public CornerRadius RightCornerRadius => new CornerRadius(0, CornerRadius.TopRight, CornerRadius.BottomRight, 0);

        public ButtonPair()
        {
            InitializeComponent();
        }
    }
}
