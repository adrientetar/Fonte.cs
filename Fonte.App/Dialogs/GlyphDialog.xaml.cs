/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Dialogs
{
    using System.Collections.Generic;
    using Windows.UI.Xaml.Controls;

    public partial class GlyphDialog : ContentDialog
    {
        public Data.Glyph Glyph { get; private set; }

        IList<Data.Glyph> Glyphs { get; }

        public GlyphDialog(Data.Font font)
        {
            Glyphs = font.Glyphs;

            InitializeComponent();
        }

        public GlyphDialog(IList<Data.Glyph> glyphs)
        {
            Glyphs = glyphs;

            InitializeComponent();
        }

        void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Glyph = (Data.Glyph)List.SelectedItem;
        }
    }
}
