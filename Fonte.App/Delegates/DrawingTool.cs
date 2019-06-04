/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Delegates
{
    using Fonte.App.Controls;
    using Fonte.App.Utilities;
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Geometry;

    using System.Collections.Generic;
    using System.Numerics;
    using Windows.Foundation;
    using Windows.System;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;

    public class DrawingTool : BaseTool
    {
        private bool _drawLine;
        private Data.Path _path;
        private List<Point> _points = new List<Point>();
        Point _p1, _p2;

        public override void OnDisabled(DesignCanvas canvas)
        {
            base.OnDisabled(canvas);

            _path = null;
            if (_points != null)
            {
                _points.Clear();
                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnDraw(DesignCanvas canvas, CanvasDrawingSession ds, float rescale)
        {
            if (_path != null && _points.Count > 0)
            {
                var p0 = _path.Points[_path.Points.Count - 1];
                var p3 = _points[_points.Count - 1];

                if (_drawLine || _points.Count < 2)
                {
                    ds.DrawLine(p0.X, p0.Y, (float)p3.X, (float)p3.Y, Colors.Black);
                }
                else
                {
                    var pb = new CanvasPathBuilder(ds);
                    pb.BeginFigure(p0.ToVector2());
                    pb.AddCubicBezier(
                        new Vector2((float)_p1.X, (float)_p1.Y),
                        new Vector2((float)_p2.X, (float)_p2.Y),
                        new Vector2((float)p3.X, (float)p3.Y)
                    );
                    pb.EndFigure(CanvasFigureLoop.Open);

                    ds.DrawGeometry(CanvasGeometry.CreatePath(pb), Colors.Black);

                    foreach (var point in _points)
                    {
                        ds.DrawLine((float)point.X, (float)point.Y + 4, (float)point.X, (float)point.Y - 4, Colors.Tomato);
                        ds.DrawLine((float)point.X - 4, (float)point.Y, (float)point.X + 4, (float)point.Y, Colors.Tomato);
                    }
                }
            }
        }

        public override void OnKeyDown(DesignCanvas canvas, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Menu)
            {
                _drawLine = true;
            }
            else if (e.Key == VirtualKey.Escape && _path != null)
            {
                _path = null;
            }
            else
            {
                return;
            }

            e.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        public override void OnKeyUp(DesignCanvas canvas, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Menu)
            {
                _drawLine = false;
            }
            else
            {
                return;
            }

            e.Handled = true;
            ((App)Application.Current).InvalidateData();
        }

        public override void OnPointerPressed(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(canvas, e);

            var ptPoint = e.GetCurrentPoint(canvas);
            if (ptPoint.Properties.IsLeftButtonPressed)
            {
                var pos = canvas.GetCanvasPosition(ptPoint.Position);

                if (_path != null)
                {
                    var points = _path.Points;

                    points[points.Count - 1].Selected = false;
                    if (_points.Count >= 2)
                    {
                        points.AddRange(
                            new List<Data.Point>() {
                                new Data.Point((float)_p1.X, (float)_p1.Y),
                                new Data.Point((float)_p2.X, (float)_p2.Y),
                                new Data.Point((float)pos.X, (float)pos.Y, Data.PointType.Curve),
                            }
                        );
                    }
                    else
                    {
                        points.Add(
                            new Data.Point((float)pos.X, (float)pos.Y, Data.PointType.Line)
                        );
                    }
                    points[points.Count - 1].Selected = true;
                }
                else
                {
                    _path = new Data.Path(
                        new List<Data.Point>() {
                            new Data.Point((float)pos.X, (float)pos.Y, Data.PointType.Move)
                        }
                    );
                    canvas.Layer.Paths.Add(_path);

                    _path.Points[0].Selected = true;
                }

                _points.Clear();
                ((App)Application.Current).InvalidateData();
            }
        }

        public override void OnPointerMoved(DesignCanvas canvas, PointerRoutedEventArgs e)
        {
            base.OnPointerMoved(canvas, e);

            var ptPoint = e.GetCurrentPoint(canvas);
            if (_path != null && !ptPoint.Properties.IsLeftButtonPressed)
            {
                var pos = canvas.GetCanvasPosition(ptPoint.Position);

                if (_points.Count >= 2)
                {
                    var p0 = _path.Points[_path.Points.Count - 1];

                    (_p1, _p2) = BezierMath.BezierApproximation(_points, new Point(p0.X, p0.Y), pos);
                }
                else
                {
                    _p1 = _p2 = new Point();
                }

                _points.Add(pos);
                ((App)Application.Current).InvalidateData();
            }
        }

#region IToolBarEntry implementation

        public override IconElement Icon { get; } = new FontIcon() { Glyph = "\uec87" };

        public override string Name => "Drawing";

        public override KeyboardAccelerator Shortcut { get; } = new KeyboardAccelerator() { Key = VirtualKey.D };

#endregion
    }
}
