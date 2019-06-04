/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Utilities
{
    using Fonte.Data.Geometry;

    using System;
    using System.Collections.Generic;
    using System.Numerics;

    public class BezierMath
    {
        // Adapted from PaperJS getNearestTime().
        public static Vector2 ProjectPointOnCurve(Vector2 point, List<Point> curve)
        {
            var p0 = curve[0].ToVector2();
            var p1 = curve[1].ToVector2();
            var p2 = curve[2].ToVector2();
            var p3 = curve[3].ToVector2();

            var steps = 100;
            float minDistance_2 = float.PositiveInfinity;
            Vector2 minValue = Vector2.Zero;
            float minT = float.NaN;

            bool refineProjection(float t)
            {
                if (t >= 0 && t <= 1)
                {
                    var ot = 1 - t;
                    var value = ot * ot * ot * p0 +
                            3 * ot * ot *  t * p1 +
                            3 * ot *  t *  t * p2 +
                                 t *  t *  t * p3;
                    var delta = value - point;
                    var distance_2 = delta.LengthSquared();
                    if (distance_2 < minDistance_2)
                    {
                        minDistance_2 = distance_2;
                        minValue = value;
                        minT = t;
                        return true;
                    }
                }
                return false;
            };

            for (int i = 0; i <= steps; ++i)
            {
                var t = i / steps;
                refineProjection(t);
            }

            var step = 1f / (2 * steps);
            while (step > 1e-8)
            {
                if (!refineProjection(minT - step) && !refineProjection(minT + step))
                {
                    step = .5f * step;
                }
            }
            return minValue;
        }

        public static Vector2? ProjectPointOnLine(Vector2 point, List<Point> line)
        {
            var a = line[0].ToVector2();
            var b = line[1].ToVector2();

            var ab = b - a;
            var l2 = ab.LengthSquared();

            if (l2 != 0)
            {
                var ap = point - a;
                var t = Vector2.Dot(ap, ab) / l2;

                if (t >= 0 && t <= 1)
                {
                    return a + t * ab;
                }
            }
            else
            {
                return a;
            }

            return null;
        }
    }
}