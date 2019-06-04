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

        public static void DrawComponents(Data.Layer layer, CanvasDrawingSession ds, float rescale, Color componentColor, bool drawSelection)
        {
            var val = (byte)Math.Max(componentColor.R - 90, 0);
            var selectedComponentColor = Color.FromArgb(135, val, val, val);
            foreach (var component in layer.Components)
            {
                ds.FillGeometry(component.ClosedCanvasPath, drawSelection && component.Selected ? selectedComponentColor : componentColor);
                ds.DrawGeometry(component.OpenCanvasPath, componentColor);

                // TODO: disable on sizes < MinDetails
                var origin = component.Origin;
                ds.DrawLine(origin.X, origin.Y + 5 * rescale, origin.X, origin.Y, componentColor);
                ds.DrawLine(origin.X, origin.Y, origin.X + 4.5f * rescale, origin.Y, componentColor);
            }
        }

        public static void DrawFill(Data.Layer layer, CanvasDrawingSession ds, float rescale, Color color)
        {
            ds.FillGeometry(layer.ClosedCanvasPath, color);
            // Also stroke open paths here, since they don't fill
            ds.DrawGeometry(layer.OpenCanvasPath, Color.FromArgb(255, 34, 34, 34), strokeWidth: rescale);
        }

        public static void DrawMetrics(Data.Layer layer, CanvasDrawingSession ds, float rescale)
        {
            if (layer.Master is Data.Master master)
            {
                var color = Color.FromArgb(255, 204, 206, 200);

                var ascender = master.Ascender;
                var capHeight = master.CapHeight;
                var descender = master.Descender;
                var xHeight = master.XHeight;

                var width = layer.Width;
                var hi = Math.Max(ascender, capHeight);

                ds.DrawLine(0, ascender, width, ascender, color, strokeWidth: rescale);
                ds.DrawLine(0, capHeight, width, capHeight, color, strokeWidth: rescale);
                ds.DrawLine(0, xHeight, width, xHeight, color, strokeWidth: rescale);
                ds.DrawLine(0, 0, width, 0, color, strokeWidth: rescale);
                ds.DrawLine(0, descender, width, descender, color, strokeWidth: rescale);

                ds.DrawLine(0, hi, 0, descender, color, strokeWidth: rescale);
                ds.DrawLine(width, hi, width, descender, color, strokeWidth: rescale);
            }
        }

        public static void DrawPoints(Data.Layer layer, CanvasDrawingSession ds, float rescale,
                                      Color pointColor, Color smoothPointColor)
        {
            // save these in the Drawing class state? or in a context class/struct
            var backgroundColor = Color.FromArgb(255, 255, 255, 255);
            var notchColor = Color.FromArgb(255, 68, 68, 68);
            var controlPointColor = Color.FromArgb(255, 116, 116, 116);
            var otherColor = Color.FromArgb(240, 140, 140, 140);

            var handlePath = new CanvasPathBuilder(ds);
            var notchPath = new CanvasPathBuilder(ds);
            var pointPath = new CanvasPathBuilder(ds);
            var selectedPointPath = new CanvasPathBuilder(ds);
            var smoothPointPath = new CanvasPathBuilder(ds);
            var selectedSmoothPointPath = new CanvasPathBuilder(ds);
            var controlPointPath = new CanvasPathBuilder(ds);
            var selectedControlPointPath = new CanvasPathBuilder(ds);
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
                        (start.Smooth ? selectedSmoothPointPath : selectedPointPath).AddGeometry(
                            CreateTriangle(ds, start.X, start.Y, angle, 9 * rescale)
                        );
                    }
                    else
                    {
                        (start.Smooth ? smoothPointPath : pointPath).AddGeometry(
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
                        handlePath.BeginFigure(prev.X, prev.Y);
                        handlePath.AddLine(point.X, point.Y);
                        handlePath.EndFigure(CanvasFigureLoop.Open);
                    }
                    breakHandle = false;

                    if (isOffCurve)
                    {
                        if (point.Selected)
                        {
                            selectedControlPointPath.AddGeometry(
                                CanvasGeometry.CreateEllipse(ds, point.X, point.Y, 4 * rescale, 4 * rescale)
                            );
                        }
                        else
                        {
                            controlPointPath.AddGeometry(
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
                            notchPath.BeginFigure(
                                point.X - cos * notchSize, point.Y - sin * notchSize
                            );
                            notchPath.AddLine(
                                point.X + cos * notchSize, point.Y + sin * notchSize
                            );
                            notchPath.EndFigure(CanvasFigureLoop.Open);

                            if (ReferenceEquals(point, start))
                            {
                            }
                            else if (point.Selected)
                            {
                                selectedSmoothPointPath.AddGeometry(
                                    CanvasGeometry.CreateEllipse(ds, point.X, point.Y, 5.15f * rescale, 5.15f * rescale)
                                );
                            }
                            else
                            {
                                smoothPointPath.AddGeometry(
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
                                selectedPointPath.AddGeometry(
                                    CanvasGeometry.CreateRectangle(ds, point.X - r, point.Y - r, 2 * r, 2 * r)
                                );
                            }
                            else
                            {
                                var r = 3.25f * rescale;
                                pointPath.AddGeometry(
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
            ds.DrawGeometry(CanvasGeometry.CreatePath(handlePath), otherColor, strokeWidth: rescale);
            // on curves
            selectedPointPath.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            ds.FillGeometry(CanvasGeometry.CreatePath(selectedPointPath), pointColor);
            selectedSmoothPointPath.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            ds.FillGeometry(CanvasGeometry.CreatePath(selectedSmoothPointPath), smoothPointColor);
            ds.DrawGeometry(CanvasGeometry.CreatePath(pointPath), pointColor, strokeWidth: 1.3f * rescale);
            ds.DrawGeometry(CanvasGeometry.CreatePath(smoothPointPath), smoothPointColor, strokeWidth: 1.3f * rescale);
            // notch
            ds.DrawGeometry(CanvasGeometry.CreatePath(notchPath), notchColor, strokeWidth: rescale);
            // off curves
            controlPointPath.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            using (var controlPointGeometry = CanvasGeometry.CreatePath(controlPointPath))
            {
                ds.FillGeometry(controlPointGeometry, backgroundColor);
                ds.DrawGeometry(controlPointGeometry, controlPointColor, strokeWidth: 1.3f * rescale);
            }
            selectedControlPointPath.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            ds.FillGeometry(CanvasGeometry.CreatePath(selectedControlPointPath), controlPointColor);
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
                ds.DrawRectangle(rect.ToFoundationRect(), Color.FromArgb(128, 34, 34, 34), rescale, strokeStyle);

                var pathBuilder = new CanvasPathBuilder(ds);
                var radius = 4 * rescale;
                var margin = 4;
                var loX = rect.Left - radius - margin;
                var loY = rect.Bottom - radius - margin;
                var hiX = rect.Right + radius + margin;
                var hiY = rect.Top + radius + margin;
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
            // Open paths are stroked in DrawFill
        }
    }
}
