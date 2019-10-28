// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App
{
    using Fonte.Data.Utilities;

    using System.Linq;
    using Windows.ApplicationModel;
    using Windows.UI.WindowManagement;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    public sealed partial class PreviewPage : Page
    {
        public static DependencyProperty FontProperty = DependencyProperty.Register(
            "Font", typeof(Data.Font), typeof(CanvasPage), new PropertyMetadata(null, OnFontChanged));

        public Data.Font Font
        {
            get => (Data.Font)GetValue(FontProperty);
            set { SetValue(FontProperty, value); }
        }

        public PreviewPage()
        {
            InitializeComponent();
        }

        void OnFontChanged()
        {
            //AppWindow.GetForUIContext(UIContext).Title = $"{Font.FamilyName} – {Package.Current.DisplayName}";

            UpdateLayers(TextBox.Text);
        }

        void OnTextChanged(object sender, TextChangedEventArgs args)
        {
            var text = ((TextBox)sender).Text;

            UpdateLayers(text);
        }

        static void OnFontChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((PreviewPage)sender).OnFontChanged();
        }

        void UpdateLayers(string text)
        {
            var glyphs = Font.Glyphs;

            Canvas.Layers = text.Select(ch => Conversion.ToUnicode(ch.ToString()))
                                .Select(uni => glyphs.Where(glyph => glyph.Unicode == uni).FirstOrDefault())
                                .Where(glyph => glyph != null)
                                .Select(glyph => glyph.Layers[0])
                                .ToArray();
        }
    }
}
