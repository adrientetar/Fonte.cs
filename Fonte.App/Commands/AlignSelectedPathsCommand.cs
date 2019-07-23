/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Commands
{
    using Fonte.Data.Geometry;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public abstract class AlignSelectedPathsCommand : ICommand
    {
        protected abstract Func<Data.Path, Data.Geometry.Rect, Vector2> TransformFunc
        { get; }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (parameter is Data.Layer layer)
            {
                return layer.Selection.OfType<Data.Point>().Any();
            }
            return false;
        }

        public void Execute(object parameter)
        {
            var layer = (Data.Layer)parameter;

            if (AlignSelectedPaths(layer, TransformFunc))
            {
                ((App)Application.Current).InvalidateData();
            }
        }

        public void NotifyCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, null);
        }

        bool AlignSelectedPaths(Data.Layer layer, Func<Data.Path, Rect, Vector2> transformFunc)
        {
            var ret = false;

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
                // If only one path is selected, we'll align it to the metrics rect
                if (selectedPaths.Count == 1)
                {
                    selectedBounds = new Rect(0, 0, layer.Width, 0);

                    if (layer.Master is Data.Master master)
                    {
                        selectedBounds.Y = master.Descender;
                        selectedBounds.Height = Math.Max(master.Ascender, master.CapHeight) - selectedBounds.Y;
                    }
                }

                using (var group = layer.CreateUndoGroup())
                {
                    foreach (var path in selectedPaths)
                    {
                        var delta = transformFunc(path, selectedBounds);

                        if (delta.Length() != 0)
                        {
                            path.Transform(Matrix3x2.CreateTranslation(delta));

                            ret = true;
                        }
                    }
                }
            }
            return ret;
        }
    }

    public class AlignLeftCommand : AlignSelectedPathsCommand
    {
        protected override Func<Data.Path, Data.Geometry.Rect, Vector2> TransformFunc { get; } = (path, refBounds) => new Vector2(
                path.Bounds.Left > refBounds.Left ? refBounds.Left - path.Bounds.Left : 0,
                0
            );
    }

    public class CenterHorizontallyCommand : AlignSelectedPathsCommand
    {
        protected override Func<Data.Path, Data.Geometry.Rect, Vector2> TransformFunc { get; } = (path, refBounds) => {
                var refXMid = refBounds.Left + Math.Round(.5 * refBounds.Width);
                var xMid = path.Bounds.Left + Math.Round(.5 * path.Bounds.Width);
                return new Vector2(
                    (float)(refXMid - xMid),
                    0
                );
            };
    }

    public class AlignRightCommand : AlignSelectedPathsCommand
    {
        protected override Func<Data.Path, Data.Geometry.Rect, Vector2> TransformFunc { get; } = (path, refBounds) => new Vector2(
                path.Bounds.Right < refBounds.Right ? refBounds.Right - path.Bounds.Right : 0,
                0
            );
    }

    public class AlignTopCommand : AlignSelectedPathsCommand
    {
        protected override Func<Data.Path, Data.Geometry.Rect, Vector2> TransformFunc { get; } = (path, refBounds) => new Vector2(
                0,
                path.Bounds.Top < refBounds.Top ? refBounds.Top - path.Bounds.Top : 0
            );
    }

    public class CenterVerticallyCommand : AlignSelectedPathsCommand
    {
        protected override Func<Data.Path, Data.Geometry.Rect, Vector2> TransformFunc { get; } = (path, refBounds) => {
                var refYMid = refBounds.Bottom + Math.Round(.5 * refBounds.Height);
                var yMid = path.Bounds.Bottom + Math.Round(.5 * path.Bounds.Height);
                return new Vector2(
                    0,
                    (float)(refYMid - yMid)
                );
            };
    }

    public class AlignBottomCommand : AlignSelectedPathsCommand
    {
        protected override Func<Data.Path, Data.Geometry.Rect, Vector2> TransformFunc { get; } = (path, refBounds) => new Vector2(
                0,
                path.Bounds.Bottom > refBounds.Bottom ? refBounds.Bottom - path.Bounds.Bottom : 0
            );
    }
}
