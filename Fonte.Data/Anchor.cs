/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Changes;
    using Fonte.Data.Interfaces;
    using Newtonsoft.Json;

    using System.Numerics;

    public partial class Anchor : ISelectable
    {
        internal float _x;
        internal float _y;
        internal string _name;

        internal bool _selected;

        // XXX serialize to writesingle ; check that it's needed
        [JsonProperty("x")]
        public float X
        {
            get => _x;
            set
            {
                if (value != _x)
                {
                    new AnchorXChange(this, value).Apply();
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
                    new AnchorYChange(this, value).Apply();
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
                    new AnchorNameChange(this, value).Apply();
                }
            }
        }

        /**/

        [JsonIgnore]
        public Layer Parent
        { get; internal set; }

        [JsonIgnore]
        public bool Selected
        {
            get => _selected;
            set
            {
                if (value != _selected)
                {
                    new AnchorSelectedChange(this, value).Apply();
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
            return $"{nameof(Anchor)}({Name}, {X}, {Y})";
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }
    }
}
