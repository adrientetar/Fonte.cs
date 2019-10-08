/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Utilities
{
    using System;
    using System.Diagnostics;
    using System.Numerics;

    public static class Ops
    {
        public const float PI_1_2 = 1f / 2 * MathF.PI;
        public const float PI_2 = 2 * MathF.PI;

        public static float AngleBetween(Vector2 u, Vector2 v)
        {
            var w = new Vector2(-u.Y, u.X);
            var det = Vector2.Dot(v, w);  // sin
            var dot = Vector2.Dot(u, v);  // cos
            if (det == 0 || dot == 0)
                throw new InvalidOperationException("Cannot compute angle from zero vector");

            return MathF.Atan2(det, dot);
        }

        public static float Modulo(float x, float m)
        {
            Debug.Assert(m >= 0);
            var r = x % m;

            return r < 0 ? r + m : r;
        }
    }
}
