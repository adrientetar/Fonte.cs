/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Commands
{
    using System;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public class ReverseAllPathsCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;  // layer != null?
        }

        public void Execute(object parameter)
        {
            var layer = (Data.Layer)parameter;

            foreach (var path in layer.Paths)
            {
                path.Reverse();
            }
            ((App)Application.Current).InvalidateData();
        }
    }
}
