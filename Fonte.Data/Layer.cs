/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Geometry;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Numerics;
    using Windows.Foundation;

    public partial class Layer
    {
        // TODO caching

        public Rect Bounds
        {
            get
            {
                return CanvasPath.ComputeBounds();
            }
        }

        public CanvasGeometry CanvasPath
        {
            get
            {
                var device = CanvasDevice.GetSharedDevice();
                var builder = new CanvasPathBuilder(device);

                var stack = new Vector2[2];
                var stackIndex = 0;
                foreach (var path in Paths) {
                    var start = path.Points[0];
                    var skip = start.Type == PointType.Move;
                    if (!skip) {
                        start = path.Points[path.Points.Count - 1];
                    }
                    builder.BeginFigure(start.Position);

                    foreach (var point in path.Points)
                    {
                        if (skip) {
                            skip = false;
                            continue;
                        }
                        switch (point.Type) {
                            case PointType.Curve:
                                Debug.Assert(stackIndex == 2);
                                builder.AddCubicBezier(stack[0], stack[1], point.Position);
                                stackIndex = 0;
                                break;
                            case PointType.Line:
                                builder.AddLine(point.Position);
                                break;
                            case PointType.None:
                                stack[stackIndex++] = point.Position;
                                break;
                        }
                    }

                    builder.EndFigure(start.Type == PointType.Move? CanvasFigureLoop.Open : CanvasFigureLoop.Closed);
                }

                return CanvasGeometry.CreatePath(builder);
            }
        }

        // could just store an actual reference to the master,
        // and serialize as masterName
        [JsonProperty("masterName")]
        public string MasterName { get; set; }

        [JsonProperty("width")]
        public long Width { get; set; }

        [JsonProperty("paths")]
        public Path[] Paths { get; set; }
    }
}
