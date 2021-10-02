// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Fonte.App.Utilities;
using Microsoft.UI.Xaml;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;


namespace Fonte.App.Commands
{
    public class UnitePathsCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (parameter is Data.Layer layer)
            {
                return layer.Paths.Where(path => !path.IsOpen)
                                  .Any();
            }
            return false;
        }

        public void Execute(object parameter)
        {
            var layer = (Data.Layer)parameter;
            var useSelection = Enumerable.Any(layer.Selection, item => item is Data.Point);

            var usePaths = new List<Data.Path>();
            var retainPaths = new List<Data.Path>();
            foreach (var path in layer.Paths)
            {
                if (path.IsOpen)
                {
                    retainPaths.Add(path);
                }
                else
                {
                    if (useSelection && !Enumerable.Any(path.Points, point => point.IsSelected))
                    {
                        retainPaths.Add(path);
                    }
                    else
                    {
                        usePaths.Add(path);
                    }
                }
            }

            if (BooleanOps.HasOverlaps(usePaths))
            {
                var resultPaths = BooleanOps.Union(usePaths);

                using (var group = layer.CreateUndoGroup())
                {
                    layer.Paths.Clear();
                    layer.Paths.AddRange(resultPaths);
                    layer.Paths.AddRange(retainPaths);

                    ((App)Application.Current).InvalidateData();
                }
            }
        }

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, null);
        }
    }
}
