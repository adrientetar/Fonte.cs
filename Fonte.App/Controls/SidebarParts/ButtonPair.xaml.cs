/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Controls.SidebarParts
{
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public partial class ButtonPair : UserControl
    {
        public event RoutedEventHandler LeftClick;
        public event RoutedEventHandler RightClick;

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

        public static DependencyProperty LeftToolTipProperty = DependencyProperty.Register(
            "LeftToolTip", typeof(string), typeof(ButtonPair), null);

        public string LeftToolTip
        {
            get => (string)GetValue(LeftToolTipProperty);
            set { SetValue(LeftToolTipProperty, value); }
        }

        public static DependencyProperty RightToolTipProperty = DependencyProperty.Register(
            "RightToolTip", typeof(string), typeof(ButtonPair), null);

        public string RightToolTip
        {
            get => (string)GetValue(RightToolTipProperty);
            set { SetValue(RightToolTipProperty, value); }
        }

        public CornerRadius LeftCornerRadius => new CornerRadius(CornerRadius.TopLeft, 0, 0, CornerRadius.BottomLeft);

        public CornerRadius RightCornerRadius => new CornerRadius(0, CornerRadius.TopRight, CornerRadius.BottomRight, 0);

        public ButtonPair()
        {
            InitializeComponent();
        }

        void OnLeftButtonClick(object sender, RoutedEventArgs e)
        {
            LeftClick.Invoke(sender, e);
        }

        void OnRightButtonClick(object sender, RoutedEventArgs e)
        {
            RightClick.Invoke(sender, e);
        }
    }
}
