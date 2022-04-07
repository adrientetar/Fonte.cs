// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Fonte.App.Utilities;

    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;
    using Windows.Foundation;
    using Windows.System;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public class DrawingTool : BaseTool
    {
        protected override CoreCursor DefaultCursor { get; } = Cursors.Pen;

        public override void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerPressed(canvas, args);

            var ptPoint = args.GetCurrentPoint(canvas);
            var addCurve = ptPoint.Properties.IsRightButtonPressed;
            if ((addCurve || ptPoint.Properties.IsLeftButtonPressed) && canvas.Layer is Data.Layer layer)
            {
                using (var group = layer.CreateUndoGroup())
                {
                    var pos = canvas.FromClientPosition(ptPoint.Position);
                    var tappedItem = canvas.HitTest(pos);
                    var selPoint = GetSelectedPoint(canvas);

                    if (tappedItem is Data.Point tappedPoint && tappedPoint.Type != Data.PointType.None && Is.AtOpenBoundary(tappedPoint))
                    {
                        var tappedPath = tappedPoint.Parent;

                        if (selPoint != null && Is.AtOpenBoundary(selPoint) && AreVisiblyDistinct(canvas, selPoint, tappedPoint))
                        {
                            var selPath = selPoint.Parent;
                            var selPoints = selPath.Points;
                            Outline.JoinPaths(selPath, selPoints[0] == selPoint,
                                              tappedPath, tappedPath.Points[0] == tappedPoint);
                        }

                        if (selPoint != null)
                        {
                            selPoint.IsSelected = false;
                        }
                        tappedPoint.IsSelected = true;
                    }
                    else
                    {
                        var hadSmoothCurve = false;
                        Data.Point shouldSmooth = null;

                        Data.Path path;
                        Data.PointType type;
                        // Add a point to the current path, if any
                        if (selPoint != null && Is.AtOpenBoundary(selPoint))
                        {
                            path = selPoint.Parent;
                            var lastPoint = path.Points.Last();
                            if (!args.KeyModifiers.HasFlag(VirtualKeyModifiers.Menu))
                            {
                                if (lastPoint.Type == Data.PointType.Curve)
                                {
                                    lastPoint.IsSmooth = hadSmoothCurve = true;
                                }
                                else
                                {
                                    shouldSmooth = lastPoint;
                                }
                            }

                            if (args.KeyModifiers.HasFlag(VirtualKeyModifiers.Shift))
                            {
                                pos = ClampToOrigin(pos, new Point(lastPoint.X, lastPoint.Y));
                            }
                            type = Data.PointType.Line;
                        }
                        // Else just create a new one
                        else
                        {
                            path = new Data.Path();
                            layer.Paths.Add(path);
                            type = Data.PointType.Move;
                            addCurve = false;
                        }

                        // In any case, unselect all points (*click*) and enable new point
                        layer.ClearSelection();
                        var x = Outline.RoundToGrid((float)pos.X);
                        var y = Outline.RoundToGrid((float)pos.Y);
                        path.Points.Add(new Data.Point(x, y, type)
                        {
                            IsSelected = true
                        });
                        if (addCurve)
                        {
                            path.Segments.Last().ConvertTo(Data.PointType.Curve);

                            if (shouldSmooth != null)
                            {
                                Outline.TryTogglePointSmoothness(shouldSmooth);
                            }
                        }

                        if (hadSmoothCurve)
                        {
                            var extractor = new SmoothCurveExtractor(path);
                            extractor.ProcessSegments();

                            if (extractor.ShouldInterpolate)
                            {
                                var cps = Hobby.SolveOpen(extractor.SourceData.ToList(),
                                                          dir0: extractor.Dir0, dirn: extractor.DirN);

                                var i = 0;
                                foreach (var cp in extractor.ControlPoints)
                                {
                                    var loc = cps[i++];

                                    cp.X = loc.X;
                                    cp.Y = loc.Y;
                                }
                                Debug.Assert(i == cps.Length);
                            }
                        }
                    }
                }

                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerMoved(canvas, args);
        }

        public override void OnPointerReleased(DesignCanvas canvas, PointerRoutedEventArgs args)
        {
            base.OnPointerReleased(canvas, args);
        }

        public override void OnRightTapped(DesignCanvas canvas, RightTappedRoutedEventArgs args)
        {
        }

        /**/

        bool AreVisiblyDistinct(DesignCanvas canvas, Data.Point point, Data.Point other)
        {
            var pos = canvas.FromCanvasPosition(point.ToVector2().ToPoint());
            var otherPos = canvas.FromCanvasPosition(other.ToVector2().ToPoint());

            return CanStartDragging(pos, otherPos);
        }

        Data.Point GetSelectedPoint(DesignCanvas canvas)
        {
            var selection = canvas.Layer.Selection;

            if (selection.Count == 1 && selection.First() is Data.Point point)
            {
                return point;
            }
            return null;
        }

#region IToolBarEntry implementation

        public override IconSource Icon { get; } = new FontIconSource() { FontSize = 16, Glyph = "\uf7bb" };  // ee56  // ef15

        public override string Name => "Drawing";

        public override KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator() { Key = VirtualKey.D };

#endregion
    }

    class SmoothCurveExtractor
    {
        int _count = 0;
        float? _dir0 = null;
        float? _dirn = null;

        Data.Path _path;
        Data.Point _prevOn = null;
        State _state = State.None;

        public int Count => _count;
        public float? Dir0 => _dir0;
        public float? DirN => _dirn;

        public IEnumerable<Data.Point> ControlPoints
        {
            get
            {
                return _path.Points.Skip(_path.Points.Count - _count * 3 - Offset)
                                   .Where(point => point.Type == Data.PointType.None);
            }
        }

        public bool ShouldInterpolate => _count > 1;

        public IEnumerable<Vector2> SourceData
        {
            get
            {
                var skipPoint = _state == State.TrailingLine ? _path.Points.Last() : null;
                // TODO: we could limit count to 3, as Keynote seems to be doing
                return _path.Points.Skip(_path.Points.Count - _count * 3 - Offset)
                                   .Where(point => point.Type != Data.PointType.None && point != skipPoint)
                                   .Select(point => point.ToVector2());
            }
        }

        int Offset => _state == State.TrailingLine ? 2 : 1;

        enum State
        {
            None,
            CurveSection,
            TrailingLine
        };

        public SmoothCurveExtractor(Data.Path path)
        {
            _path = path;
        }

        void AddSegment(Data.Segment segment)
        {
            var on = segment.OnCurve;

            if (_state == State.None ||
                _state == State.TrailingLine)
            {
                _count = 0;
                if (_state == State.TrailingLine)
                {
                    _dir0 = _dirn;
                    _dirn = null;

                    _state = State.None;
                }

                if (on.Type == Data.PointType.Line)
                {
                    _dir0 = Hobby.VectorAngle(on.ToVector2() - _prevOn.ToVector2());
                }
                else if (on.Type == Data.PointType.Curve && on.IsSmooth)
                {
                    _state = State.CurveSection;
                    ++_count;
                }
                else
                {
                    _dir0 = null;
                }
            }
            else if (_state == State.CurveSection)
            {
                if (on.Type == Data.PointType.Curve)
                {
                    ++_count;

                    if (!on.IsSmooth)
                    {
                        _state = State.None;
                    }
                }
                else if (on.Type == Data.PointType.Line)
                {
                    _state = State.TrailingLine;
                    _dirn = Hobby.VectorAngle(on.ToVector2() - _prevOn.ToVector2());
                }
            }
        }

        public void ProcessSegments()
        {
            foreach (var segment in _path.Segments)
            {
                AddSegment(segment);
                _prevOn = segment.OnCurve;
            }
        }
    }
}
