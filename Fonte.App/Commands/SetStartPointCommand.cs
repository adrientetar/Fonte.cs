/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Commands
{
    using System;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public class SetStartPointCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;  // layer != null?
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
