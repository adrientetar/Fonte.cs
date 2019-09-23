/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Utilities
{
    using Fonte.Data.Utilities;

    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;

    class Slicing
    {
        public static bool SlicePaths(Data.Layer layer, Vector2 p0, Vector2 p1)
        {
            // TODO: handle open contours
            var pathSegments = new List<Data.Segment[]>();
            var openSplitSegments = new HashSet<Data.Segment>();
            var splitSegments = new List<(Data.Segment, IEnumerable<Data.Segment>)>();
            foreach (var path in layer.Paths)
            {
                var isOpen = path.IsOpen;
                var segments = path.Segments.ToArray();
                var index = 0;
                while (index < segments.Length)
                {
                    var segment = segments[index];
                    var intersections = segment.IntersectLine(p0, p1);
                    if (intersections.Length > 0)
                    {
                        // TODO: handle more intersections
                        var (_, t) = intersections.First();
                        var otherSegment = segment.SplitAt(t);
                        foreach (var point in Enumerable.Concat(segment.Points, otherSegment.OffCurves))
                        {
                            point.X = Outline.RoundToGrid(point.X);
                            point.Y = Outline.RoundToGrid(point.Y);
                        }

                        segments = path.Segments.ToArray();
                        if (isOpen)
                        {
                            openSplitSegments.Add(segment);
                        }
                        else
                        {
                            splitSegments.Add((
                                segment,
                                Sequence.IterAt(segments, index)
                            ));
                        }
                        index += 2;
                    }
                    else
                    {
                        index += 1;
                    }
                }
                pathSegments.Add(segments);
            }

            var size = splitSegments.Count;
            if (size >= 2)
            {
                // TODO: use black/white area for odd len elision
                var segmentsDict = new Dictionary<Data.Segment, IEnumerable<Data.Segment>>();
                // Sort segments by distance of the split point from p0 and build graph of pairs
                foreach (var (split1, split2) in ByTwo(splitSegments.OrderBy(
                                                           item =>
                                                           {
                                                               var seg = item.Item1;
                                                               var d = seg.OnCurve.ToVector2() - p0;
                                                               return d.LengthSquared();
                                                           })))
                {
                    segmentsDict[split1.Item1] = split2.Item2;
                    segmentsDict[split2.Item1] = split1.Item2;
                }

                var result = new List<Data.Path>();
                foreach (var (path, segments) in layer.Paths.Zip(pathSegments, (p, ss) => (p, ss)))
                {
                    if (path.IsOpen)
                    {
                        foreach (var segment in segments.AsEnumerable().Reverse())
                        {
                            if (openSplitSegments.Contains(segment))
                            {
                                result.Add(SplitPath(path, path.Points.IndexOf(segment.OnCurve)));
                            }
                        }
                        result.Add(path);
                    }
                    else
                    {
                        var hasCut = false;
                        foreach (var segment in segments)
                        {
                            if (segmentsDict.ContainsKey(segment))
                            {
                                result.Add(MakePath(segment, segmentsDict));
                                hasCut = true;
                            }
                        }
                        if (!hasCut)
                        {
                            result.Add(path);
                        }
                    }
                }

                layer.Paths.Clear();
                layer.Paths.AddRange(result);
            }
            else if (size == 1)
            {
                var segment = splitSegments[0].Item1;
                var path = segment.Parent;
                Outline.BreakPath(path, path.Points.IndexOf(segment.OnCurve));
            }
            else
            {
                return false;
            }
            return true;
        }

        static IEnumerable<(T, T)> ByTwo<T>(IEnumerable<T> source)
        {
            using (var it = source.GetEnumerator())
            {
                while (it.MoveNext())
                {
                    var first = it.Current;
                    if (it.MoveNext())
                    {
                        yield return (first, it.Current);
                    }
                }
            }
        }

        static Data.Path MakePath(Data.Segment endSegment, Dictionary<Data.Segment, IEnumerable<Data.Segment>> segmentsDict, Data.Path path = null, Data.Segment? targetSegment = null)
        {
            if (path == null)
                path = new Data.Path();
            if (targetSegment == null)
                targetSegment = endSegment;

            var remSegments = segmentsDict[targetSegment.Value];
            segmentsDict.Remove(targetSegment.Value);

            var jumpToSegment = remSegments.First();
            {
                var point = jumpToSegment.OnCurve.Clone();
                point.IsSmooth = false;
                point.Type = Data.PointType.Line;
                path.Points.Add(point);
            }

            foreach (var segment in remSegments.Skip(1))
            {
                var isJump = segmentsDict.ContainsKey(segment);
                var isLast = Equals(segment, endSegment);

                foreach (var point in segment.Points)
                {
                    var outPoint = point.Clone();
                    if ((isJump || isLast) && point.Type != Data.PointType.None)
                    {
                        outPoint.IsSmooth = false;
                    }
                    path.Points.Add(outPoint);
                }
                if (isLast) break;
                if (isJump)
                {
                    MakePath(endSegment, segmentsDict, path, segment);
                    break;
                }
            }

            return path;
        }

        static Data.Path SplitPath(Data.Path path, int index)
        {
            var points = path.Points;
            var point = points[index];
            point.IsSmooth = false;

            var after = points.GetRange(index, points.Count - index)
                              .Select(p => p.Clone())
                              .ToList();
            points.RemoveRange(index, points.Count - index);
            points.Add(point.Clone());
            after[0].Type = Data.PointType.Move;

            return new Data.Path(points: after);
        }
    }
}
