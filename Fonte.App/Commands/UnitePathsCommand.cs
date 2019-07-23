/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Commands
{
    using Fonte.App.Utilities;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public class UnitePathsCommand : ICommand
    {
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter)
        {
            if (parameter is Data.Layer layer)
            {
                return layer.Paths.Count > 0;
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

            var resultPaths = BooleanOps.Union(usePaths);
            if (resultPaths.Select(path => path.Points.Count).Sum() !=
                usePaths.Select(path => path.Points.Count).Sum())
            {
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
