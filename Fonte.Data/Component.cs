/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Interfaces;
    using Newtonsoft.Json;

    using System.Numerics;

    public partial class Component : ILayerItem, ISelectable
    {
        private string _glyphName;
        private Matrix3x2 _transformation;

        private bool _selected;

        [JsonProperty("glyphName")]
        public string GlyphName
        {
            get => _glyphName;
            set
            {
                if (value != _glyphName)
                {
                    _glyphName = value;
                    Parent?.ApplyChange(ChangeFlags.Shape, this);
                }
            }
        }

        [JsonProperty("transformation")]
        public Matrix3x2 Transformation
        {
            get => _transformation;
            set
            {
                if (value != _transformation)
                {
                    _transformation = value;
                    Parent?.ApplyChange(ChangeFlags.Shape, this);
                }
            }
        }

        /**/

        [JsonIgnore]
        public Vector2 Origin => Vector2.Transform(Vector2.Zero, Transformation);

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
        public Component(string glyphName, Matrix3x2? transformation = null)
        {
            _glyphName = glyphName;
            _transformation = transformation ?? Matrix3x2.Identity;
        }

        public override string ToString()
        {
            return $"{nameof(Component)}({_glyphName}, {_transformation})";
        }
    }
}
