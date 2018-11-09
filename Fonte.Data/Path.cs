/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Geometry;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Numerics;
    using System.Reflection;
    using Windows.Foundation;

    [JsonConverter(typeof(PathConverter))]
    public partial class Path
    {
        private Rect _bounds = Rect.Empty;
        private CanvasGeometry _canvasPath;
        private ObservableDictionary<string, object> _extraData;

        public IDictionary<string, object> ExtraData
        {
            get
            {
                if (_extraData == null)
                {
                    _extraData = new ObservableDictionary<string, object>();
                    _watchExtraData();
                }
                return _extraData;
            }
        }

        public Layer Parent { get; internal set; }

        public ObservableList<Point> Points { get; }

        /**/

        public Rect Bounds
        {
            get
            {
                if (_bounds.IsEmpty)
                {
                    _bounds = CanvasPath.ComputeBounds();
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

        public IEnumerable<Segment> Segments
        {
            get
            {
                int start = 0, count = 0;
                foreach (var point in Points)
                {
                    ++count;
                    if (point.Type != PointType.None)
                    {
                        yield return new Segment(Points, start, count);
                        start += count;
                        count = 0;
                    }
                }
            }
        }

        public string UniqueId
        {
            get
            {
                if (ExtraData.TryGetValue("id", out object value) && (value as string != null))
                {
                    return (string)value;
                }
                ExtraData["id"] = Guid.NewGuid().ToString();
                return (string)ExtraData["id"];
            }
        }

        public Path(List<Point> points = null, IDictionary<string, object> extraData = null)
        {
            Points = points != null ?
                new ObservableList<Point>(points, copy: false) :
                new ObservableList<Point>();
            _watchPoints();

            foreach (Point point in Points)
            {
                point.Parent = this;
            }

            if (extraData != null)
            {
                _extraData = new ObservableDictionary<string, object>(extraData, copy: false);
                _watchExtraData();
            }
        }

        public void Close()
        {
            if (Points.Count > 0 && IsOpen)
            {
                var point = Points[0];
                Points.RemoveAt(0);
                point.Smooth = false;
                point.Type = PointType.Line;
                Points.Add(point);
            }
        }

        public void Reverse()
        {
            if (Points.Count > 0)
            {
                var start = Points[0];
                var type = start.Type;
                if (type != PointType.Move)
                {
                    var pivot = Points[Points.Count - 1];
                    Points.RemoveAt(Points.Count - 1);
                    Points.Reverse();
                    Points.Add(pivot);
                    type = pivot.Type;
                }
                else
                {
                    Points.Reverse();
                }

                foreach(var point in Points)
                {
                    if (point.Type != PointType.None)
                    {
                        var pType = point.Type;
                        point.Type = type;
                        type = pType;
                    }
                }
            }
        }

        public void StartAt(int index)
        {
            if (IsOpen)
            {
                throw new NotImplementedException("Cannot set start point in open path");
            }
            if (Points.Count - index + 1 != 0)
            {
                var end = Points.GetRange(0, index + 1);
                if (end.Count > 0 && end[end.Count - 1].Type == PointType.None)
                {
                    throw new IndexOutOfRangeException($"Index {index} breaks a segment");
                }
                Points.RemoveRange(0, index + 1);
                Points.AddRange(end);
            }
        }

        public override string ToString()
        {
            return $"{nameof(Path)}({Points})";
        }

        public void Transform(Matrix3x2 matrix)
        {
            throw new NotImplementedException();
        }

        internal void ApplyChange(ChangeFlags flags, Point point = null)
        {
            if (flags.HasFlag(ChangeFlags.Outline))
            {
                _bounds = Rect.Empty;
                _canvasPath = null;
            }

            Parent.ApplyChange(flags, point);
        }

        private void _watchExtraData()
        {
            _extraData.CollectionChanged += (sender, e) =>
            {
                Parent?.ApplyChange(ChangeFlags.None);
            };
        }

        private void _watchPoints()
        {
            Points.CollectionChanged += (sender, e) =>
            {
                if (e.OldItems != null)
                    foreach (Point point in e.OldItems)
                    {
                        point.Parent = null;

                        if (point.Selected)
                        {
                            Parent?.ApplyChange(ChangeFlags.SelectionRemove, point);
                        }
                    }

                if (e.NewItems != null)
                    foreach (Point point in e.NewItems)
                    {
                        Debug.Assert(point.Parent == null);

                        point.Selected = false;
                        point.Parent = this;
                    }

                ApplyChange(ChangeFlags.Outline);
            };
        }
    }

    public class Segment
    {
        private readonly int _count;
        private readonly int _index;
        private ObservableList<Point> _points;

        public Rect Bounds
        {
            get
            {
                throw new NotImplementedException();
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

        public List<Point> Points
        {
            get
            {
                var index = _index - (OnCurve.Type != PointType.Move ? 1 : 0);
                /* Start point on the other end */
                if (index < 0)
                {
                    var list = _points.GetRange(index, _count);
                    list.Insert(0, _points[_points.Count - 1]);
                    return list;
                }
                return _points.GetRange(index, _count);
            }
        }

        public Segment(ObservableList<Point> points, int index, int count)
        {
            _count = count;
            _index = index;
            _points = points;
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
            var path = value as Path;

            IEnumerable<object> data;
            if (path.ExtraData != null && path.ExtraData.Count > 0)
            {
                var content = new List<object>(path.Points.Count + 1);
                content.AddRange(path.Points);
                content.Add(path.ExtraData);
                data = content;
            }
            else
            {
                data = path.Points.List;
            }
            serializer.Serialize(writer, data);
        }
    }
}
