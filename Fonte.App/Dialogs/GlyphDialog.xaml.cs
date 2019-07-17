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
            InitializeComponent();

            Glyphs = font.Glyphs;
        }

        public GlyphDialog(IList<Data.Glyph> glyphs)
        {
            InitializeComponent();

            Glyphs = glyphs;
        }

        void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            Glyph = (Data.Glyph)List.SelectedItem;
        }
    }
}
