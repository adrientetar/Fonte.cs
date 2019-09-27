/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Changes;
    using Fonte.Data.Collections;
    using Fonte.Data.Geometry;
    using Fonte.Data.Interfaces;
    using Fonte.Data.Utilities;
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Geometry;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;

    [JsonConverter(typeof(PathConverter))]
    public partial class Path
    {
        internal List<Point> _points;

        internal Dictionary<string, object> _extraData;

        private Rect _bounds = Rect.Empty;
        private CanvasGeometry _canvasPath;

        public ObserverList<Point> Points
        {
            get
            {
                var items = new ObserverList<Point>(_points);
                items.ChangeRequested += (sender, args) =>
                {
                    if (args.Action == NotifyChangeRequestedAction.Add)
                    {
                        new PathPointsChange(this, args.NewStartingIndex, args.NewItems, true).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Remove)
                    {
                        new PathPointsChange(this, args.OldStartingIndex, args.OldItems, false).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Replace)
                    {
                        new PathPointsReplaceChange(this, args.NewStartingIndex, args.NewItems).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Reset)
                    {
                        new PathPointsResetChange(this).Apply();
                    }
                };
                return items;
            }
        }

        public Dictionary<string, object> ExtraData
        {
            get
            {
                if (_extraData == null)
                {
                    _extraData = new Dictionary<string, object>();
                }
                return _extraData;
            }
        }

        public Layer Parent
        { get; internal set; }

        /**/

        public Rect Bounds
        {
            get
            {
                if (_bounds.IsEmpty)
                {
                    _bounds = Conversion.FromFoundationRect(CanvasPath.ComputeBounds());
                }
                return _bounds;
            }
        }

        public CanvasGeometry CanvasPath
        {
            get
            {
                if (_canvasPath == null)
                {
                    var device = CanvasDevice.GetSharedDevice();
                    var builder = new CanvasPathBuilder(device);

                    var stack = new Vector2[2];
                    var stackIndex = 0;

                    var start = _points[0];
                    var isOpen = start.Type == PointType.Move;
                    if (!isOpen)
                    {
                        start = _points[_points.Count - 1];
                    }
                    builder.BeginFigure(start.X, start.Y);

                    foreach (var point in _points.Skip(isOpen ? 1 : 0))
                    {
                        switch (point.Type)
                        {
                            case PointType.Curve:
                                Debug.Assert(stackIndex == 2);
                                builder.AddCubicBezier(stack[0], stack[1], point.ToVector2());
                                stackIndex = 0;
                                break;
                            case PointType.Line:
                                builder.AddLine(point.X, point.Y);
                                break;
                            case PointType.None:
                                stack[stackIndex++] = point.ToVector2();
                                break;
                            default:
                                throw new InvalidOperationException($"{point.Type} isn't a valid segment type here");
                        }
                    }

                    builder.EndFigure(isOpen ? CanvasFigureLoop.Open : CanvasFigureLoop.Closed);

                    _canvasPath = CanvasGeometry.CreatePath(builder);
                }
                return _canvasPath;
            }
        }

        public bool IsOpen
        {
            get
            {
                return Points.Count == 0 || Points[0].Type == PointType.Move;
            }
        }

        public bool IsSelected
        {
            get
            {
                foreach (var point in Points)
                {
                    if (!point.IsSelected) return false;
                }
                return true;
            }
            set
            {
                foreach (var point in Points)
                {
                    point.IsSelected = value;
                }
            }
        }

        public IEnumerable<Segment> Segments
        {
            get
            {
                int start = 0, count = 0;
                foreach (var point in _points)
                {
                    ++count;
                    if (point.Type != PointType.None)
                    {
                        yield return new Segment(this, start, count);
                        start += count;
                        count = 0;
                    }
                }
            }
        }

        public Path(List<Point> points = default, Dictionary<string, object> extraData = default)
        {
            _points = points ?? new List<Point>();
            _extraData = extraData;

            foreach (Point point in Points)
            {
                //point.Selected = false;
                point.Parent = this;
            }
        }

        public void Close()
        {
            var points = Points;

            if (points.Count > 0 && IsOpen)
            {
                using var group = Parent?.CreateUndoGroup();

                var point = points.PopAt(0);
                points.Add(point);
                point.IsSmooth = false;
                point.Type = PointType.Line;
            }
        }

        public void Reverse()
        {
            var points = Points;

            if (points.Count > 0)
            {
                using var group = Parent?.CreateUndoGroup();

                var start = points[0];
                var type = start.Type;

                List<Point> result;
                if (type != PointType.Move)
                {
                    var pivot = points.Last();

                    result = points.GetRange(0, points.Count - 1);
                    result.Reverse();
                    result.Add(pivot);
                    type = pivot.Type;
                }
                else
                {
                    result = points.GetRange(0, points.Count);
                    result.Reverse();
                }

                foreach (var point in result)
                {
                    if (point.Type != PointType.None)
                    {
                        (point.Type, type) = (type, point.Type);
                    }
                }
                // TODO: add a replace action? Could be introduced as a setter on the owner property
                points.Clear();
                points.AddRange(result);
            }
        }

        public void StartAt(int index)
        {
            if (IsOpen)
                throw new InvalidOperationException("Cannot set start point in open path");

            if (Points.Count - index + 1 != 0)
            {
                using var group = Parent?.CreateUndoGroup();

                var end = Points.GetRange(0, index + 1);
                if (end.Count > 0 && end[end.Count - 1].Type == PointType.None)
                {
                    throw new InvalidOperationException($"Index {index} isn't at segment boundary");
                }
                Points.RemoveRange(0, index + 1);
                Points.AddRange(end);
            }
        }

        public override string ToString()
        {
            return $"{nameof(Path)}({string.Join(", ", Points)})";
        }

        public void Transform(Matrix3x2 matrix, bool selectionOnly = false)
        {
            using var group = Parent?.CreateUndoGroup();

            foreach (var point in Points)
            {
                if (!selectionOnly || point.IsSelected)
                {
                    var pos = Vector2.Transform(point.ToVector2(), matrix);

                    point.X = pos.X;
                    point.Y = pos.Y;
                }
            }
        }

        internal void OnChange(IChange change)
        {
            _bounds = Rect.Empty;
            _canvasPath = null;

            Parent?.OnChange(change);
        }
    }

    public struct Segment : ISelectable
    {
        private readonly Path _path;
        private readonly int _index;
        private readonly int _count;

        public bool IsSelected
        {
            get
            {
                var points = PointsInclusive;
                return points[0].IsSelected && points[points.Count - 1].IsSelected;
            }
            set
            {
                foreach (var point in PointsInclusive)
                {
                    point.IsSelected = value;
                }
            }
        }

        public List<Point> OffCurves
        {
            get
            {
                return _path._points.GetRange(_index, _count - 1);
            }
        }

        public Point OnCurve
        {
            get
            {
                return _path._points[_index + _count - 1];
            }
        }

        public Path Parent
        {
            get
            {
                return _path;
            }
        }

        public List<Point> Points
        {
            get
            {
                return _path._points.GetRange(_index, _count);
            }
        }

        public List<Point> PointsInclusive
        {
            get
            {
                if (_index == 0)
                {
                    var points = Points;
                    if (OnCurve.Type != PointType.Move)
                    {
                        points.Insert(0, _path._points.Last());
                    }
                    return points;
                }
                return _path._points.GetRange(_index - 1, _count + 1);
            }
        }

        public Segment(Path path, int index, int count)
        {
            _path = path;
            _index = index;
            _count = count;
        }

        public Segment ConvertTo(PointType type)
        {
            var onCurve = OnCurve;
            if (type == onCurve.Type)
            {
                return this;
            }

            using var group = Parent.Parent?.CreateUndoGroup();

            if (type == PointType.Curve)
            {
                if (onCurve.Type == PointType.Line)
                {
                    var points = _path.Points;
                    var start = PointsInclusive[0];

                    onCurve.Type = PointType.Curve;
                    points.Insert(_index, new Point(
                            start.X + .65f * (OnCurve.X - start.X),
                            start.Y + .65f * (OnCurve.Y - start.Y)
                        ));
                    points.Insert(_index, new Point(
                            start.X + .35f * (OnCurve.X - start.X),
                            start.Y + .35f * (OnCurve.Y - start.Y)
                        ));

                    return new Segment(_path, _index, 3);
                }
            }
            else if (type == PointType.Line)
            {
                if (onCurve.Type == PointType.Curve)
                {
                    var start = PointsInclusive[0];

                    onCurve.IsSmooth = false;
                    onCurve.Type = PointType.Line;
                    start.IsSmooth = false;
                    _path.Points.RemoveRange(_index, _count - 1);

                    return new Segment(_path, _index, 1);
                }
            }
            else if (type == PointType.Move)
            {
                if (onCurve.Type == PointType.Curve ||
                    onCurve.Type == PointType.Line)
                {
                    if (_index != 0)
                        throw new InvalidOperationException(
                            string.Format("Segment for conversion to {0} needs to be at index 0 ({1})", type, _index));

                    onCurve.IsSmooth = false;
                    onCurve.Type = PointType.Move;
                    _path.Points.RemoveRange(_index, _count - 1);

                    return new Segment(_path, _index, 1);
                }
            }

            throw new InvalidOperationException(
                string.Format("Cannot convert from {0} to {1}", onCurve.Type, type));
        }

        public void Remove(bool nodeBias = false)
        {
            var onCurve = OnCurve;

            // Remove points around node, if a second curve segment follows
            if (nodeBias && onCurve.Type == PointType.Curve)
            {
                var points = _path._points;
                var firstOff = points[_index];
                var nextOff = Sequence.NextItem(points, _index + _count - 1);

                if (!(points[0].Type == PointType.Move && points.Last() == onCurve) &&
                    nextOff.Type == PointType.None)
                {
                    nextOff.X = firstOff.X;
                    nextOff.Y = firstOff.Y;
                }
            }
            _path.Points.RemoveRange(_index, _count);
        }

        public (Vector2, float)[] IntersectLine(Vector2 p0, Vector2 p1)
        {
            var onCurve = OnCurve;

            if (onCurve.Type == PointType.Curve)
            {
                return BezierMath.IntersectLineAndCurve(p0, p1, PointsInclusive.Select(p => p.ToVector2())
                                                                               .ToArray());
            }
            else if (onCurve.Type == PointType.Line)
            {
                var result = BezierMath.IntersectLines(p0, p1, PointsInclusive.Select(p => p.ToVector2())
                                                                              .ToArray());
                if (result.HasValue) return new (Vector2, float)[] { result.Value };
            }
            return new (Vector2, float)[0];
        }

        public (Vector2, float)? ProjectPoint(Vector2 point)
        {
            var onCurve = OnCurve;

            if (onCurve.Type == PointType.Curve)
            {
                return BezierMath.ProjectPointOnCurve(point, PointsInclusive.Select(p => p.ToVector2())
                                                                            .ToArray());
            }
            else if (onCurve.Type == PointType.Line)
            {
                return BezierMath.ProjectPointOnLine(point, PointsInclusive.Select(p => p.ToVector2())
                                                                           .ToArray());
            }
            return null;
        }

        public Segment SplitAt(float t)
        {
            var points = PointsInclusive;
            var onCurve = points.Last();

            if (onCurve.Type == PointType.Curve)
            {
                var curves = BezierMath.SplitCurve(points.Select(p => p.ToVector2())
                                                         .ToArray(), t);

                _path.Points.InsertRange(_index, new List<Point>()
                {
                    new Point(curves[0, 1].X, curves[0, 1].Y),
                    new Point(curves[0, 2].X, curves[0, 2].Y),
                    new Point(curves[0, 3].X, curves[0, 3].Y, PointType.Curve, isSmooth: true)
                });
                points[1].X = curves[1, 1].X;
                points[1].Y = curves[1, 1].Y;
                points[2].X = curves[1, 2].X;
                points[2].Y = curves[1, 2].Y;
                points[3].X = curves[1, 3].X;
                points[3].Y = curves[1, 3].Y;
            }
            else if (onCurve.Type == PointType.Line)
            {
                var loc = Vector2.Lerp(points.First().ToVector2(),
                                       onCurve.ToVector2(),
                                       t);

                _path.Points.Insert(_index, new Point(loc.X, loc.Y, PointType.Line));
            }
            else
            {
                throw new NotImplementedException($"Cannot split {onCurve.Type} segment");
            }

            return new Segment(_path, _index + _count, _count);
        }

        public override string ToString()
        {
            return $"{nameof(Segment)}({_index}..{_index + _count}, {OnCurve.Type})";
        }
    }

    internal class PathConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Path).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var array = JArray.Load(reader);

            var last = array.Last;
            Dictionary<string, object> extraData = null;
            if (last.Type == JTokenType.Object)
            {
                extraData = last.ToObject<Dictionary<string, object>>();
                array.RemoveAt(array.Count - 1);
            }

            return new Path(
                    array.ToObject<List<Point>>(),
                    extraData
                );
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var path = (Path)value;

            IEnumerable<object> data;
            if (path.ExtraData != null && path.ExtraData.Count > 0)
            {
                var content = new List<object>(path.Points.Count + 1);
                content.AddRange(path._points);
                content.Add(path.ExtraData);
                data = content;
            }
            else
            {
                data = path._points;
            }
            serializer.Serialize(writer, data);
        }
    }
}
