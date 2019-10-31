// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public partial class GlyphDialog : ContentDialog
    {
        public ContentDialogResult Result { get; set; }

        public new async Task<ContentDialogResult> ShowAsync()
        {
            var baseResult = await base.ShowAsync();
            if (baseResult == ContentDialogResult.None)
            {
                return Result;
            }
            return baseResult;
        }

        /**/

        public static readonly DependencyProperty CurrentGlyphsProperty =
            DependencyProperty.Register("CurrentGlyphs", typeof(IList<Data.Glyph>), typeof(GlyphDialog), null);

        public IList<Data.Glyph> CurrentGlyphs
        {
            get { return (IList<Data.Glyph>)GetValue(CurrentGlyphsProperty); }
            set { SetValue(CurrentGlyphsProperty, value); }
        }

        public Data.Glyph Glyph { get; private set; }

        IList<Data.Glyph> Glyphs { get; }

        public GlyphDialog(IList<Data.Glyph> glyphs)
        {
            Glyphs = glyphs;

            CurrentGlyphs = glyphs;

            InitializeComponent();
        }

        void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Glyph = List.SelectedIndex switch
            {
                var ix when ix < 0 => CurrentGlyphs.FirstOrDefault(),
                var ix when ix < CurrentGlyphs.Count => CurrentGlyphs[ix],
                _ => null
            };
        }

        void OnTextBoxKeyDown(object sender, KeyRoutedEventArgs args)
        {
            if (args.Key == VirtualKey.Enter)
            {
                Result = ContentDialogResult.Primary;
                OnPrimaryButtonClick(this, default);
                Hide();
            }
            else if (args.Key == VirtualKey.Escape)
            {
                Hide();
            }
            else if (args.Key == VirtualKey.Up)
            {
                var idx = List.SelectedIndex;

                if (idx > 0)
                {
                    List.SelectedIndex = --idx;
                }
            }
            else if (args.Key == VirtualKey.Down)
            {
                var idx = List.SelectedIndex;

                if (idx < List.Items.Count - 1)
                {
                    List.SelectedIndex = ++idx;
                }
            }
            else
            {
                return;
            }

            args.Handled = true;
        }

        void OnTextBoxTextChanged(object sender, TextChangedEventArgs args)
        {
            var text = ((TextBox)sender).Text;

            CurrentGlyphs = Glyphs.Where(glyph => glyph.Name.StartsWith(text))
                                  .ToArray();

            if (List.Items.Count > 0)
            {
                List.SelectedIndex = 0;
            }
        }
    }
}
