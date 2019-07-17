/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Utilities
{
    using Fonte.Data.Geometry;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Numerics;

    class Misc
    {
        public struct BBoxHandle
        {
            public HandleKind Kind { get; }
            public Vector2 Position { get; }

            public BBoxHandle(HandleKind kind, Vector2 pos)
            {
                Kind = kind;
                Position = pos;
            }
        }

        [Flags]
        public enum HandleKind
        {
            Left        = 1 << 0,
            Top         = 1 << 1,
            Right       = 1 << 2,
            Bottom      = 1 << 3,
            TopLeft     = Top | Left,
            TopRight    = Top | Right,
            BottomRight = Bottom | Right,
            BottomLeft  = Bottom | Left,
        }

        public static BBoxHandle[] GetSelectionHandles(Data.Layer layer, float rescale)
        {
            var bounds = layer.SelectionBounds;
            var n = 0;
            if (bounds.Width > 0 && bounds.Height > 0) n += 4;
            if (bounds.Width > 0) n += 2;
            if (bounds.Height > 0) n += 2;
            var result = new BBoxHandle[n];
            var i = 0;

            if (bounds.Width > 0 && bounds.Height > 0)
            {
                result[i++] = GetSelectionHandle(bounds, HandleKind.BottomLeft, rescale);
                result[i++] = GetSelectionHandle(bounds, HandleKind.TopLeft, rescale);
                result[i++] = GetSelectionHandle(bounds, HandleKind.TopRight, rescale);
                result[i++] = GetSelectionHandle(bounds, HandleKind.BottomRight, rescale);
            }
            if (bounds.Width > 0)
            {
                result[i++] = GetSelectionHandle(bounds, HandleKind.Left, rescale);
                result[i++] = GetSelectionHandle(bounds, HandleKind.Right, rescale);
            }
            if (bounds.Height > 0)
            {
                result[i++] = GetSelectionHandle(bounds, HandleKind.Bottom, rescale);
                result[i++] = GetSelectionHandle(bounds, HandleKind.Top, rescale);
            }
            Debug.Assert(i == n);

            return result;
        }

        public static BBoxHandle GetSelectionHandle(Rect bounds, HandleKind kind, float rescale)
        {
            Vector2 pos;

            var radius = 4 * rescale;
            var margin = 4 * rescale;
            if (kind.HasFlag(HandleKind.Right))
            {
                pos.X = bounds.Right + radius + margin;
            }
            else if (kind.HasFlag(HandleKind.Left))
            {
                pos.X = bounds.Left - radius - margin;
            }
            else
            {
                pos.X = .5f * (bounds.Left + bounds.Right);
            }

            if (kind.HasFlag(HandleKind.Top))
            {
                pos.Y = bounds.Top + radius + margin;
            }
            else if (kind.HasFlag(HandleKind.Bottom))
            {
                pos.Y = bounds.Bottom - radius - margin;
            }
            else
            {
                pos.Y = .5f * (bounds.Bottom + bounds.Top);
            }
            return new BBoxHandle(kind, pos);
        }

        public struct GuidelineRule
        {
            public Data.Guideline Guideline { get; }

            public GuidelineRule(Data.Guideline guideline)
            {
                Guideline = guideline;
            }
        }

        public static IEnumerable<Data.Guideline> GetAllGuidelines(Data.Layer layer)
        {
            return Enumerable.Concat(layer.Guidelines, GetMasterGuidelines(layer));
        }

        public static IEnumerable<Data.Guideline> GetMasterGuidelines(Data.Layer layer)
        {
            return layer.Master?.Guidelines ?? Enumerable.Empty<Data.Guideline>();
        }

        public static Data.Guideline GetSelectedGuideline(Data.Layer layer)
        {
            var selection = layer.Selection;
            if (selection.Count > 1)
            {
            }
            else if (selection.Count > 0)
            {
                return layer.Selection.First() as Data.Guideline;
            }
            else if (layer.Master is Data.Master master)
            {
                return master.Guidelines.Where(g => g.IsSelected).FirstOrDefault();
            }
            return null;
        }
    }
}