/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Controls.SidebarParts
{
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;

    public partial class OriginButton : Button
    {
        public static DependencyProperty IsActiveProperty = DependencyProperty.Register(
            "IsActive", typeof(bool), typeof(OriginButton), new PropertyMetadata(false, OnActiveChanged));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set { SetValue(IsActiveProperty, value); }
        }

        public static DependencyProperty ActiveBackgroundProperty = DependencyProperty.Register(
            "ActiveBackground", typeof(Brush), typeof(OriginButton), new PropertyMetadata(null, OnActiveChanged));

        public Brush ActiveBackground
        {
            get => (Brush)GetValue(ActiveBackgroundProperty);
            set { SetValue(ActiveBackgroundProperty, value); }
        }

        public static DependencyProperty DefaultBackgroundProperty = DependencyProperty.Register(
            "DefaultBackground", typeof(Brush), typeof(OriginButton), new PropertyMetadata(null, OnActiveChanged));

        public Brush DefaultBackground
        {
            get => (Brush)GetValue(DefaultBackgroundProperty);
            set { SetValue(DefaultBackgroundProperty, value); }
        }

        public OriginButton()
        {
            InitializeComponent();
        }

        void OnActiveChanged()
        {
            Background = IsActive ? ActiveBackground : DefaultBackground;
        }

        static void OnActiveChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((OriginButton)sender).OnActiveChanged();
        }

        protected override void OnApplyTemplate()
        {
            OnActiveChanged();
        }
    }
}
