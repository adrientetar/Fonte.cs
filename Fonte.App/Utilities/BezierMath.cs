/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Windows.Foundation;

    class BezierMath
    {
        public static ValueTuple<Point, Point> BezierApproximation(List<Point> points, Point p0, Point p3, double maxSqDist = 1)
        {
            if (maxSqDist < 0)
                throw new ArgumentOutOfRangeException("Max. allowed square distance should be positive");
            if (points.Count < 2)
                throw new ArgumentException("At least two intermediate points are required");

            var ts = CentripetalParams(points, p0, p3);
            var (p1, p2) = FindBezierControlPoints(points, p0, p3, ts);

            var splPoints = BezierInterp(p0, p1, p2, p3, ts);
            var (sqX, sqY, sqI) = MaxSqDist(points, splPoints);

            var sqDist = Math.Max(sqX, sqY);

            var maxIterations = 20;
            while (sqDist > maxSqDist && maxIterations > 0)
            {
                ts = Reparametrize(points, p0, p1, p2, p3, ts);
                var (p1n, p2n) = FindBezierControlPoints(points, p0, p3, ts);

                splPoints = BezierInterp(p0, p1n, p2n, p3, ts);
                (sqX, sqY, sqI) = MaxSqDist(points, splPoints);

                var sqDistn = Math.Max(sqX, sqY);
                if (sqDistn >= sqDist) break;
                
                --maxIterations;
                (p1, p2) = (p1n, p2n);
                sqDist = sqDistn;
            }

            return (p1, p2);
        }

        /**/

        static List<Point> BezierInterp(Point p0, Point p1, Point p2, Point p3, List<double> ts)
        {
            var pts = new List<Point>(ts.Count);

            var c3x = -p0.X + 3 * (p1.X - p2.X) + p3.X;
            var c3y = -p0.Y + 3 * (p1.Y - p2.Y) + p3.Y;
            var c2x = 3 * (p0.X - (2 * p1.X) + p2.X);
            var c2y = 3 * (p0.Y - (2 * p1.Y) + p2.Y);
            var c1x = 3 * (p1.X - p0.X);
            var c1y = 3 * (p1.Y - p0.Y);
            var c0x = p0.X;
            var c0y = p0.Y;

            foreach (var t in ts)
            {
                pts.Add(
                    new Point(
                        ((c3x * t + c2x) * t + c1x) * t + c0x,
                        ((c3y * t + c2y) * t + c1y) * t + c0y
                    ));
            }

            return pts;
        }

        static List<double> CentripetalParams(List<Point> points, Point p0, Point p3)
        {
            var result = new List<double>();
            var sum = 0d;

            var p1 = p0;
            foreach (var p2 in points)
            {
                var dx = p2.X - p1.X;
                var dy = p2.Y - p1.Y;

                sum += Math.Pow(dx * dx + dy * dy, .25);
                result.Add(sum);
            }
            {
                var p2 = points[points.Count - 1];
                var dx = p3.X - p2.X;
                var dy = p3.Y - p2.Y;
                sum += Math.Pow(dx * dx + dy * dy, .25);
            }

            return result.Select(v => v / sum).ToList();
        }

        static Point BezierAt(Point p0, Point p1, Point p2, Point p3, double t)
        {
            var b0 = Math.Pow(1 - t, 3);
            var b1 = 3 * t * Math.Pow(1 - t, 2);
            var b2 = 3 * Math.Pow(t, 2) * (1 - t);
            var b3 = Math.Pow(t, 3);

            return new Point(
                p0.X * b0 + p1.X * b1 + p2.X * b2 + p3.X * b3,
                p0.Y * b0 + p1.Y * b1 + p2.Y * b2 + p3.Y * b3
            );
        }

        static Point BezierPrimeAt(Point p0, Point p1, Point p2, Point p3, double t)
        {
            var b0 = 3 * Math.Pow(1 - t, 2);
            var b1 = 6 * t * (1 - t);
            var b2 = 3 * Math.Pow(t, 2);

            return new Point(
                (p1.X - p0.X) * b0 + (p2.X - p1.X) * b1 + (p3.X - p2.X) * b2,
                (p1.Y - p0.Y) * b0 + (p2.Y - p1.Y) * b1 + (p3.Y - p2.Y) * b2
            );
        }

        static Point BezierPrimePrimeAt(Point p0, Point p1, Point p2, Point p3, double t)
        {
            var b0 = 6 * (1 - t);
            var b1 = 6 * t;

            return new Point(
                (p2.X - (2 * p1.X) + p0.X) * b0 + (p3.X - (2 * p2.X) + p1.X) * b1,
                (p2.Y - (2 * p1.Y) + p0.Y) * b0 + (p3.Y - (2 * p2.Y) + p1.Y) * b1
            );
        }

        static ValueTuple<Point, Point> FindBezierControlPoints(List<Point> points, Point p0, Point p3, List<double> ts)
        {
            Point p1, p2;

            double a1 = 0, a2 = 0, a12 = 0, c1x = 0, c1y = 0, c2x = 0, c2y = 0;
            for (int i = 0; i < points.Count; ++i)
            {
                var t = ts[i];

                var b0 = Math.Pow(1 - t, 3);
                var b1 = 3 * t * Math.Pow(1 - t, 2);
                var b2 = 3 * Math.Pow(t, 2) * (1 - t);
                var b3 = Math.Pow(t, 3);

                a1 += Math.Pow(b1, 2);
                a2 += Math.Pow(b2, 2);
                a12 += b1 * b2;

                c1x += b1 * (points[i].X - b0 * p0.X - b3 * p3.X);
                c1y += b1 * (points[i].Y - b0 * p0.Y - b3 * p3.Y);

                c2x += b2 * (points[i].X - b0 * p0.X - b3 * p3.X);
                c2y += b2 * (points[i].Y - b0 * p0.Y - b3 * p3.Y);
            }

            var den = a1 * a2 - a12 * a12;
            if (den == 0)
            {
                p1 = p0;
                p2 = p3;
            }
            else
            {
                p1 = new Point(
                    (a2 * c1x - a12 * c2x) / den,
                    (a2 * c1y - a12 * c2y) / den
                );
                p2 = new Point(
                    (a1 * c2x - a12 * c1x) / den,
                    (a1 * c2y - a12 * c1y) / den
                );
            }

            return (p1, p2);
        }

        static ValueTuple<double, double, int> MaxSqDist(List<Point> pts1, List<Point> pts2)
        {
            if (pts1.Count != pts2.Count)
                throw new ArgumentException("Points must be of equal dimension");

            var sqMaxx = Math.Pow(pts1[0].X - pts2[0].X, 2);
            var sqMaxy = Math.Pow(pts1[0].Y - pts2[0].Y, 2);
            var ix = 0;
            for (int i = 0; i < pts1.Count; ++i)
            {
                var sx = Math.Pow(pts1[i].X - pts2[i].X, 2);
                var sy = Math.Pow(pts1[i].Y - pts2[i].Y, 2);
                if (sx > sqMaxx && sy > sqMaxy)
                {
                    sqMaxx = sx;
                    sqMaxy = sy;
                    ix = i;
                }
            }

            return (sqMaxx, sqMaxy, ix);
        }

        static List<double> Reparametrize(List<Point> points, Point p0, Point p1, Point p2, Point p3, List<double> ts)
        {
            var result = new List<double>();

            for (int i = 0; i < points.Count; ++i)
            {
                var t = ts[i];
                var point = points[i];

                var p = BezierAt(p0, p1, p2, p3, t);
                var pp = BezierPrimeAt(p0, p1, p2, p3, t);
                var ppp = BezierPrimePrimeAt(p0, p1, p2, p3, t);

                var num = (p.X - point.X) * pp.X + (p.Y - point.Y) * pp.Y;
                var den = Math.Pow(pp.X, 2) + (p.X - point.X) * ppp.X +
                          Math.Pow(pp.Y, 2) + (p.Y - point.Y) * ppp.Y;

                result.Add(den == 0 ? t : t - num / den);
            }
            return result;
        }
    }
}
