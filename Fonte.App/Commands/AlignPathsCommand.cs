// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Commands
{
    using Fonte.Data.Geometry;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public abstract class AlignPathsCommand : ICommand
    {
        protected abstract Func<Data.Path, Rect, Vector2> TransformFunc
        { get; }

        public event EventHandler CanExecuteChanged;

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

            AlignPaths(layer, TransformFunc);
            ((App)Application.Current).InvalidateData();
        }

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, null);
        }

        void AlignPaths(Data.Layer layer, Func<Data.Path, Rect, Vector2> transformFunc)
        {
            Rect targetBounds;
            IEnumerable<Data.Path> targetPaths;

            bool alignToMetricsRect;
            if (layer.Selection.OfType<Data.Point>().Any())
            {
                var selectedBounds = Rect.Empty;
                var selectedPaths = new List<Data.Path>();
                foreach (var path in layer.Paths)
                {
                    if (Enumerable.Any(path.Points, point => point.IsSelected))
                    {
                        selectedBounds.Union(path.Bounds);
                        selectedPaths.Add(path);
                    }
                }

                targetBounds = selectedBounds;
                targetPaths = selectedPaths;
                alignToMetricsRect = selectedPaths.Count == 1;
            }
            else
            {
                targetBounds = default;  // this just to appease the compiler..
                targetPaths = layer.Paths;
                alignToMetricsRect = true;
            }

            if (alignToMetricsRect)
            {
                targetBounds = new Rect(0, 0, layer.Width, 0);

                if (layer.Master is Data.Master master)
                {
                    targetBounds.Y = master.Descender;
                    targetBounds.Height = Math.Max(master.Ascender, master.CapHeight) - targetBounds.Y;
                }
            }

            using (var group = layer.CreateUndoGroup())
            {
                foreach (var path in targetPaths)
                {
                    var delta = transformFunc(path, targetBounds);

                    if (delta != Vector2.Zero)
                    {
                        path.Transform(Matrix3x2.CreateTranslation(delta));
                    }
                }
            }
        }
    }

    public class AlignLeftCommand : AlignPathsCommand
    {
        protected override Func<Data.Path, Rect, Vector2> TransformFunc { get; } = (path, refBounds) => new Vector2(
                refBounds.Left - path.Bounds.Left,
                0
            );
    }

    public class CenterHorizontallyCommand : AlignPathsCommand
    {
        protected override Func<Data.Path, Rect, Vector2> TransformFunc { get; } = (path, refBounds) => {
                var refXMid = refBounds.Left + Math.Round(.5 * refBounds.Width);
                var xMid = path.Bounds.Left + Math.Round(.5 * path.Bounds.Width);
                return new Vector2(
                    (float)(refXMid - xMid),
                    0
                );
            };
    }

    public class AlignRightCommand : AlignPathsCommand
    {
        protected override Func<Data.Path, Rect, Vector2> TransformFunc { get; } = (path, refBounds) => new Vector2(
                refBounds.Right - path.Bounds.Right,
                0
            );
    }

    public class AlignTopCommand : AlignPathsCommand
    {
        protected override Func<Data.Path, Rect, Vector2> TransformFunc { get; } = (path, refBounds) => new Vector2(
                0,
                refBounds.Top - path.Bounds.Top
            );
    }

    public class CenterVerticallyCommand : AlignPathsCommand
    {
        protected override Func<Data.Path, Rect, Vector2> TransformFunc { get; } = (path, refBounds) => {
                var refYMid = refBounds.Bottom + Math.Round(.5 * refBounds.Height);
                var yMid = path.Bounds.Bottom + Math.Round(.5 * path.Bounds.Height);
                return new Vector2(
                    0,
                    (float)(refYMid - yMid)
                );
            };
    }

    public class AlignBottomCommand : AlignPathsCommand
    {
        protected override Func<Data.Path, Rect, Vector2> TransformFunc { get; } = (path, refBounds) => new Vector2(
                0,
                refBounds.Bottom - path.Bounds.Bottom
            );
    }
}
