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
    using System.Numerics;
    using Windows.Foundation;

    public partial class Layer
    {
        internal Dictionary<string, Anchor> _anchors;
        internal List<Component> _components;
        internal List<Guideline> _guidelines;
        internal List<Path> _paths;

        private Rect _bounds = Rect.Empty;
        private CanvasGeometry _closedCanvasPath;
        private CanvasGeometry _openCanvasPath;
        private List<Path> _selectedPaths;
        private HashSet<ISelectable> _selection;
        private Rect _selectionBounds = Rect.Empty;

        [JsonIgnore]
        public IReadOnlyDictionary<string, Anchor> Anchors { get; }

        [JsonProperty("anchors")]
        private List<Anchor> _anchorList
        {
            get => new List<Anchor>(Anchors.Values);
        }

        [JsonProperty("components")]
        public ObserverList<Component> Components
        {
            get
            {
                var items = new ObserverList<Component>(_components);
                items.CollectionChanged += (sender, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        if (e.NewItems.Count > 1)
                        {
                            new LayerComponentsRangeChange(this, e.NewStartingIndex, e.NewItems.Cast<Component>().ToList(), true).Apply();
                        }
                        else
                        {
                            new LayerComponentsChange(this, e.NewStartingIndex, (Component)e.NewItems[0]).Apply();
                        }
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Remove)
                    {
                        if (e.OldItems.Count > 1)
                        {
                            new LayerComponentsRangeChange(this, e.OldStartingIndex, e.OldItems.Cast<Component>().ToList(), false).Apply();
                        }
                        else
                        {
                            new LayerComponentsChange(this, e.OldStartingIndex, null).Apply();
                        }
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Replace)
                    {
                        Debug.Assert(e.NewItems.Count == 1);

                        new LayerComponentsReplaceChange(this, e.NewStartingIndex, (Component)e.NewItems[0]).Apply();
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        new LayerComponentsResetChange(this).Apply();
                    }
                };
                return items;
            }
        }

        [JsonProperty("guidelines")]
        public ObserverList<Guideline> Guidelines
        {
            get
            {
                var items = new ObserverList<Guideline>(_guidelines);
                items.CollectionChanged += (sender, e) =>
                {
                    if (e.Action == NotifyCollectionChangedAction.Add)
                    {
                        if (e.NewItems.Count > 1)
                        {
                            new LayerGuidelinesRangeChange(this, e.NewStartingIndex, e.NewItems.Cast<Guideline>().ToList(), true).Apply();
                        }
                        else
                        {
                            new LayerGuidelinesChange(this, e.NewStartingIndex, (Guideline)e.NewItems[0]).Apply();
                        }
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Remove)
                    {
                        if (e.OldItems.Count > 1)
                        {
                            new LayerGuidelinesRangeChange(this, e.OldStartingIndex, e.OldItems.Cast<Guideline>().ToList(), false).Apply();
                        }
                        else
                        {
                            new LayerGuidelinesChange(this, e.OldStartingIndex, null).Apply();
                        }
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Replace)
                    {
                        Debug.Assert(e.NewItems.Count == 1);

                        new LayerGuidelinesReplaceChange(this, e.NewStartingIndex, (Guideline)e.NewItems[0]).Apply();
                    }
                    else if (e.Action == NotifyCollectionChangedAction.Reset)
                    {
                        new LayerGuidelinesResetChange(this).Apply();
                    }
                };
                return items;
            }
        }

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

                    foreach (var anchor in _anchors.Values)
                    {
                        if (anchor.Selected)
                        {
                            _selection.Add(anchor);
                        }
                    }
                    foreach (var component in _components)
                    {
                        if (component.Selected)
                        {
                            _selection.Add(component);
                        }
                    }
                    foreach (var guideline in _guidelines)
                    {
                        if (guideline.Selected)
                        {
                            _selection.Add(guideline);
                        }
                    }
                    foreach (var path in _paths)
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
                    foreach (var item in Selection)
                    {
                        if (item is Point point)
                        {
                            _selectionBounds.Union(point.ToVector2().ToPoint());
                        }
                    }
                }
                return _selectionBounds;
            }
        }

        [JsonConstructor]
        public Layer(List<Anchor> anchors = null, List<Component> components = null, List<Guideline> guidelines = null, List<Path> paths = null, string masterName = null, int width = 600)
        {
            _anchors = anchors != null ? _makeAnchorDict(anchors) : new Dictionary<string, Anchor>();
            _components = components ?? new List<Component>();
            _guidelines = guidelines ?? new List<Guideline>();
            _paths = paths ?? new List<Path>();

            MasterName = masterName ?? string.Empty;
            Width = width;

            foreach (var anchor in _anchors.Values)
            {
                anchor.Parent = this;
            }
            foreach (var component in _components)
            {
                component.Parent = this;
            }
            foreach (var guideline in _guidelines)
            {
                guideline.Parent = this;
            }
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

        public void Clone()
        {
            throw new NotImplementedException();
        }

        public IChangeGroup CreateUndoGroup()
        {
            return Parent.UndoStore.CreateUndoGroup();
        }

        public override string ToString()
        {
            var more = "";  // XXX
            var name = "default";  // XXX
            return $"{nameof(Layer)}({name}, {_paths.Count} paths{more})";
        }

        public void Transform(Matrix3x2 matrix, bool selected = false)
        {
            throw new NotImplementedException();
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
