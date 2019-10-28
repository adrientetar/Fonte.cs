// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Commands
{
    using System;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public class ReversePathCommand : ICommand
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
            var path = (Data.Path)parameter;

            path.Reverse();
            ((App)Application.Current).InvalidateData();
        }
    }
}
