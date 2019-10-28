// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.System;
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

        public ObservableCollection<Data.Glyph> CurrentGlyphs { get; }

        public Data.Glyph Glyph { get; private set; }

        IList<Data.Glyph> Glyphs { get; }

        public GlyphDialog(IList<Data.Glyph> glyphs)
        {
            Glyphs = glyphs;

            CurrentGlyphs = new ObservableCollection<Data.Glyph>(glyphs);

            InitializeComponent();
        }

        void OnAutoSuggestBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var text = sender.Text;

                CurrentGlyphs.Clear();
                foreach (var g in Glyphs.Where(glyph => glyph.Name.StartsWith(text))) { CurrentGlyphs.Add(g); }

                if (List.Items.Count > 0)
                {
                    List.SelectedIndex = 0;
                }
            }
        }

        void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Glyph = (Data.Glyph)List.SelectedItem;
        }

        void OnAutoSuggestBoxKeyDown(object sender, KeyRoutedEventArgs args)
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
    }
}
