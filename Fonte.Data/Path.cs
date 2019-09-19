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

                    var start = Points[0];
                    var skip = start.Type == PointType.Move;
                    if (!skip)
                    {
                        start = Points[Points.Count - 1];
                    }
                    builder.BeginFigure(start.X, start.Y);

                    foreach (var point in Points)
                    {
                        if (skip)
                        {
                            skip = false;
                            continue;
                        }
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
                        }
                    }

                    builder.EndFigure(start.Type == PointType.Move ? CanvasFigureLoop.Open : CanvasFigureLoop.Closed);

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
                var points = Points;

                int start = 0, count = 0;
                foreach (var point in points)
                {
                    ++count;
                    if (point.Type != PointType.None)
                    {
                        yield return new Segment(points, start, count);
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
            if (Points.Count > 0 && IsOpen)
            {
                using (var group = Parent?.CreateUndoGroup())
                {
                    var point = Points.PopAt(0);
                    Points.Add(point);
                    point.IsSmooth = false;
                    point.Type = PointType.Line;
                }
            }
        }

        public void Reverse()
        {
            var points = Points;

            if (points.Count > 0)
            {
                using (var group = Parent?.CreateUndoGroup())
                {
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
        }

        public void StartAt(int index)
        {
            if (IsOpen)
                throw new InvalidOperationException("Cannot set start point in open path");

            if (Points.Count - index + 1 != 0)
            {
                using (var group = Parent?.CreateUndoGroup())
                {
                    var end = Points.GetRange(0, index + 1);
                    if (end.Count > 0 && end[end.Count - 1].Type == PointType.None)
                    {
                        throw new InvalidOperationException($"Index {index} isn't at segment boundary");
                    }
                    Points.RemoveRange(0, index + 1);
                    Points.AddRange(end);
                }
            }
        }

        public override string ToString()
        {
            return $"{nameof(Path)}({string.Join(", ", Points)})";
        }

        public void Transform(Matrix3x2 matrix, bool selectionOnly = false)
        {
            using (var group = Parent?.CreateUndoGroup())
            {
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
        private readonly int _count;
        private readonly int _index;
        private readonly ObserverList<Point> _points;

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
                return _points.GetRange(_index, _count - 1);
            }
        }

        public Point OnCurve
        {
            get
            {
                return _points[_index + _count - 1];
            }
        }

        public Path Parent
        {
            get
            {
                return _points[_index].Parent;
            }
        }

        public List<Point> Points
        {
            get
            {
                return _points.GetRange(_index, _count);
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
                        points.Insert(0, _points[_points.Count - 1]);
                    }
                    return points;
                }
                return _points.GetRange(_index - 1, _count + 1);
            }
        }

        public Segment(ObserverList<Point> points, int index, int count)
        {
            _count = count;
            _index = index;
            _points = points;
        }

        public void ConvertTo(PointType type)
        {
            var onCurve = OnCurve;

            bool ok = type == onCurve.Type;
            if (type == PointType.Curve)
            {
                if (onCurve.Type == PointType.Line)
                {
                    var start = PointsInclusive[0];

                    onCurve.Type = PointType.Curve;
                    _points.Insert(_index, new Point(
                            start.X + .65f * (OnCurve.X - start.X),
                            start.Y + .65f * (OnCurve.Y - start.Y)
                        ));
                    _points.Insert(_index, new Point(
                            start.X + .35f * (OnCurve.X - start.X),
                            start.Y + .35f * (OnCurve.Y - start.Y)
                        ));

                    ok = true;
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
                    _points.RemoveRange(_index, _count - 1);

                    ok = true;
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
                    _points.RemoveRange(_index, _count - 1);

                    ok = true;
                }
            }

            if (!ok)
            {
                throw new InvalidOperationException(
                    string.Format("Cannot convert from {0} to {1}", onCurve.Type, type));
            }
        }

        public void Remove(bool nodeBias = false)
        {
            var onCurve = OnCurve;

            // Remove points around node, if a second curve segment follows
            if (nodeBias && onCurve.Type == PointType.Curve)
            {
                var firstOff = _points[_index];
                var nextOff = Sequence.NextItem(_points, _index + _count - 1);

                if (!(_points[0].Type == PointType.Move && _points.Last() == onCurve) &&
                    nextOff.Type == PointType.None)
                {
                    nextOff.X = firstOff.X;
                    nextOff.Y = firstOff.Y;
                }
            }
            _points.RemoveRange(_index, _count);
        }

        public List<Vector2> IntersectLine(Vector2 p1, Vector2 p2)
        {
            throw new NotImplementedException();
        }

        public (Vector2, float)? ProjectPoint(Vector2 p)
        {
            var onCurve = OnCurve;

            if (onCurve.Type == PointType.Curve)
            {
                return BezierMath.ProjectPointOnCurve(p, PointsInclusive);
            }
            else if (onCurve.Type == PointType.Line)
            {
                return BezierMath.ProjectPointOnLine(p, PointsInclusive);
            }

            return null;
        }

        public void SplitAt(float t)
        {
            var points = PointsInclusive;
            var onCurve = points.Last();

            if (onCurve.Type == PointType.Curve)
            {
                var curves = BezierMath.SplitCurve(points, t);

                _points.InsertRange(_index, new List<Point>()
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

                _points.Insert(_index, new Point(loc.X, loc.Y, PointType.Line));
            }
            else
            {
                throw new NotImplementedException($"Cannot split {onCurve.Type} segment");
            }
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
