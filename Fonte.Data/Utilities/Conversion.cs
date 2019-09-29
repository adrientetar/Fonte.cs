/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Utilities
{
    using Fonte.Data.Geometry;

    using System;

    public static class Conversion
    {
        public static Rect FromFoundationRect(Windows.Foundation.Rect rect)
        {
            return new Rect(
                (float)rect.X,
                (float)rect.Y,
                (float)rect.Width,
                (float)rect.Height);
        }

        public static double ToDegrees(double angle)
        {
            return 180 * angle / Math.PI;
        }

        public static double ToRadians(double angle)
        {
            return Math.PI * angle / 180;
        }
    }
}