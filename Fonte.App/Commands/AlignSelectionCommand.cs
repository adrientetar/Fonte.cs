// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Fonte.App.Utilities;
using Microsoft.UI.Xaml;

using System;
using System.Linq;
using System.Numerics;
using System.Windows.Input;


namespace Fonte.App.Commands
{
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
