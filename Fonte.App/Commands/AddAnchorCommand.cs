/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Commands
{
    using Fonte.App.Controls;
    using Fonte.App.Utilities;

    using System;
    using System.Windows.Input;
    using Windows.Foundation;
    using Windows.UI.Xaml;

    public class AddAnchorCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;  // layer != null?
        }

        public void Execute(object parameter)
        {
            var (canvas, pos) = (ValueTuple<DesignCanvas, Point>)parameter;
            var layer = canvas.Layer;

            var anchor = new Data.Anchor(
                Outline.RoundToGrid((float)pos.X),
                Outline.RoundToGrid((float)pos.Y),
                "new anchor"
            );
            layer.Anchors.Add(anchor);
            layer.ClearSelection();
            anchor.IsSelected = true;
            ((App)Application.Current).InvalidateData();

            canvas.EditAnchorName(anchor);
        }
    }
}
