/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Changes;
    using Fonte.Data.Collections;
    using Fonte.Data.Geometry;
    using Fonte.Data.Interfaces;
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Geometry;
    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
    using System.Numerics;

    public partial class Layer : IComparable<Layer>
    {
        internal List<Anchor> _anchors;
        internal List<Component> _components;
        internal List<Guideline> _guidelines;
        internal List<Path> _paths;
        internal string _masterName;
        internal string _name;
        internal float _width;
        internal float _height;
        internal float? _yOrigin;

        private Rect _bounds = Rect.Empty;
        private CanvasGeometry _closedCanvasPath;
        private CanvasGeometry _openCanvasPath;
        private List<Path> _selectedPaths;
        private HashSet<ILayerElement> _selection;
        private Rect _selectionBounds = Rect.Empty;

        [JsonProperty("anchors")]
        public ObserverList<Anchor> Anchors
        {
            get
            {
                var items = new ObserverList<Anchor>(_anchors);
                items.ChangeRequested += (sender, args) =>
                {
                    if (args.Action == NotifyChangeRequestedAction.Add)
                    {
                        new LayerAnchorsChange(this, args.NewStartingIndex, args.NewItems, true).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Remove)
                    {
                        new LayerAnchorsChange(this, args.OldStartingIndex, args.OldItems, false).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Replace)
                    {
                        new LayerAnchorsReplaceChange(this, args.NewStartingIndex, args.NewItems).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Reset)
                    {
                        new LayerAnchorsResetChange(this).Apply();
                    }
                };
                return items;
            }
        }

        [JsonProperty("components")]
        public ObserverList<Component> Components
        {
            get
            {
                var items = new ObserverList<Component>(_components);
                items.ChangeRequested += (sender, args) =>
                {
                    if (args.Action == NotifyChangeRequestedAction.Add)
                    {
                        new LayerComponentsChange(this, args.NewStartingIndex, args.NewItems, true).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Remove)
                    {
                        new LayerComponentsChange(this, args.OldStartingIndex, args.OldItems, false).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Replace)
                    {
                        new LayerComponentsReplaceChange(this, args.NewStartingIndex, args.NewItems).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Reset)
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
                items.ChangeRequested += (sender, args) =>
                {
                    if (args.Action == NotifyChangeRequestedAction.Add)
                    {
                        new LayerGuidelinesChange(this, args.NewStartingIndex, args.NewItems, true).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Remove)
                    {
                        new LayerGuidelinesChange(this, args.OldStartingIndex, args.OldItems, false).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Replace)
                    {
                        new LayerGuidelinesReplaceChange(this, args.NewStartingIndex, args.NewItems).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Reset)
                    {
                        new LayerGuidelinesResetChange(this).Apply();
                    }
                };
                return items;
            }
        }

        [JsonProperty("height")]
        public float Height
        {
            get => _height;
            set
            {
                if (value != _height)
                {
                    new LayerHeightChange(this, value).Apply();
                }
            }
        }

        [JsonProperty("masterName")]
        public string MasterName
        {
            get => _masterName;
            set
            {
                if (value != _masterName)
                {
                    new LayerMasterNameChange(this, value).Apply();
                }
            }
        }

        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    new LayerNameChange(this, value).Apply();
                }
            }
        }

        [JsonProperty("paths")]
        public ObserverList<Path> Paths
        {
            get
            {
                var items = new ObserverList<Path>(_paths);
                items.ChangeRequested += (sender, args) =>
                {
                    if (args.Action == NotifyChangeRequestedAction.Add)
                    {
                        new LayerPathsChange(this, args.NewStartingIndex, args.NewItems, true).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Remove)
                    {
                        new LayerPathsChange(this, args.OldStartingIndex, args.OldItems, false).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Replace)
                    {
                        new LayerPathsReplaceChange(this, args.NewStartingIndex, args.NewItems).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Reset)
                    {
                        new LayerPathsResetChange(this).Apply();
                    }
                };
                return items;
            }
        }

        [JsonProperty("width")]
        public float Width
        {
            get => _width;
            set
            {
                if (value != _width)
                {
                    new LayerWidthChange(this, value).Apply();
                }
            }
        }

        [JsonProperty("yOrigin")]
        public float? YOrigin
        {
            get => _yOrigin;
            set
            {
                if (value != _yOrigin)
                {
                    new LayerYOriginChange(this, value).Apply();
                }
            }
        }

        /**/

        [JsonIgnore]
        public string ActualName
        {
            get
            {
                if (IsMasterLayer)
                {
                    return Master.Name;
                }
                return Name;
            }
        }

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
                // We can't cache component bounds, we aren't notified when it changes
                var bounds = _bounds;
                foreach (var component in Components)
                {
                    bounds.Union(component.Bounds);
                }
                return bounds;
            }
        }

        [JsonIgnore]
        public CanvasGeometry ClosedCanvasPath
        {
            get
            {
                if (_closedCanvasPath == null)
                {
                    _closedCanvasPath = CollectPaths(path => !path.IsOpen);
                }
                return _closedCanvasPath;
            }
        }

        [JsonIgnore]
        public bool IsEditing => Parent?.UndoStore.HasOpenGroup ?? false;

        [JsonIgnore]
        public bool IsMasterLayer
        {
            get
            {
                return !string.IsNullOrEmpty(MasterName) && string.IsNullOrEmpty(Name);
            }
        }

        [JsonIgnore]
        public bool IsVisible
        { get; set; }

        [JsonIgnore]
        public Margins Margins
        {
            get
            {
                var bounds = Bounds;
                if (!bounds.IsEmpty)
                {
                    var bottom = bounds.Bottom;
                    var top = bounds.Top;
                    if (YOrigin is float yOrigin)
                    {
                        bottom -= yOrigin - Height;
                        top += yOrigin;
                    }
                    else
                    {
                        top += Height;
                    }
                    return new Margins(
                            bounds.Left,
                            top,
                            Width - bounds.Right,
                            bottom
                        );
                }
                return Margins.Empty;
            }
        }

        [JsonIgnore]
        public Master Master
        {
            get
            {
                var font = Parent?.Parent;

                if (font != null && font.TryGetMaster(MasterName, out Master master))
                {
                    return master;
                }
                return null;
            }
        }

        [JsonIgnore]
        public CanvasGeometry OpenCanvasPath
        {
            get
            {
                if (_openCanvasPath == null)
                {
                    _openCanvasPath = CollectPaths(path => path.IsOpen);
                }
                return _openCanvasPath;
            }
        }

        [JsonIgnore]
        public Glyph Parent
        { get; internal set; }

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
                    _selection = new HashSet<ILayerElement>();

                    foreach (var anchor in _anchors)
                    {
                        if (anchor.IsSelected)
                        {
                            _selection.Add(anchor);
                        }
                    }
                    foreach (var component in _components)
                    {
                        if (component.IsSelected)
                        {
                            _selection.Add(component);
                        }
                    }
                    foreach (var guideline in _guidelines)
                    {
                        if (guideline.IsSelected)
                        {
                            _selection.Add(guideline);
                        }
                    }
                    foreach (var path in _paths)
                    {
                        foreach (var point in path.Points)
                        {
                            if (point.IsSelected)
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
                        if (item is Component component)
                        {
                            _selectionBounds.Union(component.Bounds);
                        }
                        else if (item is ILocatable iloc)
                        {
                            _selectionBounds.Union(iloc.ToVector2());
                        }
                    }
                }
                return _selectionBounds;
            }
        }

        [JsonConstructor]
        public Layer(string masterName = default, string name = default, float width = 600, float height = default, float? yOrigin = default,
                     List<Anchor> anchors = default, List<Component> components = default, List<Guideline> guidelines = default, List<Path> paths = default)
        {
            _anchors = anchors ?? new List<Anchor>();
            _components = components ?? new List<Component>();
            _guidelines = guidelines ?? new List<Guideline>();
            _paths = paths ?? new List<Path>();

            _masterName = masterName ?? string.Empty;
            _name = name ?? string.Empty;
            _width = width;
            _height = height;
            _yOrigin = yOrigin;

            foreach (var anchor in _anchors)
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
                item.IsSelected = false;
            }
            if (Master is Master master)
            {
                foreach (var guideline in master.Guidelines)
                {
                    guideline.IsSelected = false;
                }
            }
        }

        public Layer Clone()
        {
            var json = JsonConvert.SerializeObject(this);

            var layer = JsonConvert.DeserializeObject<Layer>(json);
            // TODO: consider doing Culture-specific formatting, e.g. taking IFormatProvider as optional arg
            layer.Name = DateTime.UtcNow.ToString("MMM dd yyyy @ HH:mm");
            layer.Parent = Parent;

            return layer;
        }

        public IChangeGroup CreateUndoGroup()
        {
            return Parent.UndoStore.CreateUndoGroup();
        }

        public bool TryGetAnchor(string name, out Anchor anchor)
        {
            foreach (var a in _anchors)
            {
                if (a.Name == name)
                {
                    anchor = a;
                    return true;
                }
            }

            anchor = new Anchor(0, 0, name);
            return false;
        }

        public override string ToString()
        {
            var more = Parent != null ? $"{Parent.Name}:" : string.Empty;
            var name = IsMasterLayer ? $"*{MasterName}" : Name;
            return $"{nameof(Layer)}({more}{name}, {_paths.Count} paths)";
        }

        public void Transform(Matrix3x2 matrix, bool selectionOnly = false)
        {
            using (var group = CreateUndoGroup())
            {
                foreach (var anchor in Anchors)
                {
                    if (!selectionOnly || anchor.IsSelected)
                    {
                        var pos = Vector2.Transform(anchor.ToVector2(), matrix);

                        anchor.X = pos.X;
                        anchor.Y = pos.Y;
                    }
                }
                foreach (var component in Components)
                {
                    if (!selectionOnly || component.IsSelected)
                    {
                        component.Transformation *= matrix;
                    }
                }
                foreach (var guideline in Guidelines)
                {
                    if (!selectionOnly || guideline.IsSelected)
                    {
                        // XXX: also transform the angle vector
                        //guideline.Direction = Vector2.Transform(guideline.Direction, matrix);
                        var pos = Vector2.Transform(guideline.ToVector2(), matrix);

                        guideline.X = pos.X;
                        guideline.Y = pos.Y;
                    }
                }
                foreach (var path in Paths)
                {
                    path.Transform(matrix, selectionOnly);
                }
            }
        }

        internal void OnChange(IChange change)
        {
            _bounds = _selectionBounds = Rect.Empty;
            _closedCanvasPath = _openCanvasPath = null;
            _selectedPaths = null;

            if (change.AffectsSelection)
            {
                _selection = null;
            }

            Parent?.OnChange(change);
        }

        CanvasGeometry CollectPaths(Func<Path, bool> predicate)
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

            builder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);
            return CanvasGeometry.CreatePath(builder);
        }

        int IComparable<Layer>.CompareTo(Layer other)
        {
            var masterName = MasterName;
            var otherMasterName = other.MasterName;

            if (string.IsNullOrEmpty(otherMasterName)) return -1;
            if (string.IsNullOrEmpty(masterName)) return 1;
            if (masterName == otherMasterName)
            {
                var name = Name;
                var otherName = other.Name;

                if (string.IsNullOrEmpty(name)) return -1;
                if (string.IsNullOrEmpty(otherName)) return 1;

                return name.CompareTo(otherName);
            }

            return masterName.CompareTo(otherMasterName);
        }
    }
}
