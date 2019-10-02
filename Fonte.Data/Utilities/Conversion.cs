/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Utilities
{
    using Fonte.Data.Geometry;

    using System;
    using System.Numerics;

    public static class Conversion
    {
        public static Vector2 FromAngle(float angle)
        {
            return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }

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

        public static double ToDegrees(Vector2 vec)
        {
            return ToDegrees(ToRadians(vec));
        }

        public static double ToRadians(double angle)
        {
            return Math.PI * angle / 180;
        }

        public static float ToRadians(Vector2 vec)
        {
            return MathF.Atan2(vec.Y, vec.X);
        }

        public static float ToRadians(Vector2 u, Vector2 v)
        {
            var w = new Vector2(-u.Y, u.X);
            var det = Vector2.Dot(v, w);  // sin
            var dot = Vector2.Dot(u, v);  // cos
            if (det == 0 || dot == 0)
                throw new InvalidOperationException("Cannot compute angle from zero vector");

            return MathF.Atan2(det, dot);
        }
    }
}