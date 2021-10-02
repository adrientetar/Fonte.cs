// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;


namespace Fonte.App.Controls.SidebarParts
{
    public partial class OriginButton : Button
    {
        public static DependencyProperty AccentProperty = DependencyProperty.Register(
            "Accent", typeof(Brush), typeof(OriginButton), new PropertyMetadata(null, OnUIChanged));

        public Brush Accent
        {
            get => (Brush)GetValue(AccentProperty);
            set { SetValue(AccentProperty, value); }
        }

        public static DependencyProperty IsActiveProperty = DependencyProperty.Register(
            "IsActive", typeof(bool), typeof(OriginButton), new PropertyMetadata(false, OnUIChanged));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set { SetValue(IsActiveProperty, value); }
        }

        public static DependencyProperty FillProperty = DependencyProperty.Register(
            "Fill", typeof(Brush), typeof(OriginButton), null);

        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set { SetValue(FillProperty, value); }
        }

        public static DependencyProperty StrokeProperty = DependencyProperty.Register(
            "Stroke", typeof(Brush), typeof(OriginButton), null);

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set { SetValue(StrokeProperty, value); }
        }

        public OriginButton()
        {
            InitializeComponent();
        }

        void OnUIChanged()
        {
            Fill = IsActive ? Accent : Background;
            Stroke = IsActive ? Accent : Foreground;
        }

        static void OnUIChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((OriginButton)sender).OnUIChanged();
        }

        protected override void OnApplyTemplate()
        {
            OnUIChanged();
        }
    }
}
