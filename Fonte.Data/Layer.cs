/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Interfaces;
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Geometry;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using Windows.Foundation;

    public partial class Layer
    {
        private Rect _bounds = Rect.Empty;
        private CanvasGeometry _closedCanvasPath;
        private CanvasGeometry _openCanvasPath;
        private List<Path> _selectedPaths;
        private HashSet<ISelectable> _selection;
        private Rect _selectionBounds = Rect.Empty;

        [JsonIgnore]
        public ObservableDictionary<string, Anchor> Anchors { get; }

        [JsonProperty("anchors")]
        private List<Anchor> _anchorList
        {
            get => new List<Anchor>(Anchors.Values);
        }

        [JsonProperty("components")]
        public ObservableList<Component> Components { get; }

        [JsonProperty("guidelines")]
        public ObservableList<Guideline> Guidelines { get; }

        // could just store an actual reference to the master,
        // and serialize as masterName
        // -> but then hard to keep up to date as Parent and masters change
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
                    _openCanvasPath = _collectPaths(path => path.IsOpen);
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
        public IReadOnlyCollection<ISelectable> Selection => _selection;

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
        public Layer(List<Anchor> anchors = null, List<Component> components = null, List<Guideline> guidelines = null, List<Path> paths = null, string masterName = null, int width = 600)
        {
            // we could do all this work + set parent in getter
            Anchors = _watchAnchors(anchors);
            Components = _watchItems(components); // ChangeFlags.ShapeOutline ?
            Guidelines = _watchItems(guidelines);
            Paths = _watchItems(paths, flags: ChangeFlags.ShapeOutline);

            MasterName = masterName ?? string.Empty;
            Width = width;

            _selection = new HashSet<ISelectable>();
        }

        internal void ApplyChange(ChangeFlags flags, object obj = null)
        {
            if (flags.HasFlag(ChangeFlags.Selection))
            {
                var item = (ISelectable)obj;
                if (item.Selected)
                {
                    _selection.Add(item);
                }
                else
                {
                    _selection.Remove(item);
                }
                _selectionBounds = Rect.Empty;
                if (item is Point) _selectedPaths = null;
            }
            else
            {
                if (flags.HasFlag(ChangeFlags.Shape))
                {
                    var item = (ISelectable)obj;
                    var outline = flags.HasFlag(ChangeFlags.ShapeOutline);

                    if (item != null && item.Selected)
                    {
                        _selectionBounds = Rect.Empty;
                        if (outline) _selectedPaths = null;
                    }
                    _bounds = Rect.Empty;
                    if (outline) _closedCanvasPath = _openCanvasPath = null;
                }
                else if (flags.HasFlag(ChangeFlags.Name))
                {
                    var oldName = (string)obj;

                    var dict = Anchors.Dictionary;
                    var value = dict[oldName];
                    dict.Remove(oldName);
                    dict.Add(value.Name, value);
                }

                // XXX null-check is temporary
                Parent?.ApplyChange();
            }
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

        private ObservableDictionary<string, Anchor> _watchAnchors(List<Anchor> items)
        {
            var data = new Dictionary<string, Anchor>();
            if (items != null)
            {
                foreach (Anchor item in items)
                {
                    data.Add(item.Name, item);
                }
            }
            var observable = new ObservableDictionary<string, Anchor>(data, copy: false);

            observable.CollectionChanged += (sender, e) =>
            {
                if (e.OldItems != null)
                    foreach (KeyValuePair<string, Anchor> kv in e.OldItems)
                    {
                        var item = kv.Value;

                        item.Selected = false;
                        item.Parent = null;
                    }

                if (e.NewItems != null)
                    foreach (KeyValuePair<string, Anchor> kv in e.NewItems)
                    {
                        var item = kv.Value;

                        Debug.Assert(item.Parent == null);

                        item._Name = kv.Key;
                        item.Selected = false;
                        item.Parent = this;
                    }

                ApplyChange(ChangeFlags.None);
            };

            return observable;
        }

        private ObservableList<T> _watchItems<T>(List<T> items, ChangeFlags flags = ChangeFlags.Shape)
            where T: ILayerItem, ISelectable
        {
            var observable = items != null ?
                new ObservableList<T>(items, copy: false) :
                new ObservableList<T>();

            if (items != null)
                foreach (T item in items)
                {
                    //item.Selected = false;
                    item.Parent = this;
                }

            observable.CollectionChanged += (sender, e) =>
            {
                if (e.OldItems != null)
                    foreach (T item in e.OldItems)
                    {
                        item.Selected = false;
                        item.Parent = null;
                    }

                if (e.NewItems != null)
                    foreach (T item in e.NewItems)
                    {
                        Debug.Assert(item.Parent == null);

                        item.Selected = false;
                        item.Parent = this;
                    }

                ApplyChange(flags);
            };

            return observable;
        }
    }
}
