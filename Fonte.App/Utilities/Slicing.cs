// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Fonte.Data.Utilities;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;


namespace Fonte.App.Utilities
{
    static class Slicing
    {
        struct SplitIterator
        {
            private readonly Data.Point _splitPoint;

            public IEnumerable<Data.Segment> Segments
            {
                get
                {
                    var path = _splitPoint.Parent;
                    var segments = path.Segments.ToArray();
                    var splitPoint = _splitPoint;
                    var index = path.Segments.Select((value, ix) => (value, ix))
                                             .Where(pair => pair.value.OnCurve == splitPoint)
                                             .Select(pair => pair.ix)
                                             .First();

                    return Sequence.IterAt(segments, index, inclusive: true);
                }
            }

            public SplitIterator(Data.Point splitPoint)
            {
                _splitPoint = splitPoint;
            }
        };

        public static Vector2[] IntersectPaths(Data.Layer layer, Vector2 p0, Vector2 p1)
        {
            var points = new List<Vector2>();

            foreach (var path in layer.Paths)
            {
                foreach (var segment in path.Segments)
                {
                    foreach (var loc in segment.IntersectLine(p0, p1))
                    {
                        points.Add(loc.Item1);
                    }
                }
            }
            return points.ToArray();
        }

        public static void SlicePaths(Data.Layer layer, Vector2 p0, Vector2 p1, bool breakPaths = true)
        {
            var openSplitPoints = new HashSet<Data.Point>();
            var splitPoints = new List<Data.Point>();
            var verbatimPaths = new HashSet<Data.Path>();
            foreach (var path in layer.Paths)
            {
                var isOpen = path.IsOpen;

                var hasCuts = false;
                var skip = 0;
                foreach (var segment in SustainedSegments(path))
                {
                    if (skip > 0)
                    {
                        --skip;
                    }
                    else
                    {
                        var prev = 1f;
                        foreach (var t in segment.IntersectLine(p0, p1)
                                                 .Select(loc => loc.Item2)
                                                 .OrderByDescending(t => t))
                        {
                            var otherSegment = segment.SplitAt(t / prev);
                            foreach (var point in Enumerable.Concat(segment.Points, otherSegment.OffCurves))
                            {
                                point.X = Outline.RoundToGrid(point.X);
                                point.Y = Outline.RoundToGrid(point.Y);
                            }

                            if (!breakPaths)
                            {
                                segment.OnCurve.IsSelected = true;
                            }
                            else if (isOpen)
                            {
                                openSplitPoints.Add(segment.OnCurve);
                            }
                            else
                            {
                                splitPoints.Add(segment.OnCurve);
                            }
                            skip += 1;
                            prev = t;
                        }
                    }
                    hasCuts |= skip > 0;
                }
                if (!hasCuts)
                {
                    verbatimPaths.Add(path);
                }
            }

            if (splitPoints.Count >= 2 || openSplitPoints.Count > 0)
            {
                // Sort segments by distance of the split point from p0 and build graph of pairs
                var jumpsDict = new Dictionary<Data.Point, SplitIterator>();
                {
                    var reference = p1 - p0;
                    var processedPaths = new HashSet<Data.Path>();

                    var sortedPoints = splitPoints.OrderBy(point => (point.ToVector2() - p0).LengthSquared())
                                                  .ToList();
                    if (sortedPoints.Count % 2 > 0)
                    {
                        // TODO: currently we remove the last point, but we could rather find the undesired point
                        openSplitPoints.Add(sortedPoints.Last());
                        sortedPoints.RemoveAt(sortedPoints.Count - 1);
                    }
                    foreach (var (split1, split2) in ByTwo(sortedPoints))
                    {
                        // After our jump, we want to go back to the same side of the slice,
                        // i.e. have from and to running opposite sides of it
                        if (!processedPaths.Contains(split1.Parent) &&
                            !processedPaths.Contains(split2.Parent) && !RunningOppositeSides(split1, split2, reference))
                        {
                            var path = split2.Parent;
                            path.Reverse();

                            // Add split1 and split2 to the set of altered direction paths, to not touch those again later
                            processedPaths.Add(path);
                            processedPaths.Add(split1.Parent);
                        }

                        jumpsDict[split1] = new SplitIterator(split2);
                        jumpsDict[split2] = new SplitIterator(split1);
                    }
                }

                var result = new List<Data.Path>();
                foreach (var path in layer.Paths)
                {
                    if (verbatimPaths.Contains(path))
                    {
                        result.Add(path);
                    }
                    else if (path.IsOpen)
                    {
                        foreach (var segment in path.Segments.AsEnumerable().Reverse())
                        {
                            var onCurve = segment.OnCurve;

                            if (openSplitPoints.Contains(onCurve))
                            {
                                result.Add(BreakPath(path, path.Points.IndexOf(onCurve)));
                            }
                        }
                        result.Add(path);
                    }
                    else
                    {
                        foreach (var segment in path.Segments)
                        {
                            var onCurve = segment.OnCurve;

                            if (jumpsDict.ContainsKey(onCurve))
                            {
                                result.Add(WalkPath(onCurve, jumpsDict));
                            }
                        }
                    }
                }

                layer.Paths.Clear();
                layer.Paths.AddRange(result);
            }
        }

