// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Commands
{
    using System;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public class DeleteLayerCommand : ICommand
    {
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter)
        {
            return parameter is Data.Layer layer && !layer.IsMasterLayer;
        }

        public void Execute(object parameter)
        {
            var layer = (Data.Layer)parameter;
            var glyph = layer.Parent;

            glyph.Layers.Remove(layer);
            ((App)Application.Current).InvalidateData();
        }
    }
}
