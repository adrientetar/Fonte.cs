﻿/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Utilities
{
    using Fonte.Data.Geometry;
    using Fonte.Data.Interfaces;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
    using Point = Windows.Foundation.Point;

    public class SnapResult
    {
        public Vector2 Position { get; }
        public Data.Point NearPoint { get; }
        public (float, float) XSnapLine { get; }
        public (float, float) YSnapLine { get; }

        public bool IsVisible { get; private set; } = true;

        public SnapResult(Vector2 pos, Data.Point nearPoint, (float, float) xSnapLine, (float, float) ySnapLine)
        {
            Position = pos;
            NearPoint = nearPoint;
            XSnapLine = xSnapLine;
            YSnapLine = ySnapLine;
        }

        public void Hide()
        {
            IsVisible = false;
        }

        public IEnumerable<(Vector2, Vector2)> GetSnapLines()
        {
            if (IsVisible)
            {
                if (!float.IsNaN(XSnapLine.Item1))
                {
                    var i2 = !float.IsNaN(XSnapLine.Item2) ? XSnapLine.Item2 : Position.Y;
                    yield return (
                        new Vector2(Position.X, XSnapLine.Item1),
                        new Vector2(Position.X, i2)
                    );
                }
                if (!float.IsNaN(YSnapLine.Item1))
                {
                    var i2 = !float.IsNaN(YSnapLine.Item2) ? YSnapLine.Item2 : Position.X;
                    yield return (
                        new Vector2(YSnapLine.Item1, Position.Y),
                        new Vector2(i2, Position.Y)
                    );
                }
            }
        }
    }

    public class UIBroker
    {
        public struct BBoxHandle
        {
            public HandleKind Kind { get; }
            public Vector2 Position { get; }

            public BBoxHandle(HandleKind kind, Vector2 pos)
            {
                Kind = kind;
                Position = pos;
            }
        }

        [Flags]
        public enum HandleKind
        {
            Left        = 1 << 0,
            Top         = 1 << 1,
            Right       = 1 << 2,
            Bottom      = 1 << 3,
            TopLeft     = Top | Left,
            TopRight    = Top | Right,
            BottomRight = Bottom | Right,
            BottomLeft  = Bottom | Left,
        }

        public static IEnumerable<BBoxHandle> GetSelectionHandles(Data.Layer layer, float rescale)
        {
            var bounds = layer.SelectionBounds;

            if (bounds.Width > 0 && bounds.Height > 0)
            {
                yield return GetSelectionHandle(bounds, HandleKind.BottomLeft, rescale);
                yield return GetSelectionHandle(bounds, HandleKind.TopLeft, rescale);
                yield return GetSelectionHandle(bounds, HandleKind.TopRight, rescale);
                yield return GetSelectionHandle(bounds, HandleKind.BottomRight, rescale);
            }
            if (bounds.Width > 0)
            {
                yield return GetSelectionHandle(bounds, HandleKind.Left, rescale);
                yield return GetSelectionHandle(bounds, HandleKind.Right, rescale);
            }
            if (bounds.Height > 0)
            {
                yield return GetSelectionHandle(bounds, HandleKind.Bottom, rescale);
                yield return GetSelectionHandle(bounds, HandleKind.Top, rescale);
            }
        }

        public static BBoxHandle GetSelectionHandle(Rect bounds, HandleKind kind, float rescale)
        {
            Vector2 pos;
            var radius = 4f * rescale;
            var margin = 4.5f * rescale;

            if (kind.HasFlag(HandleKind.Right))
            {
                pos.X = bounds.Right + radius + margin;
            }
            else if (kind.HasFlag(HandleKind.Left))
            {
                pos.X = bounds.Left - radius - margin;
            }
            else
            {
                pos.X = .5f * (bounds.Left + bounds.Right);
            }

            if (kind.HasFlag(HandleKind.Top))
            {
                pos.Y = bounds.Top + radius + margin;
            }
            else if (kind.HasFlag(HandleKind.Bottom))
            {
                pos.Y = bounds.Bottom - radius - margin;
            }
            else
            {
                pos.Y = .5f * (bounds.Bottom + bounds.Top);
            }

            return new BBoxHandle(kind, pos);
        }

        public struct GuidelineRule
        {
            public Data.Guideline Guideline { get; }

            public GuidelineRule(Data.Guideline guideline)
            {
                Guideline = guideline;
            }
        }

        public static IEnumerable<Data.Guideline> GetAllGuidelines(Data.Layer layer)
        {
            return Enumerable.Concat(layer.Guidelines, GetMasterGuidelines(layer));
        }

        public static IEnumerable<Data.Guideline> GetMasterGuidelines(Data.Layer layer)
        {
            return layer.Master?.Guidelines ?? Enumerable.Empty<Data.Guideline>();
        }

        public static Data.Guideline GetSelectedGuideline(Data.Layer layer)
        {
            var selection = layer.Selection;

            if (selection.Count > 1)
            {
            }
            else if (selection.Count > 0)
            {
                return layer.Selection.First() as Data.Guideline;
            }
            else if (layer.Master is Data.Master master)
            {
                return master.Guidelines.Where(g => g.IsSelected).FirstOrDefault();
            }

            return null;
        }

        static IEnumerable<int> GetVerticalMetrics(Data.Master master)
        {
            yield return master.Ascender;
            yield return master.CapHeight;
            yield return master.XHeight;
            yield return 0;
            yield return master.Descender;
        }
        static IEnumerable<int> GetVerticalMetrics(Data.Layer layer)
        {
            if (layer.Master is Data.Master master)
            {
                return GetVerticalMetrics(master);
            }
            return Enumerable.Empty<int>();  // TODO: or a fallback?
        }

        public static object HitTest(Data.Layer layer, Point pos, float rescale, ILayerElement ignoreElement = null,
                                     bool testAnchors = true, bool testGuidelines = true, bool testSelectionHandles = true, bool testPoints = true, bool testSegments = true)
        {
            var halfSize = 5 * rescale;

            if (testAnchors)
                foreach (var anchor in layer.Anchors.AsEnumerable().Reverse())
                {
                    if (!ReferenceEquals(anchor, ignoreElement))
                    {
                        var dx = anchor.X - pos.X;
                        var dy = anchor.Y - pos.Y;

                        if (-halfSize <= dx && dx <= halfSize &&
                            -halfSize <= dy && dy <= halfSize)
                        {
                            return anchor;
                        }
                    }
                }
            if (testSelectionHandles)
                foreach (var handle in GetSelectionHandles(layer, rescale))
                {
                    var delta = handle.Position - pos.ToVector2();

                    if (-halfSize <= delta.X && delta.X <= halfSize &&
                        -halfSize <= delta.Y && delta.Y <= halfSize)
                    {
                        return handle;
                    }
                }
            if (testPoints)
                foreach (var path in layer.Paths.AsEnumerable().Reverse())
                {
                    foreach (var point in path.Points.AsEnumerable().Reverse())
                    {
                        if (!ReferenceEquals(point, ignoreElement))
                        {
                            var dx = point.X - pos.X;
                            var dy = point.Y - pos.Y;

                            if (-halfSize <= dx && dx <= halfSize &&
                                -halfSize <= dy && dy <= halfSize)
                            {
                                return point;
                            }
                        }
                    }
                }
            var p = pos.ToVector2();
            foreach (var component in layer.Components.AsEnumerable().Reverse())
            {
                if (!ReferenceEquals(component, ignoreElement) && component.ClosedCanvasPath.FillContainsPoint(p))
                {
                    return component;
                }
            }
            if (testGuidelines)
                foreach (var guideline in GetAllGuidelines(layer).Reverse())
                {
                    if (!ReferenceEquals(guideline, ignoreElement))
                    {
                        var dx = guideline.X - pos.X;
                        var dy = guideline.Y - pos.Y;

                        if (-halfSize <= dx && dx <= halfSize &&
                            -halfSize <= dy && dy <= halfSize)
                        {
                            return guideline;
                        }
                    }
                }

            var tol_2 = 9 + rescale * (6 + rescale);
            if (testSegments)
                foreach (var path in layer.Paths.AsEnumerable().Reverse())
                {
                    foreach (var segment in path.Segments)
                    {
                        var proj = segment.ProjectPoint(p);

                        if (proj.HasValue && (proj.Value.Item1 - p).LengthSquared() <= tol_2)
                        {
                            return segment;
                        }
                    }
                }
            if (testGuidelines && testSegments && GetSelectedGuideline(layer) is Data.Guideline selGuideline)
            {
                var proj = BezierMath.ProjectPointOnLine(p, selGuideline.ToVector2(), selGuideline.Direction);

                if ((proj - p).LengthSquared() <= tol_2)
                {
                    return new GuidelineRule(selGuideline);
                }
            }

            return null;
        }

        static bool IsMoveTarget(Data.Point point, Data.Point prev, Data.Point next)
        {
            return point.IsSelected || (point.Type == Data.PointType.None && (
                    (prev.Type != Data.PointType.None && prev.IsSelected) ||
                    (next.Type != Data.PointType.None && next.IsSelected)
                ));
        }

        public static SnapResult SnapPoint(Data.Layer layer, Point pos, float rescale, ILocatable snapPoint, Point? clampPoint = null)
        {
            Vector2 actualPos;
            Data.Point nearPoint = null;
            (float, float) xSnapLine;
            (float, float) ySnapLine;

            // We clamp to the snapTarget pos if specified. The clamped axis is ignored for snapping purposes.
            if (clampPoint.HasValue)
            {
                var dx = pos.X - clampPoint.Value.X;
                var dy = pos.Y - clampPoint.Value.Y;

                if (Math.Abs(dy) >= Math.Abs(dx))
                {
                    xSnapLine = ((float)clampPoint.Value.Y, (float)pos.Y);
                    actualPos.X = (float)clampPoint.Value.X;
                    actualPos.Y = SnapPointY(layer, pos, rescale, out ySnapLine, snapPoint);
                }
                else
                {
                    ySnapLine = ((float)clampPoint.Value.X, (float)pos.X);
                    actualPos.Y = (float)clampPoint.Value.Y;
                    actualPos.X = SnapPointX(layer, pos, rescale, out xSnapLine, snapPoint);
                }
            }
            else
            {
                if (SnapPointXY(layer, pos, rescale) is Data.Point point)
                {
                    actualPos = point.ToVector2();
                    nearPoint = point;
                    xSnapLine = ySnapLine = (float.NaN, float.NaN);
                }
                else
                {
                    actualPos.X = SnapPointX(layer, pos, rescale, out xSnapLine, snapPoint);
                    actualPos.Y = SnapPointY(layer, pos, rescale, out ySnapLine, snapPoint);
                }
            }

            return new SnapResult(
                actualPos,
                nearPoint,
                xSnapLine,
                ySnapLine
            );
        }

        static Data.Point SnapPointXY(Data.Layer layer, Point pos, float rescale)
        {
            var halfSize = 5 * rescale;

            foreach (var path in layer.Paths)
            {
                var prev = path.Points.Count >= 3 ? path.Points[path.Points.Count - 2] : path.Points.First();
                var point = path.Points.Count >= 3 ? path.Points[path.Points.Count - 1] : prev;
                foreach (var next in path.Points)
                {
                    if (point.Type != Data.PointType.None && !IsMoveTarget(point, prev, next))
                    {
                        var dx = point.X - pos.X;
                        var dy = point.Y - pos.Y;

                        if (-halfSize <= dx && dx <= halfSize &&
                            -halfSize <= dy && dy <= halfSize)
                        {
                            return point;
                        }
                    }

                    prev = point;
                    point = next;
                }
            }

            return null;
        }

        static float SnapPointX(Data.Layer layer, Point pos, float rescale, out (float, float) snapLine, object snapPoint)
        {
            var halfSize = 5 * rescale;

            // TODO: choose not the first, but the best snap target?
            foreach (var path in layer.Paths)
            {
                var prev = path.Points.Count >= 3 ? path.Points[path.Points.Count - 2] : path.Points.First();
                var point = path.Points.Count >= 3 ? path.Points[path.Points.Count - 1] : prev;
                foreach (var next in path.Points)
                {
                    if (!IsMoveTarget(point, prev, next) || ReferenceEquals(point, snapPoint))
                    {
                        var dx = point.X - pos.X;

                        if (-halfSize <= dx && dx <= halfSize)
                        {
                            // Bias towards oncurve
                            if (point.Type == Data.PointType.None
                                && next.Type != Data.PointType.None
                                && !next.IsSelected  /* !IsMoveTarget */
                                && next.X == point.X)
                            {
                                snapLine = (next.Y, float.NaN);
                            }
                            else
                            {
                                snapLine = (point.Y, float.NaN);
                            }

                            return point.X;
                        }
                    }

                    prev = point;
                    point = next;
                }
            }

            snapLine = (float.NaN, float.NaN);
            return (float)pos.X;
        }

        static float SnapPointY(Data.Layer layer, Point pos, float rescale, out (float, float) snapLine, object snapPoint)
        {
            var halfSize = 5 * rescale;

            foreach (var vmetric in GetVerticalMetrics(layer))
            {
                var dy = vmetric - pos.Y;

                if (-halfSize <= dy && dy <= halfSize)
                {
                    snapLine = (0, layer.Width);
                    return vmetric;
                }
            }
            foreach (var path in layer.Paths)
            {
                var prev = path.Points.Count >= 3 ? path.Points[path.Points.Count - 2] : path.Points.First();
                var point = path.Points.Count >= 3 ? path.Points[path.Points.Count - 1] : prev;
                foreach (var next in path.Points)
                {
                    if (!IsMoveTarget(point, prev, next) || ReferenceEquals(point, snapPoint))
                    {
                        var dy = point.Y - pos.Y;

                        if (-halfSize <= dy && dy <= halfSize)
                        {
                            // Bias towards oncurve
                            if (point.Type == Data.PointType.None
                                && next.Type != Data.PointType.None
                                && !next.IsSelected  /* !IsMoveTarget */
                                && next.Y == point.Y)
                            {
                                snapLine = (next.X, float.NaN);
                            }
                            else
                            {
                                snapLine = (point.X, float.NaN);
                            }

                            return point.Y;
                        }
                    }

                    prev = point;
                    point = next;
                }
            }

            snapLine = (float.NaN, float.NaN);
            return (float)pos.Y;
        }
    }
}