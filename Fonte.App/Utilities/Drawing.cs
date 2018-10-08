/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Utilities
{
    using System;
    using System.Numerics;
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Geometry;

    using Windows.UI;

    class Drawing
    {
        public static CanvasGeometry CreateTriangle(CanvasDrawingSession ds, Vector2 pos, double angle, float size)
        {
            var thirdSize = .33f * size;
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);
            var builder = new CanvasPathBuilder(ds);
            builder.BeginFigure(new Vector2(
                pos.X - cos * thirdSize - sin * size, // -thirdSize
                pos.Y + cos * size - sin * thirdSize  //  size
            ));
            builder.AddLine(new Vector2(
                pos.X - cos * thirdSize + sin * size, // -thirdSize
                pos.Y - cos * size - sin * thirdSize  // -size
            ));
            builder.AddLine(new Vector2(
                pos.X + 2 * cos * thirdSize,          //  thirdSize * 2
                pos.Y + 2 * sin * thirdSize           //  0
            ));
            builder.EndFigure(CanvasFigureLoop.Closed);
            return CanvasGeometry.CreatePath(builder);
        }

        public static void DrawPoints(Data.Layer layer, CanvasDrawingSession ds, float rescale)
        {
            // save these in the Drawing class state? or in a context class/struct
            var backgroundColor = Color.FromArgb(255, 255, 255, 255);
            var notchColor = Color.FromArgb(255, 68, 68, 68);
            var onColor = Color.FromArgb(190, 4, 100, 166);
            var smoothColor = Color.FromArgb(190, 41, 172, 118);
            var offColor = Color.FromArgb(255, 116, 116, 116);
            var otherColor = Color.FromArgb(240, 140, 140, 140);

            var handlePathB = new CanvasPathBuilder(ds);
            var notchPathB = new CanvasPathBuilder(ds);
            var onPathB = new CanvasPathBuilder(ds);
            var selectedOnPathB = new CanvasPathBuilder(ds);
            var smoothPathB = new CanvasPathBuilder(ds);
            var selectedSmoothPathB = new CanvasPathBuilder(ds);
            var offPathB = new CanvasPathBuilder(ds);
            var selectedOffPathB = new CanvasPathBuilder(ds);
            foreach (var path in layer.Paths)
            {
                var points = path.Points;
                Data.Point start;
                if (points.Count > 1)
                {
                    start = points[0];
                    Data.Point next;
                    if (start.Type != Data.PointType.Move)
                    {
                        next = start;
                        start = points[points.Count - 1];
                    }
                    else
                    {
                        next = points[1];
                    }
                    var angle = Math.Atan2(next.Y - start.Y, next.X - start.X);
                    if (start.Selected)
                    {
                        (start.Smooth ? selectedSmoothPathB : selectedOnPathB).AddGeometry(
                            CreateTriangle(ds, start.Position, angle, 9 * rescale)
                        );
                    }
                    else
                    {
                        (start.Smooth ? smoothPathB : onPathB).AddGeometry(
                            CreateTriangle(ds, start.Position, angle, 7 * rescale)
                        );
                    }
                }
                else
                {
                    start = null;
                }

                var breakHandle = path.Open;
                Data.Point prev = points[points.Count - 1];
                foreach (var point in path.Points)
                {
                    var isOffCurve = point.Type == Data.PointType.None;
                    if (!breakHandle && prev.Type == Data.PointType.None != isOffCurve)
                    {
                        handlePathB.BeginFigure(prev.Position);
                        handlePathB.AddLine(point.Position);
                        handlePathB.EndFigure(CanvasFigureLoop.Open);
                    }
                    breakHandle = false;

                    if (isOffCurve)
                    {
                        if (point.Selected)
                        {
                            selectedOffPathB.AddGeometry(
                                CanvasGeometry.CreateEllipse(ds, point.Position, 4 * rescale, 4 * rescale)
                            );
                        }
                        else
                        {
                            offPathB.AddGeometry(
                                CanvasGeometry.CreateEllipse(ds, point.Position, 3 * rescale, 3 * rescale)
                            );
                        }
                    }
                    else
                    {
                        if (point.Smooth)
                        {
                            var angle = Math.Atan2(point.Y - prev.Y, point.X - prev.X) - .5 * Math.PI;
                            var cos = (float)Math.Cos(angle);
                            var sin = (float)Math.Sin(angle);
                            var notchSize = 1.4f * rescale;
                            notchPathB.BeginFigure(
                                point.X - cos * notchSize, point.Y - sin * notchSize
                            );
                            notchPathB.AddLine(
                                point.X + cos * notchSize, point.Y + sin * notchSize
                            );
                            notchPathB.EndFigure(CanvasFigureLoop.Open);

                            if (ReferenceEquals(point, start))
                            {
                            }
                            else if (point.Selected)
                            {
                                selectedSmoothPathB.AddGeometry(
                                    CanvasGeometry.CreateEllipse(ds, point.Position, 5.15f * rescale, 5.15f * rescale)
                                );
                            }
                            else
                            {
                                smoothPathB.AddGeometry(
                                    CanvasGeometry.CreateEllipse(ds, point.Position, 4 * rescale, 4 * rescale)
                                );
                            }
                        }
                        else
                        {
                            if (ReferenceEquals(point, start))
                            {
                            }
                            else if (point.Selected)
                            {
                                var r = 4.25f * rescale;
                                selectedOnPathB.AddGeometry(
                                    CanvasGeometry.CreateRectangle(ds, point.X - r, point.Y - r, 2 * r, 2 * r)
                                );
                            }
                            else
                            {
                                var r = 3.25f * rescale;
                                onPathB.AddGeometry(
                                    CanvasGeometry.CreateRectangle(ds, point.X - r, point.Y - r, 2 * r, 2 * r)
                                );
                            }
                        }
                    }
                    prev = point;
                }
            }

            // markers
            // ...
            // handles
            ds.DrawGeometry(CanvasGeometry.CreatePath(handlePathB), otherColor, strokeWidth: rescale);
            // on curves
            ds.FillGeometry(CanvasGeometry.CreatePath(selectedOnPathB), onColor);
            ds.FillGeometry(CanvasGeometry.CreatePath(selectedSmoothPathB), smoothColor);
            ds.DrawGeometry(CanvasGeometry.CreatePath(onPathB), onColor, strokeWidth: 1.3f * rescale);
            ds.DrawGeometry(CanvasGeometry.CreatePath(smoothPathB), smoothColor, strokeWidth: 1.3f * rescale);
            // notch
            ds.DrawGeometry(CanvasGeometry.CreatePath(notchPathB), notchColor, strokeWidth: rescale);
            // off curves
            using (var offPath = CanvasGeometry.CreatePath(offPathB))
            {
                ds.FillGeometry(offPath, backgroundColor);
                ds.DrawGeometry(offPath, offColor, strokeWidth: 1.3f * rescale);
            }
            ds.FillGeometry(CanvasGeometry.CreatePath(selectedOffPathB), offColor);
        }

        public static void DrawStroke(Data.Layer layer, CanvasDrawingSession ds, float rescale)
        {
            ds.DrawGeometry(layer.CanvasPath, Color.FromArgb(255, 34, 34, 34), strokeWidth: rescale);
        }
    }
}
