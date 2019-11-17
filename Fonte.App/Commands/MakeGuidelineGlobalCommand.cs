// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Commands
{
    using System;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public class MakeGuidelineGlobalCommand : ICommand
    {
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter)
        {
            return parameter is Data.Guideline;
        }

        public void Execute(object parameter)
        {
            var guideline = (Data.Guideline)parameter;
            var layer = (Data.Layer)guideline.Parent;
            var master = layer.Master;

            layer.Guidelines.Remove(guideline);
            master.Guidelines.Add(guideline);

            ((App)Application.Current).InvalidateData();
        }
    }
}
