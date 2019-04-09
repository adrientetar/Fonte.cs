/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Changes;
    using Fonte.Data.Interfaces;
    using Fonte.Data.Utilities;
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Geometry;
    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Linq;
    using Windows.Foundation;

    public partial class Layer
    {
        internal List<Path> _paths;

        private Rect _bounds = Rect.Empty;
        private CanvasGeometry _closedCanvasPath;
        private CanvasGeometry _openCanvasPath;
        private List<Path> _selectedPaths;
        private HashSet<ISelectable> _selection;
        private Rect _selectionBounds = Rect.Empty;

        //[JsonIgnore]
        //public IReadOnlyDictionary<string, Anchor> Anchors { get; }

        [JsonProperty("anchors")]
        private IReadOnlyList<Anchor> Anchors { get; }
        //private List<Anchor> _anchorList
        //{
        //    get => new List<Anchor>(Anchors.Values);
        //}

        [JsonProperty("components")]
        public IReadOnlyList<Component> Components { get; }

        [JsonProperty("guidelines")]
        public IReadOnlyList<Guideline> Guidelines { get; }

        // could just store an actual reference to the master,
        // and serialize as masterName
        // -> but then hard to keep up to date as Parent and masters change
        [JsonProperty("masterName")]
        public string MasterName { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("paths")]
        public ObserverList<Path> Paths
        {
            get
            {
                var items = new ObserverList<Path>(_paths);
                items.CollectionChanged += (sender, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        if (e.NewItems.Count > 1)
                        {
                            new LayerPathsRangeChange(this, e.NewStartingIndex, e.NewItems.Cast<Path>().ToList(), true).Apply();
                        }
                        else
                        {
                            new LayerPathsChange(this, e.NewStartingIndex, (Path)e.NewItems[0]).Apply();
                        }
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Remove)
                    {
                        if (e.OldItems.Count > 1)
                        {
                            new LayerPathsRangeChange(this, e.OldStartingIndex, e.OldItems.Cast<Path>().ToList(), false).Apply();
                        }
                        else
                        {
                            new LayerPathsChange(this, e.OldStartingIndex, null).Apply();
                        }
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Replace)
                    {
                        Debug.Assert(e.NewItems.Count == 1);

                        new LayerPathsReplaceChange(this, e.NewStartingIndex, (Path)e.NewItems[0]).Apply();
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        new LayerPathsResetChange(this).Apply();
                    }
                };
                return items;
            }
        }

        /**/

        [JsonIgnore]
        public Rect Bounds
        {
            get
            {
                if (_bounds.IsEmpty)
                {
                    foreach (var path in Paths)
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
        public Glyph Parent
        { get; /*internal*/ set; }  // XXX

        [JsonIgnore]
        public IReadOnlyList<Path> SelectedPaths
        {
            get
            {
                if (_selectedPaths == null)
                {
                    _selectedPaths = Utilities.Selection.FilterSelection(_paths);
                }
                return _selectedPaths;
            }
        }

        [JsonIgnore]
        public IReadOnlyCollection<ISelectable> Selection
        {
            get
            {
                if (_selection == null)
                {
                    _selection = new HashSet<ISelectable>();

                    foreach (var anchor in Anchors)
                    {
                        if (anchor.Selected)
                        {
                            _selection.Add(anchor);
                        }
                    }
                    foreach (var component in Components)
                    {
                        if (component.Selected)
                        {
                            _selection.Add(component);
                        }
                    }
                    foreach (var guideline in Guidelines)
                    {
                        if (guideline.Selected)
                        {
                            _selection.Add(guideline);
                        }
                    }
                    foreach (var path in Paths)
                    {
                        foreach (var point in path.Points)
                        {
                            if (point.Selected)
                            {
                                _selection.Add(point);
                            }
                        }
                    }
                }

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
        public Layer(List<Anchor> anchors = null, List<Component> components = null, List<Guideline> guidelines = null, List<Path> paths = null, string masterName = null, int width = 600)
        {
            Anchors = anchors ?? new List<Anchor>();//anchors != null ? _makeAnchorDict(anchors) : new Dictionary<string, Anchor>();
            Components = components ?? new List<Component>();
            Guidelines = guidelines ?? new List<Guideline>();
            _paths = paths ?? new List<Path>();

            MasterName = masterName ?? string.Empty;
            Width = width;

            foreach (var path in _paths)
            {
                path.Parent = this;
            }
        }

        public void ClearSelection()
        {
            foreach (var item in Selection)
            {
                item.Selected = false;
            }
            //foreach (var guideline in Master.Guidelines)
            //{
            //    guideline.Selected = false;
            //}
        }

        public IChangeGroup CreateUndoGroup()
        {
            return Parent.UndoStore.CreateUndoGroup();
        }

        internal void OnChange(IChange change)
        {
            _selectionBounds = Rect.Empty;
            _selectedPaths = null;
            _bounds = Rect.Empty;
            _closedCanvasPath = _openCanvasPath = null;

            if (change.ClearSelection)  // .AffectsSelection ?
            {
                _selection = null;
            }

            Parent?.OnChange(change);
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

        private Dictionary<string, Anchor> _makeAnchorDict(List<Anchor> anchors)
        {
            var data = new Dictionary<string, Anchor>();

            foreach (Anchor anchor in anchors)
            {
                data.Add(anchor.Name, anchor);
            }

            return data;
        }
    }
}
