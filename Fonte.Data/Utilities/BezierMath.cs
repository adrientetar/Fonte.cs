/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;

    public static class BezierMath
    {
        /**
         * Adapted from https://www.particleincell.com/2013/cubic-line-intersection/.
         */
        public static (Vector2, float)[] IntersectLineAndCurve(Vector2 p0, Vector2 p1, IList<Vector2> curve)
        {
            Debug.Assert(curve.Count == 4);

            var bx = p0.X - p1.X;          // bx = x1 - x2
            var by = p1.Y - p0.Y;          // by = y2 - y1
            var m = p0.X * (p0.Y - p1.Y) +  // m = x1 * (y1 - y2) + y1 * (x2 - x1)
                    p0.Y * (p1.X - p0.X);

            var (a, b, c, d) = CubicParameters(curve);
            var roots = CubicRoots(
                by * a.X + bx * a.Y,
                by * b.X + bx * b.Y,
                by * c.X + bx * c.Y,
                by * d.X + bx * d.Y + m
            );

            var result = new List<(Vector2, float)>();
            foreach (var t in roots)
            {
                if (0 <= t && t <= 1)
                {
                    var sx = ((a.X * t + b.X) * t + c.X) * t + d.X;
                    var sy = ((a.Y * t + b.Y) * t + c.Y) * t + d.Y;

                    var s = (p1.X - p0.X) != 0 ?
                            (sx - p0.X) / (p1.X - p0.X) :
                            (sy - p0.Y) / (p1.Y - p0.Y);
                    if (0 <= s && s <= 1)
                    {
                        result.Add((new Vector2((float)sx, (float)sy), (float)t));
                    }
                }
            }
            return result.ToArray();
        }

        /**
         * Adapted from Andre LaMothe, "Tricks of the Windows Game Programming Gurus".
         */
        public static (Vector2, float)? IntersectLines(Vector2 p0, Vector2 p1, IList<Vector2> line, bool testLineInfinite = false)
        {
            Debug.Assert(line.Count == 2);
            var p2 = line[0];
            var p3 = line[1];

            var p01 = p1 - p0;
            var p23 = p3 - p2;
            var determinant = p23.X * p01.Y - p01.X * p23.Y;

            if (Math.Abs(determinant) >= 1e-6)
            {
                var s = ( p23.X * (p2.Y - p0.Y) - p23.Y * (p2.X - p0.X)) / determinant;
                var t = ( p01.X * (p2.Y - p0.Y) - p01.Y * (p2.X - p0.X)) / determinant;
                if ((testLineInfinite || 0 <= s && s <= 1) &&
                    0 <= t && t <= 1)
                {
                    return (p2 + p23 * t, t);
                }
            }
            return null;
        }

        /**
         * Adapted from PaperJS getNearestTime().
         */
        public static (Vector2, float) ProjectPointOnCurve(Vector2 point, IList<Vector2> curve)
        {
            Debug.Assert(curve.Count == 4);
            var p0 = curve[0];
            var p1 = curve[1];
            var p2 = curve[2];
            var p3 = curve[3];

            var steps = 100;
            float minDistance_2 = float.PositiveInfinity;
            Vector2 minValue = Vector2.Zero;
            float minT = 0;

            bool refineProjection(float t)
            {
                if (t >= 0 && t <= 1)
                {
                    var ot = 1 - t;
                    var value = ot * ot * ot * p0 +
                            3 * ot * ot *  t * p1 +
                            3 * ot *  t *  t * p2 +
                                 t *  t *  t * p3;
                    var distance_2 = (value - point).LengthSquared();
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
                var t = (float)i / steps;
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
            return (minValue, minT);
        }

        public static (Vector2, float)? ProjectPointOnLine(Vector2 point, IList<Vector2> line)
        {
            Debug.Assert(line.Count == 2);
            var a = line[0];
            var b = line[1];

            var ab = b - a;
            var l2 = ab.LengthSquared();

            if (l2 != 0)
            {
                var ap = point - a;
                var t = Vector2.Dot(ap, ab) / l2;

                if (t >= 0 && t <= 1)
                {
                    return (a + t * ab, t);
                }
            }
            else
            {
                return (a, 0f);
            }

            return null;
        }

        /**
         * Adapted from bezierjs by Pomax.
         */
        public static Vector2[,] SplitCurve(IList<Vector2> curve, float t)
        {
            Debug.Assert(curve.Count == 4);
            var q = DeCasteljauDecomposition(curve, t);

            return new Vector2[,] { { q[0], q[4], q[7], q[9] }, { q[9], q[8], q[6], q[3] } };
        }

        static (Vector2, Vector2, Vector2, Vector2) CubicParameters(IList<Vector2> curve)
        {
            var p0 = curve[0];
            var p1 = curve[1];
            var p2 = curve[2];
            var p3 = curve[3];

            var bc = Vector2.Multiply(p2 - p1, 3f);
            var c = Vector2.Multiply(p1 - p0, 3f);
            return (
                p3 - p0 - bc,
                bc - c,
                c,
                p0
            );
        }

        /**
         * Adapted from Roots3And4 by Jochen Schwarze.
         */
        static double[] CubicRoots(float a, float b, float c, float d)
        {
            if (Math.Abs(a) < 1e-6)
            {
                return QuadraticRoots(b, c, d);
            }
            List<double> result = new List<double>();

            // normal form: x^3 + Ax^2 + Bx + C = 0
            var A = b / a;
            var B = c / a;
            var C = d / a;

            // substitute x = y - A/3 to eliminate quadric term:
            // x ^ 3 + px + q = 0
            var p = 1.0 / 3 * (-1.0 / 3 * A * A + B);
            var q = 1.0 / 2 * (2.0 / 27 * A * A * A - 1.0 / 3 * A * B + C);

            // Cardano's formula
            var p_3 = p * p * p;
            var D = q * q + p_3;

            if (Math.Abs(D) < 1e-6)
            {
                if (Math.Abs(q) < 1e-6)  // one triple solution
                {
                    result.Add(0);
                }
                else  // one single and one double solution
                {
                    var u = Math.Pow(-q, 1.0 / 3);//Math.Cbrt(-q);

                    result.Add(2 * u);
                    result.Add(-u);
                }
            }
            else if (D < 0)  // Casus irreducibilis: three real solutions
            {
                var phi = 1.0 / 3 * Math.Acos(-q / Math.Sqrt(-p_3));
                var t = 2 * Math.Sqrt(-p);

                result.Add( t * Math.Cos(phi));
                result.Add(-t * Math.Cos(phi + Math.PI / 3));
                result.Add(-t * Math.Cos(phi - Math.PI / 3));
            }
            else  // one real solution
            {
                var sqrt_D = Math.Sqrt(D);
                var u =  Math.Pow(sqrt_D - q, 1.0 / 3);// Math.Cbrt(sqrt_D - q);
                var v = -Math.Pow(sqrt_D + q, 1.0 / 3);//-Math.Cbrt(sqrt_D + q);

                result.Add(u + v);
            }

            // resubstitute
            var sub = 1.0 / 3 * A;
            return result.Select(r => r - sub)
                         .ToArray();
        }

        static Vector2[] DeCasteljauDecomposition(IList<Vector2> curve, float t)
        {
            var points = curve.ToList();

            var start = 0;
            var end = points.Count - 1;
            while (end - start > 0)
            {
                for (int i = start; i < end; ++i)
                {
                    var point = Vector2.Lerp(points[i], points[i + 1], t);
                    points.Add(point);
                }
                start = end + 1;
                end = points.Count - 1;
            }

            return points.ToArray();
        }

        /**
         * Adapted from Roots3And4 by Jochen Schwarze.
         */
        static double[] QuadraticRoots(float a, float b, float c)
        {
            List<double> result = new List<double>();

            // normal form: x^2 + px + q = 0
            var p = b / (2 * a);
            var q = c / a;

            var D = p * p - q;

            if (Math.Abs(D) < 1e-6)
            {
                result.Add(-p);
            }
            else if (D > 0)
            {
                var sqrt_D = Math.Sqrt(D);

                result.Add( sqrt_D - p);
                result.Add(-sqrt_D - p);
            }
            return result.ToArray();
        }
    }
}