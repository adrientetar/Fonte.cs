﻿/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Interfaces;
    using Fonte.Data.Utilities;
    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    public partial class Glyph
    {
        private UndoStore _undoStore = new UndoStore();

        /* For kerning groups, make a struct kinda like a rect containing 4 strings? */

        [JsonProperty("layers")]
        public List<Layer> Layers { get; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("unicodes")]
        public List<string> Unicodes { get; set; }

        /**/

        [JsonIgnore]
        public bool IsModified
        {
            get => _undoStore.IsDirty;
            set
            {
                if (value)
                    throw new InvalidOperationException($"Cannot set {nameof(IsModified)} to true");

                _undoStore.Clear();
            }
        }

        [JsonIgnore]
        public bool IsSelected { get; set; }

        [JsonIgnore]
        public Font Parent
        { get; internal set; }

        [JsonIgnore]
        public IUndoProvider UndoStore => _undoStore;

        [JsonIgnore]
        public string Unicode => Unicodes.FirstOrDefault();

        [JsonConstructor]
        public Glyph(string name, List<string> unicodes = default, List<Layer> layers = default)
        {
            Layers = layers ?? new List<Layer>();
            Unicodes = unicodes ?? new List<string>();

            Name = name ?? string.Empty;

            foreach (var layer in Layers)
            {
                layer.Parent = this;
            }
        }

        public bool TryGetLayer(string masterName, out Layer layer)
        {
            foreach (var l in Layers)
            {
                if (l.IsMasterLayer && l.MasterName == masterName)
                {
                    layer = l;
                    return true;
                }
            }

            layer = new Layer(masterName);
            return false;
        }

        public override string ToString()
        {
            return $"{nameof(Glyph)}({Name}, {Layers.Count} layers)";
        }

        internal void OnChange(IChange change)
        {
            _undoStore.ProcessChange(change);
        }
    }
}
