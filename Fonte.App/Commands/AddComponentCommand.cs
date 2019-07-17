/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Commands
{
    using Fonte.App.Dialogs;

    using System;
    using System.Linq;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public class AddComponentCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            var layer = (Data.Layer)parameter;

            return layer.Parent?.Parent.Glyphs.Count > 1;
        }

        public async void Execute(object parameter)
        {
            var layer = (Data.Layer)parameter;
            var glyph = layer.Parent;
            var font = glyph.Parent;

            var glyphDialog = new GlyphDialog(font.Glyphs
                                                  .Where(g => g.Name != glyph.Name)
                                                  .ToList());
            await glyphDialog.ShowAsync();

            if (glyphDialog.Glyph is Data.Glyph sourceGlyph)
            {
                var component = new Data.Component(sourceGlyph.Name);

                layer.Components.Add(component);
                layer.ClearSelection();
                component.IsSelected = true;
                ((App)Application.Current).InvalidateData();
            }
        }
    }
}