        static int AngleSign(Vector2 u, Vector2 v) => Math.Sign(Ops.AngleBetween(u, v));

        static Data.Path BreakPath(Data.Path path, int index)
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

        static IEnumerable<(T, T)> ByTwo<T>(IEnumerable<T> source)
        {
            using var it = source.GetEnumerator();

            while (it.MoveNext())
            {
                var first = it.Current;
                if (it.MoveNext())
                {
                    yield return (first, it.Current);
                }
            }
        }

        static bool RunningOppositeSides(Data.Point from, Data.Point to, Vector2 reference)
        {
            var fromVector = StartPointFromOnCurve(from).ToVector2() - from.ToVector2();
            var toVector = StartPointFromOnCurve(to).ToVector2() - to.ToVector2();

            return AngleSign(reference, fromVector) != AngleSign(reference, toVector);
        }

        static Data.Point StartPointFromOnCurve(Data.Point point)
        {
            var path = point.Parent;
            var index = path.Points.IndexOf(point);

            return point.Type switch
            {
                Data.PointType.Curve => path.Points[index >= 3 ? index - 3 : index - 3 + path.Points.Count],
                Data.PointType.Line => Sequence.PreviousItem(path.Points, index),
                var type => throw new ArgumentException($"Unexpected point type '{type}'.", nameof(point))
            };
        }

        // This is like path.Segments, but keeps going even if points is altered
        // which we account for in caller
        static IEnumerable<Data.Segment> SustainedSegments(Data.Path path)
        {
            int start = 0, count = 0, ix = 0;

            var points = path.Points;
            while (ix < points.Count)
            {
                var point = points[ix++];

                ++count;
                if (point.Type != Data.PointType.None)
                {
                    yield return new Data.Segment(path, start, count);
                    start += count;
                    count = 0;
                }
            }
        }

        static Data.Path WalkPath(Data.Point startPoint, Dictionary<Data.Point, SplitIterator> jumpsDict,
                                  Data.Path outPath = null, Data.Point jumpFromPoint = null)
        {
            if (outPath == null)
                outPath = new Data.Path();
            if (jumpFromPoint == null)
                jumpFromPoint = startPoint;

            var remSegments = jumpsDict[jumpFromPoint].Segments;
            jumpsDict.Remove(jumpFromPoint);

            var jumpToSegment = remSegments.First();
            {
                var point = jumpToSegment.OnCurve.Clone();
                point.IsSmooth = false;
                point.Type = Data.PointType.Line;
                outPath.Points.Add(point);
            }

            var isTerminalState = false;
            foreach (var segment in remSegments.Skip(1))
            {
                var onCurve = segment.OnCurve;
                var isJump = jumpsDict.ContainsKey(onCurve);
                var isLast = onCurve == startPoint;

                foreach (var point in segment.Points)
                {
                    var outPoint = point.Clone();
                    if ((isJump || isLast) && point.Type != Data.PointType.None)
                    {
                        outPoint.IsSmooth = false;
                    }
                    outPath.Points.Add(outPoint);
                }

                isTerminalState = true;
                if (isLast) break;
                else if (isJump)
                {
                    WalkPath(startPoint, jumpsDict, outPath, onCurve);
                    break;
                }
                else
                {
                    isTerminalState = false;
                }
            }
            Debug.Assert(isTerminalState);

            return outPath;
        }
    }
}
