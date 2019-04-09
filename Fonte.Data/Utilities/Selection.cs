/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data.Utilities
{
    using System.Collections.Generic;

    public class Selection
    {
        public static List<Path> FilterSelection(IEnumerable<Path> paths, bool invert = false)
        {
            var selValue = !invert;
            var outPaths = new List<Path>();
            foreach (var path in paths)
            {
                var segments = path.Segments;
                Segment firstSegment;
                {
                    var enumerator = segments.GetEnumerator();
                    enumerator.MoveNext();
                    firstSegment = enumerator.Current;
                }

                IEnumerable<Segment> iter;
                if (!path.IsOpen && firstSegment.OnCurve.Selected == selValue)
                {
                    var segmentsList = new List<Segment>(segments);
                    var firstIndex = 0;

                    for (int ix = segmentsList.Count - 1; ix >= 0; --ix)
                    {
                        if (segmentsList[ix].OnCurve.Selected != selValue)
                        {
                            break;
                        }
                        firstIndex -= 1;
                    }
                    if (firstIndex == -segmentsList.Count)
                    {
                        // All selected, bring on the original path
                        outPaths.Add(path);
                        continue;
                    }
                    else
                    {
                        firstIndex += segmentsList.Count;
                        iter = Iterable.IterAt(segmentsList, firstIndex);
                    }
                }
                else
                {
                    iter = segments;
                }

                Path outPath = null;
                Segment? prev = null;
                var prevSelected = false;
                foreach (var segment in iter)
                {
                    var selected = segment.OnCurve.Selected == selValue;
                    if (selected)
                    {
                        if (prevSelected)
                        {
                            outPath._points.AddRange(segment.Points);
                        }
                        else
                        {
                            Point point;
                            outPath = new Path();
                            if (invert)
                            {
                                outPath.Parent = path.Parent;
                                point = segment.OnCurve;
                            }
                            else
                            {
                                point = segment.OnCurve.Clone();
                            }
                            point.Smooth = false;
                            point.Type = PointType.Move;
                            outPath._points.Add(point);
                        }
                    }
                    else
                    {
                        if (prevSelected)
                        {
                            if (invert)
                            {
                                prev.Value.OnCurve.Smooth = false;
                            }
                            // We got a path, export it and move on
                            outPaths.Add(outPath);
                            outPath = null;
                        }
                    }
                    prev = segment;
                    prevSelected = selected;
                }
                if (prevSelected)
                {
                    outPaths.Add(outPath);
                }
            }

            return outPaths;
        }
    }
}
