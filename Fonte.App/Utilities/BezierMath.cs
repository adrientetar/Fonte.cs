/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Utilities
{
    using System.Numerics;

    class BezierMath
    {
        public static Vector2 ProjectPointOnLine(Vector2 point, Vector2 origin, Vector2 direction)
        {
            var pointDirection = point - origin;
            var t = Vector2.Dot(pointDirection, direction) / direction.LengthSquared();

            return origin + t * direction;
        }
    }
}
