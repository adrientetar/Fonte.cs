/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Utilities
{
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Geometry;
    using Microsoft.Graphics.Canvas.Text;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using Windows.UI;

    class Drawing
    {
        public static void DrawAnchors(Data.Layer layer, CanvasDrawingSession ds, float rescale, Color color)
        {
            var size = 9 * rescale;
            var selectedSize = 11 * rescale;
            var margin = new Vector2(
                selectedSize,
                rescale
            );

            foreach (var anchor in layer.Anchors)
            {
                ds.FillGeometry(
                    CreateLozenge(ds, anchor.X, anchor.Y, anchor.IsSelected ? selectedSize : size), color);

                if (anchor.IsSelected && !string.IsNullOrEmpty(anchor.Name))
                {
                    DrawText(ds, anchor.Name, anchor.ToVector2() + margin, Color.FromArgb(255, 35, 35, 35),
                             hAlignment: CanvasHorizontalAlignment.Left, rescale: rescale);
                }
            }
        }

        public static void DrawComponents(Data.Layer layer, CanvasDrawingSession ds, float rescale, Color componentColor,
                                          bool drawSelection = false)
        {
            var margin = 4 * rescale;
            var originColor = Color.FromArgb(135, 34, 34, 34);
            var selectedComponentColor = Color.FromArgb(
                135,
                (byte)Math.Max(componentColor.R - 90, 0),
                (byte)Math.Max(componentColor.G - 90, 0),
                (byte)Math.Max(componentColor.B - 90, 0)
            );

            foreach (var component in layer.Components)
            {
                ds.FillGeometry(component.ClosedCanvasPath, drawSelection && component.IsSelected ? selectedComponentColor : componentColor);
                ds.DrawGeometry(component.OpenCanvasPath, componentColor, strokeWidth: rescale);

                if (drawSelection && component.IsSelected)
                {
                    var origin = component.Origin;
                    ds.DrawLine(origin.X, origin.Y + margin, origin.X, origin.Y - margin, originColor, strokeWidth: rescale);
                    ds.DrawLine(origin.X - margin, origin.Y, origin.X + margin, origin.Y, originColor, strokeWidth: rescale);
                }
            }
        }

        public static void DrawFill(Data.Layer layer, CanvasDrawingSession ds, float rescale, Color color)
        {
            ds.FillGeometry(layer.ClosedCanvasPath, color);
        }

        public static void DrawGrid(Data.Layer layer, CanvasDrawingSession ds, float rescale, Data.Geometry.Rect drawingRect)
        {
            /*var color = Color.FromArgb(255, 220, 220, 220);
            var gridSize = 1;

            for (int i = gridSize * (int)(bottomLeft.X / gridSize); i <= topRight.X; i += gridSize)
            {
                ds.DrawLine(i, (float)topRight.Y, i, (float)bottomLeft.Y, color, strokeWidth: rescale);
            }
            for (int i = gridSize * (int)(bottomLeft.Y / gridSize); i <= topRight.Y; i += gridSize)
            {
                ds.DrawLine((float)bottomLeft.X, i, (float)topRight.X, i, color, strokeWidth: rescale);
            }*/
        }

        public static void DrawGuidelines(Data.Layer layer, CanvasDrawingSession ds, float rescale, Data.Geometry.Rect drawingRect)
        {
            var halfSize = 4 * rescale;
            var selectedHalfSize = 5 * rescale;

            (IEnumerable<Data.Guideline>, Color)[] drawingPlan = {
                (layer.Guidelines, Color.FromArgb(128, 56, 71, 213)),
                (UIBroker.GetMasterGuidelines(layer), Color.FromArgb(128, 255, 51, 51)),
            };

            // TODO: draw name
            foreach (var (guidelines, color) in drawingPlan)
            {
                var selectedColor = Color.FromArgb(190, color.R, color.G, color.B);

                foreach (var guideline in guidelines)
                {
                    var pos = guideline.ToVector2();
                    var direction = guideline.Direction;

                    ds.DrawLine(pos - direction * halfSize, pos - direction * 9999f, guideline.IsSelected ? selectedColor : color, strokeWidth: rescale);
                    ds.DrawLine(pos + direction * halfSize, pos + direction * 9999f, guideline.IsSelected ? selectedColor : color, strokeWidth: rescale);

                    if (guideline.IsSelected)
                    {
                        ds.FillCircle(guideline.ToVector2(), selectedHalfSize, selectedColor);
                    }
                    else
                    {
                        ds.DrawCircle(guideline.ToVector2(), halfSize, color, strokeWidth: rescale);
                    }
                }
            }
        }

        public static void DrawLayers(Data.Layer layer, CanvasDrawingSession ds, float rescale, Color color)
        {
            if (layer.Parent is Data.Glyph glyph)
            {
                foreach (var glyphLayer in glyph.Layers)
                {
                    if (glyphLayer != layer && glyphLayer.IsVisible)
                    {
                        foreach (var component in glyphLayer.Components)
                        {
                            ds.DrawGeometry(component.ClosedCanvasPath, color, strokeWidth: rescale);
                            ds.DrawGeometry(component.OpenCanvasPath, color, strokeWidth: rescale);
                        }

                        ds.DrawGeometry(glyphLayer.ClosedCanvasPath, color, strokeWidth: rescale);
                        ds.DrawGeometry(glyphLayer.OpenCanvasPath, color, strokeWidth: rescale);
                    }
                }
            }
        }

        public static void DrawMetrics(Data.Layer layer, CanvasDrawingSession ds, float rescale, Color? alignmentZoneColor = null)
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

                if (alignmentZoneColor is Color)
                {
                    foreach (var zone in master.AlignmentZones)
                    {
                        ds.FillRectangle(0, zone.Position, width, zone.Size, alignmentZoneColor.Value);
                    }
                }

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
                                      Color cornerPointColor, Color smoothPointColor, Color markerColor)
        {
            // save these in the Drawing class state? or in a context class/struct
            var backgroundColor = Color.FromArgb(255, 255, 255, 255);
            var notchColor = Color.FromArgb(255, 68, 68, 68);
            var controlPointColor = Color.FromArgb(255, 116, 116, 116);
            var otherColor = Color.FromArgb(240, 140, 140, 140);

            var master = layer.Master;

            var handles = new CanvasPathBuilder(ds);
            var markers = new CanvasPathBuilder(ds);
            var notches = new CanvasPathBuilder(ds);
            var cornerPoints = new CanvasPathBuilder(ds);
            var selectedCornerPoints = new CanvasPathBuilder(ds);
            var smoothPoints = new CanvasPathBuilder(ds);
            var selectedSmoothPoints = new CanvasPathBuilder(ds);
            var controlPoints = new CanvasPathBuilder(ds);
            var selectedControlPoints = new CanvasPathBuilder(ds);
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
                    if (start.IsSelected)
                    {
                        (start.IsSmooth ? selectedSmoothPoints : selectedCornerPoints).AddGeometry(
                            CreateTriangle(ds, start.X, start.Y, angle, 9 * rescale)
                        );
                    }
                    else
                    {
                        (start.IsSmooth ? smoothPoints : cornerPoints).AddGeometry(
                            CreateTriangle(ds, start.X, start.Y, angle, 7 * rescale)
                        );
                    }
                }
                else
                {
                    start = null;
                }

                var breakHandle = path.IsOpen;
                var prev = points[points.Count - 1];
                foreach (var point in path.Points)
                {
                    var isOffCurve = point.Type == Data.PointType.None;
                    if (!breakHandle && prev.Type == Data.PointType.None != isOffCurve)
                    {
                        handles.BeginFigure(prev.X, prev.Y);
                        handles.AddLine(point.X, point.Y);
                        handles.EndFigure(CanvasFigureLoop.Open);
                    }
                    breakHandle = false;

                    if (isOffCurve)
                    {
                        if (point.IsSelected)
                        {
                            selectedControlPoints.AddGeometry(
                                CanvasGeometry.CreateCircle(ds, point.X, point.Y, 4 * rescale)
                            );
                        }
                        else
                        {
                            controlPoints.AddGeometry(
                                CanvasGeometry.CreateCircle(ds, point.X, point.Y, 3 * rescale)
                            );
                        }
                    }
                    else
                    {
                        if (point.IsSmooth)
                        {
                            var angle = Math.Atan2(point.Y - prev.Y, point.X - prev.X) - .5 * Math.PI;
                            var cos = (float)Math.Cos(angle);
                            var sin = (float)Math.Sin(angle);
                            var notchSize = 1.4f * rescale;
                            notches.BeginFigure(
                                point.X - cos * notchSize, point.Y - sin * notchSize
                            );
                            notches.AddLine(
                                point.X + cos * notchSize, point.Y + sin * notchSize
                            );
                            notches.EndFigure(CanvasFigureLoop.Open);

                            if (ReferenceEquals(point, start))
                            {
                            }
                            else if (point.IsSelected)
                            {
                                selectedSmoothPoints.AddGeometry(
                                    CanvasGeometry.CreateCircle(ds, point.X, point.Y, 5.15f * rescale)
                                );
                            }
                            else
                            {
                                smoothPoints.AddGeometry(
                                    CanvasGeometry.CreateCircle(ds, point.X, point.Y, 4 * rescale)
                                );
                            }
                        }
                        else
                        {
                            if (ReferenceEquals(point, start))
                            {
                            }
                            else if (point.IsSelected)
                            {
                                var r = 4.25f * rescale;
                                selectedCornerPoints.AddGeometry(
                                    CanvasGeometry.CreateRectangle(ds, point.X - r, point.Y - r, 2 * r, 2 * r)
                                );
                            }
                            else
                            {
                                var r = 3.25f * rescale;
                                cornerPoints.AddGeometry(
                                    CanvasGeometry.CreateRectangle(ds, point.X - r, point.Y - r, 2 * r, 2 * r)
                                );
                            }
                        }

                        if (master != null)
                        {
                            foreach (var zone in master.AlignmentZones)
                            {
                                var lo = zone.Position;
                                var hi = zone.Position + zone.Size;
                                if (point.Y >= lo && point.Y <= hi)
                                {
                                    if (lo > 0 && point.Y == lo ||
                                        hi <= 0 && point.Y == hi)
                                    {
                                        markers.AddGeometry(
                                            CreateLozenge(ds, point.X, point.Y, point.IsSelected ? 20 * rescale : 17 * rescale)
                                        );
                                    }
                                    else
                                    {
                                        var size = point.IsSelected ? 8 * rescale : 7 * rescale;
                                        markers.AddGeometry(
                                            CanvasGeometry.CreateCircle(ds, point.X, point.Y, size)
                                        );
                                    }
                                }
                            }
                        }
                    }
                    prev = point;
                }
            }

            // markers
            markers.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            using (var markersGeometry = CanvasGeometry.CreatePath(markers))
            {
                ds.FillGeometry(markersGeometry, markerColor);
                ds.DrawGeometry(markersGeometry, Color.FromArgb(135, 255, 255, 255), strokeWidth: rescale);
            }
            // handles
            ds.DrawGeometry(CanvasGeometry.CreatePath(handles), otherColor, strokeWidth: rescale);
            // on curves
            selectedCornerPoints.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            ds.FillGeometry(CanvasGeometry.CreatePath(selectedCornerPoints), cornerPointColor);
            selectedSmoothPoints.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            ds.FillGeometry(CanvasGeometry.CreatePath(selectedSmoothPoints), smoothPointColor);
            ds.DrawGeometry(CanvasGeometry.CreatePath(cornerPoints), cornerPointColor, strokeWidth: 1.3f * rescale);
            ds.DrawGeometry(CanvasGeometry.CreatePath(smoothPoints), smoothPointColor, strokeWidth: 1.3f * rescale);
            // notches
            ds.DrawGeometry(CanvasGeometry.CreatePath(notches), notchColor, strokeWidth: rescale);
            // off curves
            controlPoints.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            using (var controlPointsGeometry = CanvasGeometry.CreatePath(controlPoints))
            {
                ds.FillGeometry(controlPointsGeometry, backgroundColor);
                ds.DrawGeometry(controlPointsGeometry, controlPointColor, strokeWidth: 1.3f * rescale);
            }
            selectedControlPoints.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            ds.FillGeometry(CanvasGeometry.CreatePath(selectedControlPoints), controlPointColor);
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
                foreach (var handle in UIBroker.GetSelectionHandles(layer, rescale))
                {
                    pathBuilder.AddGeometry(
                        CanvasGeometry.CreateCircle(ds, handle.Position, radius));
                }
                using (var path = CanvasGeometry.CreatePath(pathBuilder))
                {
                    ds.FillGeometry(path, Color.FromArgb(120, 255, 255, 255));
                    ds.DrawGeometry(path, Color.FromArgb(255, 163, 163, 163), rescale);
                }
            }
        }

        [Flags]
        public enum StrokePaths
        {
            Closed = 1 << 0,
            Open   = 1 << 1,
            ClosedOpen = Closed | Open
        };

        public static void DrawStroke(Data.Layer layer, CanvasDrawingSession ds, float rescale, Color strokeColor, StrokePaths stroke = StrokePaths.ClosedOpen)
        {
            if (stroke.HasFlag(StrokePaths.Closed)) ds.DrawGeometry(layer.ClosedCanvasPath, strokeColor, strokeWidth: rescale);
            if (stroke.HasFlag(StrokePaths.Open)) ds.DrawGeometry(layer.OpenCanvasPath, strokeColor, strokeWidth: rescale);
        }

        public static void DrawUnicode(Data.Layer layer, CanvasDrawingSession ds, float rescale)
        {
            if (layer.Parent is Data.Glyph glyph && !string.IsNullOrEmpty(glyph.Unicode))
            {
                var ch = Convert.ToChar(Convert.ToUInt32(glyph.Unicode, 16)).ToString();
                var color = Color.FromArgb(102, 192, 192, 192);
                var height = layer.Parent?.Parent.UnitsPerEm ?? 1000;

                DrawText(ds, ch, new Vector2(.5f * layer.Width, 0), color, height, CanvasHorizontalAlignment.Center, VerticalAlignment.Baseline);
            }
        }

        enum VerticalAlignment
        {
            Center,
            Baseline
        };

        static void DrawText(CanvasDrawingSession ds, string text, Vector2 point, Color color, float fontSize = 12,
                             CanvasHorizontalAlignment hAlignment = CanvasHorizontalAlignment.Center, VerticalAlignment vAlignment = VerticalAlignment.Center,
                             float? rescale = null)
        {
            var ot = ds.Transform;
            var t = ds.Transform;

            if (rescale is float)
            {
                t.M11 = t.M22 = rescale.Value;
            }
            else
            {
                t.M22 = -t.M22;
            }
            t.Translation += new Vector2(point.X, -point.Y);

            var format = new CanvasTextFormat
            {
                FontFamily = "Segoe UI",
                FontSize = fontSize,
                HorizontalAlignment = hAlignment
            };
            if (vAlignment.HasFlag(VerticalAlignment.Baseline))
            {
                format.LineSpacing = 1;
                format.LineSpacingBaseline = 0;
            }
            else
            {
                format.VerticalAlignment = CanvasVerticalAlignment.Center;
            }

            try
            {
                ds.Transform = t;
                ds.DrawText(text, Vector2.Zero, color, format);
            }
            finally
            {
                ds.Transform = ot;
            }
        }

        static CanvasGeometry CreateLozenge(CanvasDrawingSession ds, float x, float y, float size)
        {
            var halfSize = .5f * size;
            var builder = new CanvasPathBuilder(ds);
            builder.BeginFigure(new Vector2(
                x - halfSize,
                y
            ));
            builder.AddLine(new Vector2(
                x,
                y + halfSize
            ));
            builder.AddLine(new Vector2(
                x + halfSize,
                y
            ));
            builder.AddLine(new Vector2(
                x,
                y - halfSize
            ));
            builder.EndFigure(CanvasFigureLoop.Closed);
            return CanvasGeometry.CreatePath(builder);
        }

        static CanvasGeometry CreateTriangle(CanvasDrawingSession ds, float x, float y, double angle, float size)
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
    }
}
