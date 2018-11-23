/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Interfaces;
    using Newtonsoft.Json;

    using System.Numerics;

    public partial class Guideline : ILayerItem, ISelectable
    {
        private float _x;
        private float _y;
        private float _angle;
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

        [JsonProperty("angle")]
        public float Angle
        {
            get => _angle;
            set
            {
                if (value != _angle)
                {
                    _angle = value;
                    Parent?.ApplyChange(ChangeFlags.None, this);
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
                    _name = value;
                    //Parent?.ApplyChange(ChangeFlags.Key, this);
                }
            }
        }

        // Parent can be either Layer or Master
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
        public Guideline(float x, float y, float angle, string name = null)
        {
            _x = x;
            _y = y;
            _angle = angle;
            _name = name ?? string.Empty;
        }

        public override string ToString()
        {
            return $"{nameof(Guideline)}({_x}, {_y}, angle: {_angle})";
        }

        public Vector2 ToVector2()
        {
            return new Vector2(_x, _y);
        }
    }
}
