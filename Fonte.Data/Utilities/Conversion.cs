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
        public static Rect FromFoundationRect(Windows.Foundation.Rect rect)
        {
            return new Rect(
                (float)rect.X,
                (float)rect.Y,
                (float)rect.Width,
                (float)rect.Height);
        }

        /**/

        public static float FromDegrees(float angle)
        {
            return MathF.PI * angle / 180;
        }

        public static float ToDegrees(float angle)
        {
            return 180 * angle / MathF.PI;
        }

        public static string FromUnicode(string uni)
        {
            return char.ConvertFromUtf32(Convert.ToInt32(uni, 16));
        }

        public static string ToUnicode(string str)
        {
            return char.ConvertToUtf32(str, 0).ToString("X4");
        }

        public static float FromVector(Vector2 vec)
        {
            return MathF.Atan2(vec.Y, vec.X);
        }

        public static Vector2 ToVector(float angle)
        {
            return new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        }
    }
}