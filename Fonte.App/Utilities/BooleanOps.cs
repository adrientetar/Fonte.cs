/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Utilities
{
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Geometry;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    public class BooleanOps
    {
        public static List<Data.Path> Exclude(IEnumerable<Data.Path> paths, IEnumerable<Data.Path> otherPaths)
        {
            return Op(paths, otherPaths, CanvasGeometryCombine.Exclude);
        }

        public static List<Data.Path> Intersect(IEnumerable<Data.Path> paths, IEnumerable<Data.Path> otherPaths)
        {
            return Op(paths, otherPaths, CanvasGeometryCombine.Intersect);
        }

        public static List<Data.Path> Union(IEnumerable<Data.Path> paths, IEnumerable<Data.Path> otherPaths = null)
        {
            return Op(paths, otherPaths, CanvasGeometryCombine.Union);
        }

        public static List<Data.Path> Xor(IEnumerable<Data.Path> paths, IEnumerable<Data.Path> otherPaths)
        {
            return Op(paths, otherPaths, CanvasGeometryCombine.Xor);
        }

        static List<Data.Path> Op(IEnumerable<Data.Path> paths, IEnumerable<Data.Path> otherPaths, CanvasGeometryCombine operation)
        {
            using (CanvasGeometry geom1 = MakeGeometry(paths),
                                  geom2 = MakeGeometry(otherPaths))
            {
                using (var result = geom1.CombineWith(geom2, Matrix3x2.Identity, operation, float.Epsilon))
                {
                    var recv = new GeometrySink();
                    result.SendPathTo(recv);

                    return recv.Paths;
                }
            }
        }

        static CanvasGeometry MakeGeometry(IEnumerable<Data.Path> paths)
        {
            var device = CanvasDevice.GetSharedDevice();
            var builder = new CanvasPathBuilder(device);
            if (paths != null)
            {
                foreach (var path in paths)
                {
                    builder.AddGeometry(path.CanvasPath);
                }
            }
            builder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);

            return CanvasGeometry.CreatePath(builder);
        }
    }

    class GeometrySink : ICanvasPathReceiver
    {
        public List<Data.Path> Paths { get; } = new List<Data.Path>();

        Data.Path Path => Paths.Last();

        public void BeginFigure(Vector2 start, CanvasFigureFill figureFill)
        {
            Paths.Add(new Data.Path());
            Path.Points.Add(new Data.Point(
                Outline.RoundToGrid(start.X),
                Outline.RoundToGrid(start.Y),
                Data.PointType.Move));
        }

        public void AddArc(Vector2 endPoint, float radiusX, float radiusY, float rotationAngle, CanvasSweepDirection sweepDirection, CanvasArcSize arcSize)
        {
            throw new NotImplementedException();
        }

        public void AddCubicBezier(Vector2 off1, Vector2 off2, Vector2 to)
        {
            Path.Points.Add(new Data.Point(
                Outline.RoundToGrid(off1.X),
                Outline.RoundToGrid(off1.Y)
                ));
            Path.Points.Add(new Data.Point(
                Outline.RoundToGrid(off2.X),
                Outline.RoundToGrid(off2.Y)
                ));
            Path.Points.Add(new Data.Point(
                Outline.RoundToGrid(to.X),
                Outline.RoundToGrid(to.Y),
                Data.PointType.Curve));
        }

        public void AddLine(Vector2 to)
        {
            var point = new Data.Point(
                Outline.RoundToGrid(to.X),
                Outline.RoundToGrid(to.Y),
                Data.PointType.Line);
            var last = Path.Points.Last();
            if (!(point.X == last.X && point.Y == last.Y))
            {
                Path.Points.Add(point);
            }
        }

        public void AddQuadraticBezier(Vector2 controlPoint, Vector2 endPoint)
        {
            throw new NotImplementedException();
        }

        public void SetFilledRegionDetermination(CanvasFilledRegionDetermination filledRegionDetermination)
        {
        }

        public void SetSegmentOptions(CanvasFigureSegmentOptions figureSegmentOptions)
        {
        }

        public void EndFigure(CanvasFigureLoop figureLoop)
        {
            if (figureLoop == CanvasFigureLoop.Closed)
            {
                if (Path.Points.Count > 1)  // what we really want to check here is non-null area, but that's not important
                {
                    var first = Path.Points.First();
                    var last = Path.Points.Last();

                    if (first.X == last.X && first.Y == last.Y)
                    {
                        Path.Points.RemoveAt(0);
                    }
                }
                if (Path.IsOpen)
                {
                    Path.Close();
                }

                if (Path.Points.Count > 2)
                {
                    var prev = Path.Points[Path.Points.Count - 2];
                    var point = Path.Points[Path.Points.Count - 1];
                    foreach (var next in Path.Points)
                    {
                        if (point.Type != Data.PointType.None &&
                            (prev.Type == Data.PointType.None || next.Type == Data.PointType.None) &&
                            IsFlatAngle(prev, point, next))
                        {
                            point.IsSmooth = true;
                        }

                        prev = point;
                        point = next;
                    }
                }
            }
        }

        static bool IsFlatAngle(Data.Point p0, Data.Point p1, Data.Point p2, float tol = 0.05f)
        {
            var p01 = p1.ToVector2() - p0.ToVector2();
            var p12 = p2.ToVector2() - p1.ToVector2();

            return Math.Abs(Math.Atan2(p01.Y, p01.X) - Math.Atan2(p12.Y, p12.X)) <= tol;
        }
    }
}