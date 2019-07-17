/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Commands
{
    using System;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public class MakeGuidelineLocalCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;  // layer != null?
        }

        public void Execute(object parameter)
        {
            var (guideline, layer) = (ValueTuple<Data.Guideline, Data.Layer>)parameter;
            var master = (Data.Master)guideline.Parent;

            master.Guidelines.Remove(guideline);
            layer.Guidelines.Add(guideline);

            ((App)Application.Current).InvalidateData();
        }
    }
}
