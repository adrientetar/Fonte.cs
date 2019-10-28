// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Utilities
{
    using Fonte.Data.Utilities;
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Brushes;
    using Microsoft.Graphics.Canvas.Geometry;
    using Microsoft.Graphics.Canvas.Text;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using Windows.Foundation;
    using Windows.UI;

    public static class Drawing
    {
        const float PI_1_8 = 1f / 8 * MathF.PI;
        const float PI_3_8 = 3f / 8 * MathF.PI;
        const float PI_5_8 = 5f / 8 * MathF.PI;
        const float PI_7_8 = 7f / 8 * MathF.PI;
        const float PI_9_8 = 9f / 8 * MathF.PI;
        const float PI_11_8 = 11f / 8 * MathF.PI;
        const float PI_13_8 = 13f / 8 * MathF.PI;
        const float PI_15_8 = 15f / 8 * MathF.PI;

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
                using (var geometry = CreateLozenge(ds, anchor.X, anchor.Y, anchor.IsSelected ? selectedSize : size))
                {
                    ds.FillGeometry(geometry, color);
                }

                if (anchor.IsSelected && !string.IsNullOrEmpty(anchor.Name))
                {
                    DrawText(ds, anchor.Name, anchor.ToVector2() + margin, Colors.White, rescale: rescale,
                             hAlignment: CanvasHorizontalAlignment.Left, vAlignment: CanvasVerticalAlignment.Center, backplateColor: Color.FromArgb(135, 45, 45, 45));
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

        public static void DrawCoordinates(Data.Layer layer, CanvasDrawingSession ds, float rescale)
        {
            var color = Color.FromArgb(255, 21, 116, 212);
            var canvasPath = layer.ClosedCanvasPath;
            var margin = 8 * rescale;

            foreach (var path in layer.Paths)
            {
                foreach (var (point, angle) in UIBroker.GetCurvePointsPreferredAngle(path))
                {
                    var pos = point.ToVector2() + margin * Conversion.ToVector(angle);

                    CanvasHorizontalAlignment hAlignment;
                    if (PI_5_8 <= angle && angle < PI_11_8)
                    {
                        hAlignment = CanvasHorizontalAlignment.Right;
                    }
                    // No && condition here, because we check around 0 or around 360 deg
                    else if (angle < PI_3_8 || PI_13_8 <= angle)
                    {
                        hAlignment = CanvasHorizontalAlignment.Left;
                    }
                    else
                    {
                        hAlignment = CanvasHorizontalAlignment.Center;
                    }

                    float? baseline;
                    CanvasVerticalAlignment vAlignment;
                    if (PI_1_8 <= angle && angle < PI_7_8)
                    {
                        baseline = null;
                        vAlignment = CanvasVerticalAlignment.Bottom;
                    }
                    else if (PI_9_8 <= angle && angle < PI_15_8)
                    {
                        baseline = .78f;
                        vAlignment = CanvasVerticalAlignment.Top;
                    }
                    else
                    {
                        baseline = .93f;
                        vAlignment = CanvasVerticalAlignment.Center;
                    }

                    var rx = MathF.Round(point.X, 1);
                    var ry = MathF.Round(point.Y, 1);
                    DrawText(ds, $"{rx}, {ry}", pos, color, fontSize: 10, hAlignment: hAlignment, vAlignment: vAlignment, baseline: baseline, rescale: rescale);
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
                    var direction = Conversion.ToVector(
                        Conversion.FromDegrees(guideline.Angle));

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

            using var handles = new CanvasPathBuilder(ds);
            using var markers = new CanvasPathBuilder(ds);
            using var notches = new CanvasPathBuilder(ds);
            using var cornerPoints = new CanvasPathBuilder(ds);
            using var selectedCornerPoints = new CanvasPathBuilder(ds);
            using var smoothPoints = new CanvasPathBuilder(ds);
            using var selectedSmoothPoints = new CanvasPathBuilder(ds);
            using var controlPoints = new CanvasPathBuilder(ds);
            using var selectedControlPoints = new CanvasPathBuilder(ds);
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
                    var angle = MathF.Atan2(next.Y - start.Y, next.X - start.X);
                    if (start.IsSelected)
                    {
                        using var geometry = CreateArrowhead(ds, start.X, start.Y, angle, 9 * rescale);

                        (start.IsSmooth ? selectedSmoothPoints : selectedCornerPoints).AddGeometry(geometry);
                    }
                    else
                    {
                        using var geometry = CreateArrowhead(ds, start.X, start.Y, angle, 7 * rescale);

                        (start.IsSmooth ? smoothPoints : cornerPoints).AddGeometry(geometry);
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
                            using var geometry = CanvasGeometry.CreateCircle(ds, point.X, point.Y, 4 * rescale);

                            selectedControlPoints.AddGeometry(geometry);
                        }
                        else
                        {
                            using var geometry = CanvasGeometry.CreateCircle(ds, point.X, point.Y, 3 * rescale);

                            controlPoints.AddGeometry(geometry);
                        }
                    }
                    else
                    {
                        if (point.IsSmooth)
                        {
                            var notchSize = 1.4f * rescale;
                            var pos = point.ToVector2();
                            var angle = MathF.Atan2(point.Y - prev.Y, point.X - prev.X) - Ops.PI_1_2;
                            var direction = Conversion.ToVector(angle);

                            notches.BeginFigure(
                                pos - direction * notchSize, CanvasFigureFill.Default);
                            notches.AddLine(
                                pos + direction * notchSize);
                            notches.EndFigure(CanvasFigureLoop.Open);

                            if (ReferenceEquals(point, start))
                            {
                            }
                            else if (point.IsSelected)
                            {
                                using var geometry = CanvasGeometry.CreateCircle(ds, point.X, point.Y, 5.15f * rescale);

                                selectedSmoothPoints.AddGeometry(geometry);
                            }
                            else
                            {
                                using var geometry = CanvasGeometry.CreateCircle(ds, point.X, point.Y, 4 * rescale);

                                smoothPoints.AddGeometry(geometry);
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
                                using var geometry = CanvasGeometry.CreateRectangle(ds, point.X - r, point.Y - r, 2 * r, 2 * r);

                                selectedCornerPoints.AddGeometry(geometry);
                            }
                            else
                            {
                                var r = 3.25f * rescale;
                                using var geometry = CanvasGeometry.CreateRectangle(ds, point.X - r, point.Y - r, 2 * r, 2 * r);

                                cornerPoints.AddGeometry(geometry);
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
                                        using var geometry = CreateLozenge(ds, point.X, point.Y, point.IsSelected ? 20 * rescale : 17 * rescale);

                                        markers.AddGeometry(geometry);
                                    }
                                    else
                                    {
                                        var size = point.IsSelected ? 8 * rescale : 7 * rescale;
                                        using var geometry = CanvasGeometry.CreateCircle(ds, point.X, point.Y, size);

                                        markers.AddGeometry(geometry);
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
            using (var handlesGeometry = CanvasGeometry.CreatePath(handles))
            {
                ds.DrawGeometry(handlesGeometry, otherColor, strokeWidth: rescale);
            }
            // on curves
            selectedCornerPoints.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            using (var selectedCornerPointsGeometry = CanvasGeometry.CreatePath(selectedCornerPoints))
            {
                ds.FillGeometry(selectedCornerPointsGeometry, cornerPointColor);
            }
            selectedSmoothPoints.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            using (var selectedSmoothPointsGeometry = CanvasGeometry.CreatePath(selectedSmoothPoints))
            {
                ds.FillGeometry(selectedSmoothPointsGeometry, smoothPointColor);
            }
            using (var cornerPointsGeometry = CanvasGeometry.CreatePath(cornerPoints))
            {
                ds.DrawGeometry(cornerPointsGeometry, cornerPointColor, strokeWidth: 1.3f * rescale);
            }
            using (var smoothPointsGeometry = CanvasGeometry.CreatePath(smoothPoints))
            {
                ds.DrawGeometry(smoothPointsGeometry, smoothPointColor, strokeWidth: 1.3f * rescale);
            }
            // notches
            using (var notchesGeometry = CanvasGeometry.CreatePath(notches))
            {
                ds.DrawGeometry(notchesGeometry, notchColor, strokeWidth: rescale);
            }
            // off curves
            controlPoints.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            using (var controlPointsGeometry = CanvasGeometry.CreatePath(controlPoints))
            {
                ds.FillGeometry(controlPointsGeometry, backgroundColor);
                ds.DrawGeometry(controlPointsGeometry, controlPointColor, strokeWidth: 1.3f * rescale);
            }
            selectedControlPoints.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            using (var selectedControlPointsGeometry = CanvasGeometry.CreatePath(selectedControlPoints))
            {
                ds.FillGeometry(selectedControlPointsGeometry, controlPointColor);
            }
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
                var rect = layer.SelectionBounds.ToFoundationRect();
                ds.DrawRectangle(rect, Color.FromArgb(45, 150, 150, 150), strokeWidth: rescale);
                using (var strokeStyle = new CanvasStrokeStyle
                {
                    CustomDashStyle = new float[] { 1, 4 }
                })
                {
                    ds.DrawRectangle(rect, Color.FromArgb(128, 34, 34, 34), strokeWidth: rescale, strokeStyle: strokeStyle);
                }

                var halfSize = 3.5f * rescale;
                var size = 2f * halfSize;
                var halfStrokeWidth = .5f * rescale;
                var strokeWidth = 2f * halfStrokeWidth;

                var color = Color.FromArgb(195, 255, 255, 255);
                using var borderBrush = new CanvasLinearGradientBrush(
                    ds,
                    Color.FromArgb(225, 160, 160, 160),
                    Color.FromArgb(225, 135, 135, 135)
                );
                using var geometry = CreateOuterRoundedRect(ds, 0, 0, size: size, strokeWidth: rescale);

                foreach (var handle in UIBroker.GetSelectionHandles(layer, rescale))
                {
                    borderBrush.StartPoint = new Vector2(
                            handle.Position.X, handle.Position.Y + halfSize + halfStrokeWidth);
                    borderBrush.EndPoint = new Vector2(
                        handle.Position.X, handle.Position.Y - halfSize - halfStrokeWidth);

                    ds.FillRectangle(handle.Position.X - halfSize + halfStrokeWidth,
                                        handle.Position.Y - halfSize + halfStrokeWidth,
                                        size - strokeWidth,
                                        size - strokeWidth,
                                        color);
                    ds.FillGeometry(geometry, handle.Position, borderBrush);
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
                var ch = Conversion.FromUnicode(glyph.Unicode);
                var color = Color.FromArgb(102, 192, 192, 192);
                var height = layer.Parent?.Parent.UnitsPerEm ?? 1000;

                DrawText(ds, ch, new Vector2(.5f * layer.Width, 0), color, fontSize: height, vAlignment: null);
            }
        }

        /**/

        public static void DrawText(CanvasDrawingSession ds, string text, Vector2 point, Color color, float? rescale = null, float fontSize = 12,
                                    CanvasHorizontalAlignment hAlignment = CanvasHorizontalAlignment.Center, CanvasVerticalAlignment? vAlignment = CanvasVerticalAlignment.Center,
                                    float? baseline = null, Color? backplateColor = null)
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

            using var textFormat = new CanvasTextFormat
            {
                FontFamily = "Segoe UI",
                FontSize = fontSize,
                HorizontalAlignment = hAlignment
            };
            if (vAlignment.HasValue)
            {
                textFormat.VerticalAlignment = vAlignment.Value;

                if (baseline.HasValue)
                {
                    textFormat.LineSpacingMode = CanvasLineSpacingMode.Proportional;
                    textFormat.LineSpacing = 1;
                    textFormat.LineSpacingBaseline = baseline.Value;
                }
            }
            else
            {
                textFormat.LineSpacing = 1;
                textFormat.LineSpacingBaseline = 0;
            }

            try
            {
                ds.Transform = t;
                if (backplateColor is Color)
                {
                    using var textLayout = new CanvasTextLayout(ds, text, textFormat, 0, 0)
                    {
                        WordWrapping = CanvasWordWrapping.NoWrap
                    };
                    var rect = InflateBy(textLayout.LayoutBounds, 4, -2, 4, 0);

                    ds.FillRoundedRectangle(rect, .5f * fontSize, .5f * fontSize, backplateColor.Value);
                    ds.DrawTextLayout(textLayout, Vector2.Zero, color);
                }
                else
                {
                    ds.DrawText(text, Vector2.Zero, color, textFormat);
                }
            }
            finally
            {
                ds.Transform = ot;
            }
        }

        static CanvasGeometry CreateArrowhead(CanvasDrawingSession ds, float x, float y, float angle, float size)
        {
            var thirdSize = .33f * size;
            var cos = MathF.Cos(angle);
            var sin = MathF.Sin(angle);
            using var builder = new CanvasPathBuilder(ds);

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

        static CanvasGeometry CreateLozenge(CanvasDrawingSession ds, float x, float y, float size)
        {
            var halfSize = .5f * size;
            using var builder = new CanvasPathBuilder(ds);

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

        static CanvasGeometry CreateOuterRoundedRect(CanvasDrawingSession ds, float x, float y, float size, float strokeWidth)
        {
            var halfSize = .5f * size;
            var halfStrokeWidth = .5f * strokeWidth;
            var radius = 1.5f * strokeWidth;

            using CanvasGeometry roundedRectangle = CanvasGeometry.CreateRoundedRectangle(ds,
                                                                                          x - halfSize,
                                                                                          y - halfSize,
                                                                                          size, size,
                                                                                          radius, radius),
                                 innerRectangle = CanvasGeometry.CreateRectangle(ds,
                                                                                 x - halfSize + halfStrokeWidth,
                                                                                 y - halfSize + halfStrokeWidth,
                                                                                 size - strokeWidth,
                                                                                 size - strokeWidth);

            return roundedRectangle.Stroke(strokeWidth).CombineWith(innerRectangle,
                                                                    Matrix3x2.Identity,
                                                                    CanvasGeometryCombine.Exclude);
        }

        static Rect InflateBy(Rect rect, int left, int top, int right, int bottom)
        {
            rect.X -= left;
            rect.Y -= top;

            rect.Width += left + right;
            rect.Height += top + bottom;

            return rect;
        }
    }
}
