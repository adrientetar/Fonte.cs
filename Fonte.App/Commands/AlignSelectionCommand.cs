/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Commands
{
    using Fonte.App.Utilities;

    using System;
    using System.Linq;
    using System.Numerics;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public class AlignSelectionCommand : ICommand
    {
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter)
        {
            if (parameter is Data.Layer layer)
            {
                return layer.Selection.Count > 1;
            }
            return false;
        }

        public void Execute(object parameter)
        {
            var layer = (Data.Layer)parameter;
            var rect = Data.Geometry.Rect.Empty;

            foreach (Data.Point point in layer.Selection.OfType<Data.Point>())
            {
                rect.Union(point.ToVector2());
            }

            var value = new Vector2(
                Outline.RoundToGrid(rect.Left + .5f * rect.Width),
                Outline.RoundToGrid(rect.Bottom + .5f * rect.Height));
            var vertAxis = rect.Width > rect.Height;
            foreach (Data.Point point in layer.Selection.OfType<Data.Point>())
            {
                if (vertAxis)
                {
                    point.Y = value.Y;
                }
                else
                {
                    point.X = value.X;
                }
            }

            ((App)Application.Current).InvalidateData();
        }
    }
}
