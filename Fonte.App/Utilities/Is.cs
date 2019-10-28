// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Utilities
{
    public static class Is
    {
        public static bool AtOpenBoundary(Data.Point point)
        {
            var path = point.Parent;
            if (path.IsOpen)
            {
                return path.Points[0] == point || path.Points[path.Points.Count - 1] == point;
            }
            return false;
        }
    }
}