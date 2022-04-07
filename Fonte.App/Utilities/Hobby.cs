// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    static class Hobby
    {
        static readonly float OMEGA = 0f;

        // XXX: impl dir0, dirn
        public static Vector2[] SolveOpen__(List<Vector2> points, float? dir0 = null, float? dirn = null)
        {
            var n = points.Count - 1;
            // the distances between consecutive points, called "d" in the
            // video; D[i] is the distance between P[i] and P[i+1]
            var D = new float[n];
            // the coordinate-wise directed distances between consecutive points,
            // i.e. dx[i] and is difference Px[i+1]-Px[i] and dy[i] likewise
            var deltas = new Vector2[n];
            for (var i = 0; i < n; ++i)
            {
                deltas[i] = points[i + 1] - points[i];
                D[i] = deltas[i].Length();
            }
            // the turning angles at each point, called "gamma" in the video;
            // gamma[i] is the angle at the point P[i]
            var gamma = new float[n + 1];
            for (var i = 1; i < n; ++i)
            {
                // compute sine and cosine of direction from P[i-1] to P[i]
                // (relative to x-axis)
                var sin = deltas[i - 1].Y / D[i - 1];
                var cos = deltas[i - 1].X / D[i - 1];
                // rotate deltas[i] so that for atan2 the "x-axis" is the
                // previous direction
                var rot = RotateBy(deltas[i], -sin, cos);
                gamma[i] = (float)Math.Atan2(rot.Y, rot.X); // i.e. Arg
            }
            // the "last angle" is zero, see for example "Jackowski:
            // Typographers, programmers and mathematicians"
            gamma[n] = 0;
            // a, b, and c are the diagonals of the tridiagonal matrix, d is the
            // right side
            var a = new float[n + 1];
            var b = new float[n + 1];
            var c = new float[n + 1];
            var d = new float[n + 1];
            // like in closed curve below
            for (var i = 1; i < n; ++i)
            {
                a[i] = 1 / D[i - 1];
                b[i] = (2 * D[i - 1] + 2 * D[i]) / (D[i - 1] * D[i]);
                c[i] = 1 / D[i];
                d[i] = -(2 * gamma[i] * D[i] + gamma[i + 1] * D[i - 1]) / (D[i - 1] * D[i]);
            }
            // see the Jackowski article for the following values; the result
            // will be that the curvature at the first point is identical to the
            // curvature at the second point (and likewise for the last and
            // second-to-last)
            b[0] = 2 + OMEGA;
            c[0] = 2 * OMEGA + 1;
            d[0] = -c[0] * gamma[1];
            a[n] = 2 * OMEGA + 1;
            b[n] = 2 + OMEGA;
            d[n] = 0;
            // solve system for the angles called "alpha" in the video
            var alpha = SolveThomas(a, b, c, d);
            // compute "beta" angles from "alpha" angles
            var beta = new float[n];
            for (var i = 0; i < n - 1; ++i)
                beta[i] = -gamma[i + 1] - alpha[i + 1];
            // again, see Jackowski article
            beta[n - 1] = -alpha[n];
            // now compute control point positions from angles and distances
            var res = new Vector2[2 * n];
            for (var i = 0; i < n; ++i)
            {
                var a_ = Rho(alpha[i], beta[i]) * D[i] / 3;
                var b_ = Rho(beta[i], alpha[i]) * D[i] / 3;
                var dir = Vector2.Normalize(
                    RotateByAngle(deltas[i], alpha[i])
                );
                res[2 * i] = points[i] + a_ * dir;  // cp1
                dir = Vector2.Normalize(
                    RotateByAngle(deltas[i], -beta[i])
                );
                res[2 * i + 1] = points[i + 1] - b_ * dir;  // cp2
            }
            return res;
        }

        // the "velocity function" (also called rho in the video); a and b are
        // the angles alpha and beta, the return value is the distance between
        // a control point and its neighboring point; to compute sigma(a,b)
        // we'll simply use rho(b,a)
        static float Rho(float a, float b)
        {
            // see video for formula
            var sin_a = Math.Sin(a);
            var sin_b = Math.Sin(b);
            var cos_a = Math.Cos(a);
            var cos_b = Math.Cos(b);
            var s5 = Math.Sqrt(5);
            var num = 4 + Math.Sqrt(8) * (sin_a - sin_b / 16) * (sin_b - sin_a / 16) * (cos_a - cos_b);
            var den = 2 + (s5 - 1) * cos_a + (3 - s5) * cos_b;
            return (float)(num / den);
        }

        static Vector2 RotateBy(Vector2 point, float sin, float cos)
        {
            return new Vector2(
                point.X * cos - point.Y * sin,
                point.X * sin + point.Y * cos
            );
        }

        static Vector2 RotateByAngle(Vector2 point, float alpha)
        {
            return RotateBy(point, (float)Math.Sin(alpha), (float)Math.Cos(alpha));
        }

        // Implements the Thomas algorithm for a tridiagonal system with i-th
        // row a[i]x[i-1] + b[i]x[i] + c[i]x[i+1] = d[i] starting with row
        // i=0, ending with row i=n-1 and with a[0] = c[n-1] = 0.  Returns the
        // values x[i] as an array.
        static float[] SolveThomas(float[] a, float[] b, float[] c, float[] d)
        {
            var n = a.Length;
            var cc = new float[n];
            var dd = new float[n];
            // forward sweep
            cc[0] = c[0] / b[0];
            dd[0] = d[0] / b[0];
            for (var i = 1; i < n; ++i)
            {
                var den = b[i] - cc[i - 1] * a[i];
                cc[i] = c[i] / den;
                dd[i] = (d[i] - dd[i - 1] * a[i]) / den;
            }
            var x = new float[n];
            // back substitution
            x[n - 1] = dd[n - 1];
            for (var i = n - 2; i >= 0; --i)
                x[i] = dd[i] - cc[i] * x[i + 1];
            return x;
        }

        /**/

        static readonly float A = (float)Math.Sqrt(2);
        static readonly float B = 1f / 16;
        static readonly float C = (3 - MathF.Sqrt(5)) / 2;
        static readonly float CC = 1 - C;

        const float CURL = 1f;  // 0f ?
        const float TENSION = 1f;

        public static Vector2[] SolveOpen(List<Vector2> points, float? dir0 = null, float? dirn = null)
        {
            var n = points.Count - 1;

            var d = new float[n];
            var deltas = new Vector2[n];
            for (var i = 0; i < n; ++i)
            {
                deltas[i] = points[i + 1] - points[i];
                d[i] = deltas[i].Length();
            }

            var psi = new float[n + 1];
            for (var i = 1; i < n; ++i)
            {
                // compute sine and cosine of direction from P[i-1] to P[i]
                // (relative to x-axis)
                var sin = deltas[i - 1].Y / d[i - 1];
                var cos = deltas[i - 1].X / d[i - 1];
                // rotate deltas[i] so that for atan2 the "x-axis" is the
                // previous direction
                var rot = RotateBy(deltas[i], -sin, cos);
                psi[i] = VectorAngle(rot);
            }

            psi[n] = 0;
            // Solve open

            // Start open
            var u = new float[n + 1];
            var v = new float[n + 1];
            if (dir0.HasValue)
            {
                u[0] = 0;
                v[0] = ReduceAngle(dir0.Value - VectorAngle(deltas[0]));
            }
            else
            {
                var a = TENSION;  // 0 +
                var b = TENSION;  // 1 -
                var c = a * a * CURL / (b * b);
                u[0] = ((3 - a) * c + b) / (a * c + 3 - b);
                v[0] = -u[0] * psi[1];
            }

            // Build eqns
            for (var i = 1; i < n; ++i)
            {
                var a0 = TENSION;  // i-1 +
                var a1 = TENSION;  // i +
                var b1 = TENSION;  // i -
                var b2 = TENSION;  // i+1 -

                var A = a0 / (b1 * b1) / d[i - 1];
                var B = (3 - a0) / (b1 * b1) / d[i - 1];
                var C = (3 - b2) / (a1 * a1) / d[i];
                var D = b2 / (a1 * a1) / d[i];

                var t = B - u[i - 1] * A + C;
                u[i] = D / t;
                v[i] = (-B * psi[i] - D * psi[i + 1] - A * v[i - 1]) / t;
            }

            // End open
            var theta = new float[n + 1];
            if (dirn.HasValue)
            {
                theta[n] = ReduceAngle(dirn.Value - VectorAngle(deltas[n - 1]));
            }
            else
            {
                var a = TENSION;  // n-1 +
                var b = TENSION;  // n -
                var c = b * b * CURL / (a * a);
                u[n] = (b * c + 3 - a) / ((3 - b) * c + a);
                theta[n] = v[n - 1] / (u[n - 1] - u[n]);
            }

            for (var i = n - 1; i >= 0; --i)
            {
                theta[i] = v[i] - u[i] * theta[i + 1];
            }

            // Set controls
            var res = new Vector2[2 * n];
            for (var i = 0; i < n; ++i)
            {
                var phi = -psi[i + 1] - theta[i + 1];

                var a = TENSION;  // i +
                var b = TENSION;  // i+1 -

                var sin_theta = MathF.Sin(theta[i]);
                var cos_theta = MathF.Cos(theta[i]);
                var sin_phi = MathF.Sin(phi);
                var cos_phi = MathF.Cos(phi);

                var alpha = A * (sin_theta - sin_phi * B) * (sin_phi - sin_theta * B) * (cos_theta - cos_phi);
                var beta = (1 + CC * cos_theta + C * cos_phi);
                var rho = (2 + alpha) / beta * (a / 3);
                var sigma = (2 - alpha) / beta * (b / 3);

                res[2 * i] = points[i] + RotateBy(deltas[i], sin_theta, cos_theta) * rho;
                res[2 * i + 1] = points[i + 1] - RotateBy(deltas[i], -sin_phi, cos_phi) * sigma;
            }

            return res;
        }

        static float ReduceAngle(float angle)
        {
            if (MathF.Abs(angle) > MathF.PI)
            {
                if (angle > 0)
                    angle -= MathF.PI * 2;
                else
                    angle += MathF.PI * 2;
            }
            return angle;
        }

        public static float VectorAngle(Vector2 vec)
        {
            return MathF.Atan2(vec.Y, vec.X);
        }
    }
}
