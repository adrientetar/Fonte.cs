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
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using Windows.Foundation;

    public partial class Layer
    {
        private Rect _bounds = Rect.Empty;
        private CanvasGeometry _closedCanvasPath;
        private CanvasGeometry _openCanvasPath;
        private List<Path> _selectedPaths;
        private HashSet<Point> _selection;
        private Rect _selectionBounds = Rect.Empty;

        // could just store an actual reference to the master,
        // and serialize as masterName
        [JsonProperty("masterName")]
        public string MasterName { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("paths")]
        public ObservableList<Path> Paths { get; }

        /**/

        [JsonIgnore]
        public Rect Bounds
        {
            get
            {
                if (_bounds.IsEmpty)
                {
                    foreach (Path path in Paths)
                    {
                        _bounds.Union(path.Bounds);
                    }
                }
                return _bounds;
            }
        }

        [JsonIgnore]
        public CanvasGeometry ClosedCanvasPath
        {
            get
            {
                if (_closedCanvasPath == null)
                {
                    _closedCanvasPath = _collectPaths(path => !path.IsOpen);
                }
                return _closedCanvasPath;
            }
        }

        [JsonIgnore]
        public CanvasGeometry OpenCanvasPath
        {
            get
            {
                if (_openCanvasPath == null)
                {
                    _closedCanvasPath = _collectPaths(path => path.IsOpen);
                }
                return _openCanvasPath;
            }
        }

        [JsonIgnore]
        public Glyph Parent { get; internal set; }

        [JsonIgnore]
        public IReadOnlyList<Path> SelectedPaths
        {
            get
            {
                if (_selectedPaths == null)
                {
                    throw new NotImplementedException();
                }
                return _selectedPaths;
            }
        }

        [JsonIgnore]
        public IReadOnlyCollection<Point> Selection
        {
            get
            {
                return _selection;
            }
        }

        [JsonIgnore]
        public Rect SelectionBounds
        {
            get
            {
                if (_selectionBounds.IsEmpty)
                {
                    throw new NotImplementedException();
                }
                return _selectionBounds;
            }
        }

        [JsonConstructor]
        public Layer(List<Path> paths = null, string masterName = null, int width = 600)
        {
            Paths = paths != null ?
                new ObservableList<Path>(paths, copy: false) :
                new ObservableList<Path>();
            _watchPaths();
            MasterName = masterName ?? string.Empty;
            Width = width;

            _selection = new HashSet<Point>();

            foreach (Path path in Paths)
            {
                path.Parent = this;
            }
        }

        internal void ApplyChange(ChangeFlags flags, Point point = null)
        {
            if (flags.HasFlag(ChangeFlags.Outline))
            {
                if (point != null && point.Selected)
                {
                    _selectedPaths = null;
                    _selectionBounds = Rect.Empty;
                }
                _bounds = Rect.Empty;
                _closedCanvasPath = _openCanvasPath = null;
            }

            if (flags.HasFlag(ChangeFlags.Selection))
            {
                if (point.Selected != flags.HasFlag(ChangeFlags.SelectionRemove))
                {
                    _selection.Add(point);
                }
                else
                {
                    _selection.Remove(point);
                }
                _selectedPaths = null;
                _selectionBounds = Rect.Empty;
            }

            // XXX null-check is temporary
            Parent?.ApplyChange();
        }

        private CanvasGeometry _collectPaths(Func<Path, bool> predicate)
        {
            var device = CanvasDevice.GetSharedDevice();
            var builder = new CanvasPathBuilder(device);

            foreach (var path in Paths)
            {
                if (predicate.Invoke(path))
                {
                    builder.AddGeometry(path.CanvasPath);
                }
            }

            return CanvasGeometry.CreatePath(builder);
        }

        private void _watchPaths()
        {
            Paths.CollectionChanged += (sender, e) =>
            {
                if (e.OldItems != null)
                    foreach (Path path in e.OldItems)
                    {
                        path.Parent = null;

                        /*if (point.Selected)
                        {
                            Parent?.ApplyChange(ChangeFlags.SelectionRemove, point);
                        }*/
                    }

                if (e.NewItems != null)
                    foreach (Path path in e.NewItems)
                    {
                        Debug.Assert(path.Parent == null);

                        //path.Selected = false;
                        path.Parent = this;
                    }

                ApplyChange(ChangeFlags.Outline);
            };
        }
    }
}
