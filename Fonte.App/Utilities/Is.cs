/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

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