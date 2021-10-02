// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Microsoft.UI.Xaml;

using System;
using System.Windows.Input;


namespace Fonte.App.Commands
{
    public class SetStartPointCommand : ICommand
    {
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter)
        {
            return parameter is Data.Point point &&
                   point.Parent is Data.Path path &&
                   !path.IsOpen;
        }

        public void Execute(object parameter)
        {
            var point = (Data.Point)parameter;
            var path = point.Parent;

            path.StartAt(path.Points.IndexOf(point));
            ((App)Application.Current).InvalidateData();
        }
    }
}
