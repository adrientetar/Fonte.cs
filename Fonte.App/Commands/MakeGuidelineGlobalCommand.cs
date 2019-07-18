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
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter)
        {
            return true;
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
