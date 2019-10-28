// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Controls.SidebarParts
{
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls.Primitives;

    [TemplatePart(Name = "CheckedContent", Type = typeof(object))]
    [TemplatePart(Name = "UncheckedContent", Type = typeof(object))]
    public partial class IconToggleButton : ToggleButton
    {
        public event RoutedEventHandler Toggled;

        public static DependencyProperty ActualContentProperty = DependencyProperty.Register(
            "ActualContent", typeof(object), typeof(IconToggleButton), null);

        public object ActualContent
        {
            get => GetValue(ActualContentProperty);
            set { SetValue(ActualContentProperty, value); }
        }

        public static DependencyProperty CheckedContentProperty = DependencyProperty.Register(
            "CheckedContent", typeof(object), typeof(IconToggleButton), new PropertyMetadata(null, OnContentChanged));

        public object CheckedContent
        {
            get => GetValue(CheckedContentProperty);
            set { SetValue(CheckedContentProperty, value); }
        }

        public static DependencyProperty UncheckedContentProperty = DependencyProperty.Register(
            "UncheckedContent", typeof(object), typeof(IconToggleButton), new PropertyMetadata(null, OnContentChanged));

        public object UncheckedContent
        {
            get => GetValue(UncheckedContentProperty);
            set { SetValue(UncheckedContentProperty, value); }
        }

        public IconToggleButton()
        {
            InitializeComponent();

            RegisterPropertyChangedCallback(IsCheckedProperty, OnPropertyChanged);
        }

        static void OnContentChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((IconToggleButton)sender).UpdateContent();
        }

        void OnPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            UpdateContent();

            Toggled?.Invoke(this, Toggled_EventArgs);
        }

        void UpdateContent()
        {
            ActualContent = IsChecked.Value ? CheckedContent : UncheckedContent;
        }

        static readonly RoutedEventArgs Toggled_EventArgs = new RoutedEventArgs();
    }
}
