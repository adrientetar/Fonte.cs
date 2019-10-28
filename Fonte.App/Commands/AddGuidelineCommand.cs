// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Commands
{
    using Fonte.App.Controls;
    using Fonte.App.Utilities;

    using System;
    using System.Windows.Input;
    using Windows.Foundation;
    using Windows.UI.Xaml;

    public class AddGuidelineCommand : ICommand
    {
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            var (canvas, pos) = (ValueTuple<DesignCanvas, Point>)parameter;
            var layer = canvas.Layer;

            var guideline = new Data.Guideline(
                Outline.RoundToGrid((float)pos.X),
                Outline.RoundToGrid((float)pos.Y),
                0
            );
            layer.Guidelines.Add(guideline);
            layer.ClearSelection();
            guideline.IsSelected = true;
            ((App)Application.Current).InvalidateData();
        }
    }
}
