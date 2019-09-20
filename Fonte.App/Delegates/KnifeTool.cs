/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Fonte.App.Utilities;
    using Fonte.Data.Interfaces;
    using Fonte.Data.Utilities;
    using Microsoft.Graphics.Canvas;

    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
    using Windows.Foundation;
    using Windows.System;
    using Windows.UI;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public class KnifeTool : BaseTool
    {
        private Point? _origin;
        private Point _anchor;
        private Vector2[] _points;

        private IChangeGroup _undoGroup;

        protected override CoreCursor DefaultCursor { get; } = Cursors.Knife;

        public override void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
            if (_points != null)
            {
                var color = Color.FromArgb(120, 38, 38, 38);
                var radius = 3.5f * rescale;

                foreach (var point in _points)
                {
                    ds.FillCircle(point, radius, color);
                }
                ds.DrawLine(_origin.Value.ToVector2(), _anchor.ToVector2(), Color.FromArgb(120, 60, 60, 60), strokeWidth: rescale);
            }
        }

        public override void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerPressed(canvas, args);

            var ptPoint = args.GetCurrentPoint(canvas);
            if (ptPoint.Properties.IsLeftButtonPressed)
            {
                var layer = canvas.Layer;

                _undoGroup = layer.CreateUndoGroup();
                _origin = _anchor = canvas.FromClientPosition(ptPoint.Position);

                layer.ClearSelection();
                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerMoved(canvas, args);

            if (_origin.HasValue)
            {
                _anchor = canvas.FromClientPosition(args.GetCurrentPoint(canvas).Position);
                if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
                {
                    _anchor = ClampToOrigin(_anchor, _origin.Value);
                }

                _points = IntersectPaths(canvas.Layer, _origin.Value.ToVector2(), _anchor.ToVector2());
                canvas.Invalidate();
            }
        }

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerReleased(canvas, args);

            if (_points != null)
            {
                var layer = canvas.Layer;

                layer.ClearSelection();
                SlicePaths(layer, _origin.Value.ToVector2(), _anchor.ToVector2());

                _undoGroup.Dispose();
                _undoGroup = null;
                ((App)Application.Current).InvalidateData();
            }
            _origin = null;
            _points = null;

            canvas.Invalidate();
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

        static Vector2[] IntersectPaths(Data.Layer layer, Vector2 p0, Vector2 p1)
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

        static bool SlicePaths(Data.Layer layer, Vector2 p0, Vector2 p1)
        {
            // TODO: handle open contours
            var pathSegments = new List<Data.Segment[]>();
            var splitSegments = new List<(Data.Segment, IEnumerable<Data.Segment>)>();
            foreach (var path in layer.Paths)
            {
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
                        segment.SplitAt(t);
                        segments = path.Segments.ToArray();

                        splitSegments.Add((
                            segment,
                            Sequence.IterAt(segments, index)
                        ));
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
                layer.Paths.Clear();
                layer.Paths.AddRange(result);
                return true;
            }
            return false;
        }

        #region IToolBarEntry implementation

        public override IconSource Icon { get; } = new FontIconSource() { FontSize = 16, Glyph = "\ue7e6" };

        public override string Name => "Knife";

        public override KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator() { Key = VirtualKey.E };

        #endregion
    }
}
