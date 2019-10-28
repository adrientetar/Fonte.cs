// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Commands
{
    using Fonte.App.Dialogs;

    using System;
    using System.Linq;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public class AddComponentCommand : ICommand
    {
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter)
        {
            if (parameter is Data.Layer layer)
            {
                return layer.Parent?.Parent.Glyphs.Count > 1;
            }
            return false;
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
