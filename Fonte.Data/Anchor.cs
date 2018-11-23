/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Interfaces;
    using Newtonsoft.Json;

    using System.Numerics;

    public partial class Anchor : ILayerItem, ISelectable
    {
        private float _x;
        private float _y;
        private string _name;

        private bool _selected;

        // XXX serialize to writesingle ; check that it's needed
        [JsonProperty("x")]
        public float X
        {
            get => _x;
            set
            {
                if (value != _x)
                {
                    _x = value;
                    Parent?.ApplyChange(ChangeFlags.Shape, this);
                }
            }
        }

        // XXX serialize to writesingle
        [JsonProperty("y")]
        public float Y
        {
            get => _y;
            set
            {
                if (value != _y)
                {
                    _y = value;
                    Parent?.ApplyChange(ChangeFlags.Shape, this);
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
                    var oldName = _name;
                    _name = value;
                    Parent?.ApplyChange(ChangeFlags.Name, oldName);
                }
            }
        }

        [JsonIgnore]
        internal string _Name
        {
            set
            {
                _name = value;
            }
        }

        [JsonIgnore]
        public Layer Parent { get; internal set; }

        [JsonIgnore]
        /* internal */ Layer ILayerItem.Parent { get => Parent; set { Parent = value; } }

        [JsonIgnore]
        public bool Selected
        {
            get => _selected;
            set
            {
                if (value != _selected)
                {
                    _selected = value;
                    Parent?.ApplyChange(ChangeFlags.Selection, this);
                }
            }
        }

        [JsonConstructor]
        public Anchor(float x, float y, string name = null)
        {
            _x = x;
            _y = y;
            _name = name ?? string.Empty;
        }

        public override string ToString()
        {
            return $"{nameof(Anchor)}({_name}, {_x}, {_y})";
        }

        public Vector2 ToVector2()
        {
            return new Vector2(_x, _y);
        }
    }
}
