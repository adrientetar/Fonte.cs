/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;

    class BezierMath
    {
        public static Vector2[] FitCubic(List<Vector2> points, float err)
        {
            if (points.Count < 2)
                throw new ArgumentException("At least two points are required for curve fitting");

            var leftTangent = Vector2.Normalize(points[1] - points[0]);
            var rightTangent = Vector2.Normalize(points[points.Count - 2] - points[points.Count - 1]);
            return FitCubic(points, leftTangent, rightTangent, err);
        }

        /**
         * Adapted from "Algorithm for Automatically Fitting Digitized Curves" by Philip J. Schneider.
         */
        public static Vector2[] FitCubic(List<Vector2> points, Vector2 leftTangent, Vector2 rightTangent, float err)
        {
            if (points.Count < 2)
                throw new ArgumentException("At least two points are required for curve fitting");
            else if (points.Count == 2)
            {
                var dist = (points[1] - points[0]).Length() / 3;
                return new Vector2[] { points[0], points[0] + leftTangent * dist, points[1] + rightTangent * dist, points[1] };
            }

            var u = ChordLengthParameters(points);
            var curve = GenerateBezier(points, u, leftTangent, rightTangent);
            var maxError = ComputeMaxError(points, curve, u);
            if (maxError < err)
            {
                return curve;
            }

            //if (maxError < err * err)
            {
                for (int i = 0; i < 20; ++i)
                {
                    var uPrime = Reparametrize(curve, points, u);
                    curve = GenerateBezier(points, uPrime, leftTangent, rightTangent);
                    maxError = ComputeMaxError(points, curve, uPrime);
                    if (maxError < err)
                    {
                        return curve;
                    }
                    u = uPrime;
                }
            }

            return curve;
        }

        static float[] ChordLengthParameters(List<Vector2> points)
        {
            var u = new float[points.Count];

            u[0] = 0f;
            for (int i = 1; i < points.Count; ++i)
            {
                u[i] = u[i - 1] + (points[i] - points[i - 1]).Length();
            }
            var last = u[points.Count - 1];
            for (int i = 1; i < points.Count; ++i)
            {
                u[i] = u[i] / last;
            }

            return u;
        }

        static float ComputeMaxError(List<Vector2> points, IList<Vector2> curve, IList<float> parameters)
        {
            var maxDist = 0f;
            for (int i = 0; i < parameters.Count; ++i)
            {
                var u = parameters[i];
                var dist = (Q(curve, u) - points[i]).LengthSquared();
                if (dist > maxDist)
                {
                    maxDist = dist;
                }
            }

            return maxDist;
        }

        static Vector2[] GenerateBezier(List<Vector2> points, IList<float> parameters, Vector2 leftTangent, Vector2 rightTangent)
        {
            var curve = new Vector2[] { points[0], default, default, points[points.Count - 1] };

            var A = new Vector2[parameters.Count, 2];
            for (int i = 0; i < parameters.Count; ++i)
            {
                var u = parameters[i];

                A[i, 0] = leftTangent  * 3 * (1 - u) * (1 - u) * u;
                A[i, 1] = rightTangent * 3 * (1 - u) * u * u;
            }

            var C = new float[2, 2] { { 0, 0 }, { 0, 0 } };
            var X = new float[2] { 0, 0 };
            for (int i = 0; i < parameters.Count; ++i)
            {
                C[0, 0] += Vector2.Dot(A[i, 0], A[i, 0]);
                C[0, 1] += Vector2.Dot(A[i, 0], A[i, 1]);
                C[1, 0] += Vector2.Dot(A[i, 0], A[i, 1]);
                C[1, 1] += Vector2.Dot(A[i, 1], A[i, 1]);

                var u = parameters[i];
                var pt = points[i] - Q(new Vector2[] { points[0], points[0], points.Last(), points.Last() }, u);

                X[0] += Vector2.Dot(A[i, 0], pt);
                X[1] += Vector2.Dot(A[i, 1], pt);
            }

            var det_C0_C1 = C[0, 0] * C[1, 1] - C[1, 0] * C[0, 1];
            var det_C0_X  = C[0, 0] * X[1]    - C[1, 0] * X[0];
            var det_X_C1  = X[0]    * C[1, 1] - X[1]    * C[0, 1];

            var leftAlpha = Math.Abs(det_C0_C1) <= 1e-6 ? 0f : det_X_C1 / det_C0_C1;
            var rightAlpha = Math.Abs(det_C0_C1) <= 1e-6 ? 0f : det_C0_X / det_C0_C1;

            var segLength = (points[0] - points.Last()).Length();
            var epsilon = 1e-6 * segLength;
            if (leftAlpha < epsilon || rightAlpha < epsilon)
            {
                curve[1] = curve[0] + leftTangent * (segLength / 3);
                curve[2] = curve[3] + rightTangent * (segLength / 3);
            }
            else
            {
                curve[1] = curve[0] + leftTangent * leftAlpha;
                curve[2] = curve[3] + rightTangent * rightAlpha;
            }

            return curve;
        }

        static float ManhattanLength(Vector2 value)
        {
            return Vector2.Dot(value, Vector2.One);
        }

        static float NewtonRaphson(IList<Vector2> curve, Vector2 point, float u)
        {
            var d = Q(curve, u) - point;
            var qp = QPrime(curve, u);

            var denominator = ManhattanLength(qp * qp + d * QPrimePrime(curve, u));
            if (Math.Abs(denominator) < 1e-6)
            {
                return u;
            }

            var numerator = ManhattanLength(d * qp);
            return u - numerator / denominator;
        }

        static float[] Reparametrize(IList<Vector2> curve, List<Vector2> points, IList<float> parameters)
        {
            var u = new float[parameters.Count];

            for (int i = 0; i < parameters.Count; ++i)
            {
                u[i] = NewtonRaphson(curve, points[i], parameters[i]);
            }

            return u;
        }

        /**/

        /**
         * Adapted from BezierInfo by Pomax.
         */
        public static (Vector2, Vector2) ConstrainCurve(IList<Vector2> curve, Vector2 point, float t)
        {
            Debug.Assert(curve.Count == 4);
            var p0 = curve[0];
            var p3 = curve[3];

            var q = DeCasteljauDecomposition(curve, t);
            var initialA = q[5];
            var initialPoint = q[9];

            var bottom = t * t * t + (1 - t) * (1 - t) * (1 - t);
            var top = bottom - 1;
            var ratio = Math.Abs(top / bottom);

            var C = Data.Utilities.BezierMath.IntersectLines(initialA, initialPoint, new Vector2[] { p0, p3 }, testLineInfinite: true) switch
            {
                var (c, _) => c,
                _ => initialA + .5f * (initialPoint - initialA)
            };
            var A = point - (C - point) / ratio;

            var dbl = q[7] - initialPoint;
            var dbr = q[8] - initialPoint;

            var pc1 = point + dbl;
            var sc1 = A - (A - pc1) / (1 - t);

            var pc2 = point + dbr;
            var sc2 = A + (pc2 - A) / t;

            return (
                p0 + (sc1 - p0) / t,
                p3 - (p3 - sc2) / (1 - t)
            );
        }

        public static Vector2 ProjectPointOnLine(Vector2 point, Vector2 origin, Vector2 direction)
        {
            var pointDirection = point - origin;
            var t = Vector2.Dot(pointDirection, direction) / direction.LengthSquared();

            return origin + t * direction;
        }

        public static Vector2 Q(IList<Vector2> points, float t)
        {
            return points[0] * (1 - t) * (1 - t) * (1 - t) +
                   points[1] * 3 * (1 - t) * (1 - t) * t +
                   points[2] * 3 * (1 - t) * t * t +
                   points[3] * t * t * t;
        }

        static Vector2 QPrime(IList<Vector2> points, float t)
        {
            return (points[1] - points[0]) * 3 * (1 - t) * (1 - t) +
                   (points[2] - points[1]) * 6 * (1 - t) * t +
                   (points[3] - points[2]) * 3 * t * t;
        }

        static Vector2 QPrimePrime(IList<Vector2> points, float t)
        {
            return (points[2] - 2 * points[1] + points[0]) * 6 * (1 - t) +
                   (points[3] - 2 * points[2] + points[1]) * 6 * t;
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
    }
}
