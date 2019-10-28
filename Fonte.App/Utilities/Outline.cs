// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Utilities
{
    using Fonte.Data.Utilities;
    using PointType = Data.PointType;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Diagnostics;

    public enum MoveMode
    {
        Normal = 0,
        // Maintain control points % position
        InterpolateCurve,
        // Don't move control points across handles
        StaticHandles
    };

    class Outline
    {
        public static void BreakPath(Data.Path path, int index)
        {
            var points = path.Points;
            var point = points[index];
            if (point.Type == PointType.None)
            {
                throw new InvalidOperationException($"Index {index} isn't at segment boundary");
            }
            point.IsSmooth = false;

            var after = points.GetRange(index, points.Count - index);
            if (path.IsOpen)
            {
                points.RemoveRange(index, points.Count - index);
                var newPath = new Data.Path();
                path.Parent.Paths.Add(newPath);
                newPath.Points.AddRange(after);
            }
            else
            {
                var before = points.GetRange(0, index);
                points.Clear();
                points.AddRange(after);
                points.AddRange(before);
            }
            points.Add(point.Clone());
            point.Type = PointType.Move;
        }

        public static void DeleteSelection(Data.Layer layer, bool breakPaths = false)
        {
            using var group = layer.CreateUndoGroup();

            for (int ix = layer.Anchors.Count - 1; ix >= 0; --ix)
            {
                var anchor = layer.Anchors[ix];
                if (anchor.IsSelected)
                {
                    layer.Anchors.RemoveAt(ix);
                }
            }

            for (int ix = layer.Components.Count - 1; ix >= 0; --ix)
            {
                var component = layer.Components[ix];
                if (component.IsSelected)
                {
                    layer.Components.RemoveAt(ix);
                }
            }

            if (layer.Master is Data.Master master)
            {
                for (int ix = master.Guidelines.Count - 1; ix >= 0; --ix)
                {
                    var guideline = master.Guidelines[ix];
                    if (guideline.IsSelected)
                    {
                        master.Guidelines.RemoveAt(ix);
                    }
                }
            }
            for (int ix = layer.Guidelines.Count - 1; ix >= 0; --ix)
            {
                var guideline = layer.Guidelines[ix];
                if (guideline.IsSelected)
                {
                    layer.Guidelines.RemoveAt(ix);
                }
            }

            if (breakPaths)
            {
                BreakPathsSelection(layer);
            }
            else
            {
                DeletePathsSelection(layer);
            }
        }

        public static void JoinPaths(Data.Path path, bool atStart, Data.Path otherPath, bool atOtherStart,
                                     bool mergeJoin = false)
        {
            if (path == otherPath)
            {
                if (atStart == atOtherStart)
                    throw new InvalidOperationException("Invalid same-point join");

                path.Close();
                if (mergeJoin)
                {
                    var duplPoint = path.Points.Pop();
                    // Here lastPoint might not have precisely the same position as the dupl. point we just pruned
                    var lastPoint = path.Points.Last();
                    lastPoint.X = duplPoint.X;
                    lastPoint.Y = duplPoint.Y;
                }
            }
            else
            {
                if (atStart)
                {
                    path.Reverse();
                }
                if (!atOtherStart)
                {
                    otherPath.Reverse();
                }

                var points = path.Points;
                var type = mergeJoin switch
                {
                    true => points.Pop().Type,
                    _    => PointType.Line
                };

                var layer = path.Parent;
                layer.ClearSelection();
                var otherPoints = otherPath.Points.ToList();
                otherPath.Points.Clear();
                layer.Paths.Remove(otherPath);
                points.AddRange(otherPoints);
                var otherFirstPoint = otherPoints[0];
                otherFirstPoint.Type = type;
                otherFirstPoint.IsSelected = true;
            }
        }

        public static void MoveSelection(Data.Layer layer, float dx, float dy, MoveMode mode = MoveMode.Normal)
        {
            using var group = layer.CreateUndoGroup();

            foreach (var anchor in layer.Anchors)
            {
                if (anchor.IsSelected)
                {
                    anchor.X = RoundToGrid(anchor.X + dx);
                    anchor.Y = RoundToGrid(anchor.Y + dy);
                }
            }
            foreach (var component in layer.Components)
            {
                if (component.IsSelected)
                {
                    var t = component.Transformation;
                    t.M31 = RoundToGrid(t.M31 + dx);
                    t.M32 = RoundToGrid(t.M32 + dy);
                    component.Transformation = t;
                }
            }
            foreach (var guideline in UIBroker.GetAllGuidelines(layer))
            {
                if (guideline.IsSelected)
                {
                    guideline.X = RoundToGrid(guideline.X + dx);
                    guideline.Y = RoundToGrid(guideline.Y + dy);
                }
            }
            foreach (var path in layer.Paths)
            {
                MoveSelection(path, dx, dy, mode);
            }
        }

        /*public*/ static void MoveSelection(Data.Path path, float dx, float dy, MoveMode mode = MoveMode.Normal)
        {
            if (path.Points.Count < 2)
            {
                var lonePoint = path.Points.First();
                if (lonePoint.IsSelected)
                {
                    lonePoint.X = RoundToGrid(lonePoint.X + dx);
                    lonePoint.Y = RoundToGrid(lonePoint.Y + dy);
                }
                return;
            }

            // First pass: move
            var prev = path.Points[path.Points.Count - 2];
            var point = path.Points.Last();
            foreach (var next in path.Points)
            {
                if (point.IsSelected)
                {
                    point.X = RoundToGrid(point.X + dx);
                    point.Y = RoundToGrid(point.Y + dy);

                    if (point.Type != PointType.None && mode != MoveMode.StaticHandles)
                    {
                        if (!prev.IsSelected && prev.Type == PointType.None && point.Type != PointType.Move)
                        {
                            prev.X = RoundToGrid(prev.X + dx);
                            prev.Y = RoundToGrid(prev.Y + dy);
                        }
                        if (!next.IsSelected && next.Type == PointType.None)
                        {
                            next.X = RoundToGrid(next.X + dx);
                            next.Y = RoundToGrid(next.Y + dy);
                        }
                    }
                }
                prev = point;
                point = next;
            }
            if (path.Points.Count < 3)
            {
                return;
            }

            // Second pass: project
            var prevOn = point;
            foreach (var next in path.Points)
            {
                var atNode = point.Type != PointType.None &&
                             (prev.Type == PointType.None || next.Type == PointType.None);

                if (mode == MoveMode.InterpolateCurve)
                {
                    if (next.Type == PointType.Curve)
                    {
                        InterpolateCurve(prevOn, prev, point, next, dx, dy);
                    }
                }
                else if (mode == MoveMode.StaticHandles)
                {
                    if (atNode)
                    {
                        ConstrainStaticHandles(prev, point, next, dx, dy);
                    }
                }
                else
                {
                    if (atNode && point.Type != PointType.Move && point.IsSmooth)
                    {
                        ConstrainSmoothPoint(prev, point, next, mode != MoveMode.StaticHandles);
                    }
                }

                if (point.Type != PointType.None)
                {
                    prevOn = point;
                }
                prev = point;
                point = next;
            }
        }

        public static void StretchCurve(Data.Layer layer, IList<Data.Point> curve, float dx, float dy, bool maintainDirection = false)
        {
            Debug.Assert(curve.Count == 4);

            var leftVector = curve[1].ToVector2();
            var rightVector = curve[2].ToVector2();
            using var group = layer.CreateUndoGroup();

            curve[1].X = curve[1].X + dx;
            curve[1].Y = curve[1].Y + dy;
            if (maintainDirection || curve[0].IsSmooth)
            {
                VectorProjection(curve[1], curve[0].ToVector2(), leftVector);
            }
            else
            {
                curve[1].X = RoundToGrid(curve[1].X);
                curve[1].Y = RoundToGrid(curve[1].Y);
            }

            curve[2].X = curve[2].X + dx;
            curve[2].Y = curve[2].Y + dy;
            if (maintainDirection || curve[3].IsSmooth)
            {
                VectorProjection(curve[2], curve[3].ToVector2(), rightVector);
            }
            else
            {
                curve[2].X = RoundToGrid(curve[2].X);
                curve[2].Y = RoundToGrid(curve[2].Y);
            }
        }

        public static void RoundSelection(Data.Layer layer)
        {
            using var group = layer.CreateUndoGroup();

            foreach (var anchor in layer.Anchors)
            {
                if (anchor.IsSelected)
                {
                    anchor.X = RoundToGrid(anchor.X);
                    anchor.Y = RoundToGrid(anchor.Y);
                }
            }
            foreach (var component in layer.Components)
            {
                if (component.IsSelected)
                {
                    var t = component.Transformation;
                    // TODO: could round scale to 2 decimal digits, like we do when transforming
                    // worth having a round to digits (default = 2) method here?
                    t.M31 = RoundToGrid(t.M31);
                    t.M32 = RoundToGrid(t.M32);
                    component.Transformation = t;
                }
            }
            foreach (var guideline in UIBroker.GetAllGuidelines(layer))
            {
                if (guideline.IsSelected)
                {
                    // TODO: introduce some angle rounding?
                    guideline.X = RoundToGrid(guideline.X);
                    guideline.Y = RoundToGrid(guideline.Y);
                }
            }
            foreach (var path in layer.Paths)
            {
                foreach (var point in path.Points)
                {
                    if (point.IsSelected)
                    {
                        point.X = RoundToGrid(point.X);
                        point.Y = RoundToGrid(point.Y);
                    }
                }
            }
        }

        public static float RoundToGrid(float value)
        {
            // TODO: get rounding info from the app, cache as static value
            return MathF.Round(value);
        }

        public static bool TryJoinPath(Data.Layer layer, Data.Point point)
        {
            if (Is.AtOpenBoundary(point) && layer.Paths
                                                 .SelectMany(path => path.Points)
                                                 .Where(p => p != point && p.X == point.X && p.Y == point.Y)
                                                 .LastOrDefault() is Data.Point otherPoint)
            {
                return TryJoinPath(layer, point, otherPoint);
            }
            return false;
        }
        public static bool TryJoinPath(Data.Layer layer, Data.Point point, Data.Point otherPoint)
        {
            if (Is.AtOpenBoundary(point) && Is.AtOpenBoundary(otherPoint))
            {
                JoinPaths(point.Parent,
                          point.Parent.Points.IndexOf(point) == 0,
                          otherPoint.Parent,
                          otherPoint.Parent.Points.IndexOf(otherPoint) == 0,
                          true);
                return true;
            }
            return false;
        }

        public static bool TryTogglePointSmoothness(Data.Path path, int index)
        {
            var point = path.Points[index];
            if (point.Type != PointType.None)
            {
                var value = !point.IsSmooth;
                if (value)
                {
                    if (Is.AtOpenBoundary(point))
                    {
                        return false;
                    }

                    var before = Sequence.PreviousItem(path.Points, index);
                    var after = Sequence.NextItem(path.Points, index);
                    if (before.Type != PointType.None && after.Type != PointType.None)
                    {
                        return false;
                    }
                    else if (before.Type != PointType.None)
                    {
                        VectorRotation(after, before.ToVector2(), point.ToVector2());
                    }
                    else if (after.Type != PointType.None)
                    {
                        VectorRotation(before, after.ToVector2(), point.ToVector2());
                    }
                }

                point.IsSmooth = !point.IsSmooth;
                return true;
            }
            return false;
        }
        public static bool TryTogglePointSmoothness(Data.Point point)
        {
            var path = point.Parent;
            return TryTogglePointSmoothness(path, path.Points.IndexOf(point));
        }

        /**/

        static bool AnyOffCurveSelected(Data.Segment segment)
        {
            return Enumerable.Any(segment.OffCurves, point => point.IsSelected);
        }

        static void BreakPathsSelection(Data.Layer layer)
        {
            var outPaths = new List<Data.Path>();
            foreach (var path in Selection.FilterSelection(layer.Paths, invert: true))
            {
                var segmentsList = new List<Data.Segment>(path.Segments);
                IEnumerable<Data.Segment> iter;
                if (!(path.IsOpen ||
                      AnyOffCurveSelected(segmentsList.First())
                      ))
                {
                    int index;
                    for (index = segmentsList.Count - 1; index >= 0; --index)
                    {
                        if (AnyOffCurveSelected(segmentsList[index]))
                        {
                            break;
                        }
                    }
                    if (index <= 0)
                    {
                        // None selected, bring on the original path
                        outPaths.Add(path);
                        continue;
                    }
                    else
                    {
                        iter = Sequence.IterAt(segmentsList, index);
                    }
                }
                else
                {
                    iter = segmentsList;
                }

                Data.Path outPath = null;
                foreach (var segment in iter)
                {
                    if (AnyOffCurveSelected(segment) || segment.OnCurve.Type == PointType.Move)
                    {
                        if (outPath != null)
                        {
                            outPaths.Add(outPath);
                        }
                        outPath = new Data.Path();

                        var point = segment.OnCurve.Clone();
                        point.IsSmooth = false;
                        point.Type = PointType.Move;
                        outPath.Points.Add(point);
                    }
                    else
                    {
                        outPath.Points.AddRange(segment.Points
                                                       .Select(p => p.Clone())
                                                       .ToList());
                    }
                }
                outPaths.Add(outPath);
            }

            layer.Paths.Clear();
            layer.Paths.AddRange(outPaths);
        }

        static void DeletePathsSelection(Data.Layer layer)
        {
            if (!TryReconstructCurve(layer))
            {
                var outPaths = new List<Data.Path>();
                foreach (var path in layer.Paths)
                {
                    var segments = path.Segments.ToList();

                    var forwardMove = false;
                    for (int ix = segments.Count - 1; ix >= 0; --ix)
                    {
                        var segment = segments[ix];
                        var onCurve = segment.OnCurve;
                        if (onCurve.IsSelected)
                        {
                            forwardMove = ix == 0 && onCurve.Type == PointType.Move;

                            segment.Remove(nodeBias: true);
                        }
                        else if (AnyOffCurveSelected(segment))
                        {
                            segment.ConvertTo(PointType.Line);
                        }
                    }

                    if (path.Points.Count > 0)
                    {
                        if (forwardMove)
                        {
                            segments[0].ConvertTo(PointType.Move);
                        }
                        outPaths.Add(path);
                    }
                }

                layer.Paths.Clear();
                layer.Paths.AddRange(outPaths);
            }
        }

        static Vector2 SamplePoints(List<Vector2> samples, IList<Vector2> points, bool atStart)
        {
            var n = 20;  // TODO: adaptive sample count
            var start = atStart ? 0 : 1;

            if (points.Count == 4)
            {
                for (int i = start; i < n; ++i)
                {
                    samples.Add(BezierMath.Q(points, (float)i / (n - 1)));
                }
            }
            else
            {
                Debug.Assert(points.Count == 2);

                if (atStart) samples.Add(points[0]);
                samples.Add(points[1]);
            }

            return Vector2.Normalize(atStart ? points[1] - points[0] : points[points.Count - 2] - points[points.Count - 1]);
        }

        static bool TryReconstructCurve(Data.Layer layer)
        {
            var selection = layer.Selection;
            if (selection.Count == 1 &&
                layer.Selection.First() is Data.Point point)
            {
                var path = point.Parent;
                var segments = path.Segments.ToList();
                Data.Segment firstSegment = default;
                Data.Segment secondSegment = default;

                var segment = segments.Last();
                foreach (var next in segments)
                {
                    var onCurve = segment.OnCurve;
                    if (onCurve.IsSelected)
                    {
                        if (!(path.IsOpen && path.Points.Last() == onCurve) &&
                            onCurve.Type == PointType.Curve || next.OnCurve.Type == PointType.Curve)
                        {
                            firstSegment = segment;
                            secondSegment = next;
                        }
                    }

                    segment = next;
                }

                if (!Equals(firstSegment, secondSegment))
                {
                    var samples = new List<Vector2>();

                    var leftTangent = SamplePoints(samples, firstSegment.PointsInclusive
                                                                        .Select(p => p.ToVector2())
                                                                        .ToArray(), true);
                    var rightTangent = SamplePoints(samples, secondSegment.PointsInclusive
                                                                          .Select(p => p.ToVector2())
                                                                          .ToArray(), false);
                    var fitPoints = BezierMath.FitCubic(samples, leftTangent, rightTangent, .01f);

                    layer.ClearSelection();
                    var curveSegment = firstSegment;
                    var otherSegment = secondSegment;
                    if (curveSegment.OnCurve.Type != PointType.Curve)
                    {
                        (curveSegment, otherSegment) = (otherSegment, curveSegment);
                    }
                    var offCurves = curveSegment.OffCurves;
                    offCurves[0].X = RoundToGrid(fitPoints[1].X);
                    offCurves[0].Y = RoundToGrid(fitPoints[1].Y);
                    offCurves[1].X = RoundToGrid(fitPoints[2].X);
                    offCurves[1].Y = RoundToGrid(fitPoints[2].Y);
                    var onCurve = firstSegment.OnCurve;
                    onCurve.X = secondSegment.OnCurve.X;
                    onCurve.Y = secondSegment.OnCurve.Y;
                    otherSegment.Remove();
                    return true;
                }
            }
            return false;
        }

        /**/

        static void ConstrainSmoothPoint(Data.Point p1, Data.Point p2, Data.Point p3, bool handleMovement)
        {
            if (p2.IsSelected)
            {
                if (p1.Type == PointType.None)
                {
                    (p1, p3) = (p3, p1);
                }
                if (p1.Type != PointType.None)
                {
                    VectorRotation(p3, p1.ToVector2(), p2.ToVector2());
                }
            }
            else if (p1.IsSelected != p3.IsSelected)
            {
                if (p1.IsSelected)
                {
                    (p1, p3) = (p3, p1);
                }
                if (p1.Type != PointType.None)
                {
                    VectorProjection(p3, p1.ToVector2(), p2.ToVector2());
                }
                else if (handleMovement)
                {
                    VectorRotation(p1, p3.ToVector2(), p2.ToVector2());
                }
            }
        }

        static void ConstrainStaticHandles(Data.Point p1, Data.Point p2, Data.Point p3, float dx, float dy)
        {
            if (p2.IsSelected && p2.Type != PointType.Move)
            {
                if (p1.IsSelected || p3.IsSelected)
                {
                    if (p1.IsSelected)
                    {
                        (p1, p3) = (p3, p1);
                    }
                    if (!p1.IsSelected && p3.Type == PointType.None)
                    {
                        VectorRotation(p3, p1.ToVector2(), p2.ToVector2());
                    }
                }
                else
                {
                    if (p2.IsSmooth)
                    {
                        VectorProjection(p2, p1.ToVector2(), p3.ToVector2());
                    }
                }
            }
            else if (p1.IsSelected != p3.IsSelected)
            {
                if (p1.IsSelected)
                {
                    (p1, p3) = (p3, p1);
                }
                if (p3.Type == PointType.None)
                {
                    if (p2.IsSmooth && p2.Type != PointType.Move)
                    {
                        VectorProjection(p3, p1.ToVector2(), p2.ToVector2());
                    }
                    else
                    {
                        // XXX: now that we're rounding, this doesn't work so well anymore
                        var rvec = new Vector2(p3.X - dx, p3.Y - dy);
                        VectorProjection(p3, p2.ToVector2(), rvec);
                    }
                }
            }
        }

        static void InterpolateCurve(Data.Point on1, Data.Point off1, Data.Point off2, Data.Point on2, float dx, float dy)
        {
            if (on2.IsSelected != on1.IsSelected)
            {
                var sign = on1.IsSelected ? -1 : 1;
                var sdelta = new Vector2(sign * dx, sign * dy);

                var ondelta = on2.ToVector2() - on1.ToVector2();
                var factor = ondelta - sdelta;
                if (factor.X != 0 && factor.Y != 0)
                {
                    factor = ondelta / factor;
                }

                if (!off1.IsSelected)
                {
                    off1.X = RoundToGrid(on1.X + factor.X * (off1.X - on1.X));
                    off1.Y = RoundToGrid(on1.Y + factor.Y * (off1.Y - on1.Y));
                }
                if (!off2.IsSelected)
                {
                    off2.X = RoundToGrid(on1.X + factor.X * (off2.X - on1.X - sdelta.X));
                    off2.Y = RoundToGrid(on1.Y + factor.Y * (off2.Y - on1.Y - sdelta.Y));
                }
            }
        }

        static void VectorProjection(Data.Point point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var l2 = ab.LengthSquared();
            if (l2 != 0)
            {
                var ap = point.ToVector2() - a;
                var t = Vector2.Dot(ap, ab) / l2;

                point.X = RoundToGrid(a.X + t * ab.X);
                point.Y = RoundToGrid(a.Y + t * ab.Y);
            }
            else
            {
                point.X = RoundToGrid(a.X);
                point.Y = RoundToGrid(a.Y);
            }
        }

        static void VectorRotation(Data.Point point, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var ab_len = ab.Length();
            if (ab_len != 0)
            {
                var p = point.ToVector2();
                var pb_len = (b - p).Length();
                var t = (ab_len + pb_len) / ab_len;

                point.X = RoundToGrid(a.X + t * ab.X);
                point.Y = RoundToGrid(a.Y + t * ab.Y);
            }
        }
    }
}
