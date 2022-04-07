// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Commands
{
    using Fonte.App.Utilities;

    using System;
    using System.Linq;
    using System.Windows.Input;
    using Windows.UI.Xaml;

    public class BalanceHandlesCommand : ICommand
    {
#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        public bool CanExecute(object parameter)
        {
            if (parameter is Data.Layer layer)
            {
                return layer.Selection.Where(item => item is Data.Point point && point.Type == Data.PointType.None)
                                      .Any();
            }
            return false;
        }

        public void Execute(object parameter)
        {
            var layer = (Data.Layer)parameter;
            var ok = false;
            using var group = layer.CreateUndoGroup();

            foreach (var path in layer.Paths)
            {
                foreach (var segment in path.Segments)
                {
                    if (Outline.AnyOffCurveSelected(segment))
                    {
                        var curve = segment.PointsInclusive;
                        var result = Outline.GetHandlesPercentage(curve);

                        if (result != null)
                        {
                            Outline.StretchCurve(layer, curve, .5f * (MathF.Min(result.Value.Item1, 1f) +
                                                                      MathF.Min(result.Value.Item2, 1f)));
                            ok = true;
                        }
                    }
                }
            }

            if (ok) ((App)Application.Current).InvalidateData();
        }
    }
}
