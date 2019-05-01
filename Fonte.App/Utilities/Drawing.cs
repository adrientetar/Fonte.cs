/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Utilities
{
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Geometry;

    using System;
    using System.Numerics;
    using Windows.UI;

    class Drawing
    {
        public static CanvasGeometry CreateTriangle(CanvasDrawingSession ds, float x, float y, double angle, float size)
        {
            var thirdSize = .33f * size;
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);
            var builder = new CanvasPathBuilder(ds);
            builder.BeginFigure(new Vector2(
                x - cos * thirdSize - sin * size, // -thirdSize
                y + cos * size - sin * thirdSize  //  size
            ));
            builder.AddLine(new Vector2(
                x - cos * thirdSize + sin * size, // -thirdSize
                y - cos * size - sin * thirdSize  // -size
            ));
            builder.AddLine(new Vector2(
                x + 2 * cos * thirdSize,          //  thirdSize * 2
                y + 2 * sin * thirdSize           //  0
            ));
            builder.EndFigure(CanvasFigureLoop.Closed);
            return CanvasGeometry.CreatePath(builder);
        }

        public static void DrawComponents(Data.Layer layer, CanvasDrawingSession ds, float rescale)
        {
            throw new NotImplementedException();
        }

        public static void DrawFill(Data.Layer layer, CanvasDrawingSession ds, float rescale)
        {
            ds.FillGeometry(layer.ClosedCanvasPath, Color.FromArgb(77, 244, 244, 244));
            ds.FillGeometry(layer.OpenCanvasPath, Color.FromArgb(77, 244, 244, 244));
        }

        public static void DrawMetrics(Data.Layer layer, CanvasDrawingSession ds, float rescale)
        {
            throw new NotImplementedException();
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
                            CreateTriangle(ds, start.X, start.Y, angle, 9 * rescale)
                        );
                    }
                    else
                    {
                        (start.Smooth ? smoothPathB : onPathB).AddGeometry(
                            CreateTriangle(ds, start.X, start.Y, angle, 7 * rescale)
                        );
                    }
                }
                else
                {
                    start = null;
                }

                var breakHandle = path.IsOpen;
                Data.Point prev = points[points.Count - 1];
                foreach (var point in path.Points)
                {
                    var isOffCurve = point.Type == Data.PointType.None;
                    if (!breakHandle && prev.Type == Data.PointType.None != isOffCurve)
                    {
                        handlePathB.BeginFigure(prev.X, prev.Y);
                        handlePathB.AddLine(point.X, point.Y);
                        handlePathB.EndFigure(CanvasFigureLoop.Open);
                    }
                    breakHandle = false;

                    if (isOffCurve)
                    {
                        if (point.Selected)
                        {
                            selectedOffPathB.AddGeometry(
                                CanvasGeometry.CreateEllipse(ds, point.X, point.Y, 4 * rescale, 4 * rescale)
                            );
                        }
                        else
                        {
                            offPathB.AddGeometry(
                                CanvasGeometry.CreateEllipse(ds, point.X, point.Y, 3 * rescale, 3 * rescale)
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
                                    CanvasGeometry.CreateEllipse(ds, point.X, point.Y, 5.15f * rescale, 5.15f * rescale)
                                );
                            }
                            else
                            {
                                smoothPathB.AddGeometry(
                                    CanvasGeometry.CreateEllipse(ds, point.X, point.Y, 4 * rescale, 4 * rescale)
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
            selectedOnPathB.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            ds.FillGeometry(CanvasGeometry.CreatePath(selectedOnPathB), onColor);
            selectedSmoothPathB.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            ds.FillGeometry(CanvasGeometry.CreatePath(selectedSmoothPathB), smoothColor);
            ds.DrawGeometry(CanvasGeometry.CreatePath(onPathB), onColor, strokeWidth: 1.3f * rescale);
            ds.DrawGeometry(CanvasGeometry.CreatePath(smoothPathB), smoothColor, strokeWidth: 1.3f * rescale);
            // notch
            ds.DrawGeometry(CanvasGeometry.CreatePath(notchPathB), notchColor, strokeWidth: rescale);
            // off curves
            offPathB.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            using (var offPath = CanvasGeometry.CreatePath(offPathB))
            {
                ds.FillGeometry(offPath, backgroundColor);
                ds.DrawGeometry(offPath, offColor, strokeWidth: 1.3f * rescale);
            }
            selectedOffPathB.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            ds.FillGeometry(CanvasGeometry.CreatePath(selectedOffPathB), offColor);
        }

        public static void DrawSelection(Data.Layer layer, CanvasDrawingSession ds, float rescale)
        {
            foreach (var path in layer.SelectedPaths)
            {
                ds.DrawGeometry(path.CanvasPath, Color.FromArgb(155, 145, 170, 196), strokeWidth: 3.5f * rescale);
            }
        }

        public static void DrawSelectionBounds(Data.Layer layer, CanvasDrawingSession ds, float rescale)
        {
            if (layer.Selection.Count > 1)
            {
                var rect = layer.SelectionBounds;
                var strokeStyle = new CanvasStrokeStyle
                {
                    CustomDashStyle = new float[] { 1, 4 }
                };
                ds.DrawRectangle(rect, Color.FromArgb(128, 34, 34, 34), rescale, strokeStyle);

                var pathBuilder = new CanvasPathBuilder(ds);
                var radius = 4 * rescale;
                var margin = 4;
                var loX = (float)(rect.Left - radius - margin);
                var loY = (float)(rect.Top - radius - margin);
                var hiX = (float)(rect.Right + radius + margin);
                var hiY = (float)(rect.Bottom + radius + margin);
                if (rect.Width > 0 && rect.Height > 0)
                {
                    pathBuilder.AddGeometry(
                        CanvasGeometry.CreateCircle(ds, new Vector2(loX, loY), radius));
                    pathBuilder.AddGeometry(
                        CanvasGeometry.CreateCircle(ds, new Vector2(loX, hiY), radius));
                    pathBuilder.AddGeometry(
                        CanvasGeometry.CreateCircle(ds, new Vector2(hiX, hiY), radius));
                    pathBuilder.AddGeometry(
                        CanvasGeometry.CreateCircle(ds, new Vector2(hiX, loY), radius));
                }
                if (rect.Width > 0)
                {
                    var midY = .5f * (loY + hiY);
                    pathBuilder.AddGeometry(
                        CanvasGeometry.CreateCircle(ds, new Vector2(loX, midY), radius));
                    pathBuilder.AddGeometry(
                        CanvasGeometry.CreateCircle(ds, new Vector2(hiX, midY), radius));
                }
                if (rect.Height > 0)
                {
                    var midX = .5f * (loX + hiX);
                    pathBuilder.AddGeometry(
                        CanvasGeometry.CreateCircle(ds, new Vector2(midX, loY), radius));
                    pathBuilder.AddGeometry(
                        CanvasGeometry.CreateCircle(ds, new Vector2(midX, hiY), radius));
                }
                using (var path = CanvasGeometry.CreatePath(pathBuilder))
                {
                    ds.FillGeometry(path, Color.FromArgb(120, 255, 255, 255));
                    ds.DrawGeometry(path, Color.FromArgb(255, 163, 163, 163), rescale);
                }
            }
        }

        public static void DrawStroke(Data.Layer layer, CanvasDrawingSession ds, float rescale)
        {
            ds.DrawGeometry(layer.ClosedCanvasPath, Color.FromArgb(255, 34, 34, 34), strokeWidth: rescale);
            ds.DrawGeometry(layer.OpenCanvasPath, Color.FromArgb(255, 34, 34, 34), strokeWidth: rescale);
        }
    }
}
