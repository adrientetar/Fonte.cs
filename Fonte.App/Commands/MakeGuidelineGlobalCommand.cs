/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Commands
{
    using System;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public class MakeGuidelineGlobalCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;  // layer != null?
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
