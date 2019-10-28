// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Commands
{
    using Fonte.App.Utilities;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public abstract class BinaryBooleanOpCommand : ICommand
    {
        protected abstract Func<IEnumerable<Data.Path>, IEnumerable<Data.Path>, List<Data.Path>> BooleanFunc
        { get; }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (parameter is Data.Layer layer)
            {
                return layer.Paths.Where(path => !path.IsOpen)
                                  .Skip(1).Any();
            }
            return false;
        }

        public void Execute(object parameter)
        {
            var layer = (Data.Layer)parameter;
            var useSelection = Enumerable.Any(layer.Selection, item => item is Data.Point);

            var usePaths = new List<Data.Path>();
            var retainPaths = new List<Data.Path>();
            Data.Path refPath = null;
            foreach (var path in layer.Paths)
            {
                if (path.IsOpen)
                {
                    retainPaths.Add(path);
                }
                //else
                //{
                if (refPath == null && Enumerable.Any(path.Points, point => point.IsSelected))
                {
                    refPath = path;
                }
                else
                {
                    usePaths.Add(path);
                }
                //}
            }
            // TODO: consider dropping this behavior, more confusing than useful
            if (refPath == null)
            {
                refPath = usePaths.Last();
                usePaths.RemoveAt(usePaths.Count - 1);
            }

            var refPathArray = new Data.Path[] { refPath };
            if (BooleanOps.HasOverlaps(usePaths, refPathArray))
            {
                var resultPaths = BooleanFunc(usePaths, refPathArray);

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

    public class SubtractPathsCommand : BinaryBooleanOpCommand
    {
        protected override Func<IEnumerable<Data.Path>, IEnumerable<Data.Path>, List<Data.Path>> BooleanFunc { get; } = (a, b) => BooleanOps.Exclude(a, b);
    }

    public class IntersectPathsCommand : BinaryBooleanOpCommand
    {
        protected override Func<IEnumerable<Data.Path>, IEnumerable<Data.Path>, List<Data.Path>> BooleanFunc { get; } = (a, b) => BooleanOps.Intersect(a, b);
    }

    public class XorPathsCommand : BinaryBooleanOpCommand
    {
        protected override Func<IEnumerable<Data.Path>, IEnumerable<Data.Path>, List<Data.Path>> BooleanFunc { get; } = (a, b) => BooleanOps.Xor(a, b);
    }
}
