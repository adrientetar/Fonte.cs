/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Changes;
    using Fonte.Data.Interfaces;
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Geometry;
    using Newtonsoft.Json;

    using System;
    using System.Numerics;

    public partial class Component : ISelectable
    {
        internal string _glyphName;
        internal Matrix3x2 _transformation;

        internal bool _selected;

        [JsonProperty("glyphName")]
        public string GlyphName
        {
            get => _glyphName;
            set
            {
                if (value != _glyphName)
                {
                    new ComponentGlyphNameChange(this, value).Apply();
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
                    new ComponentTransformationChange(this, value).Apply();
                }
            }
        }

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
                    new ComponentSelectedChange(this, value).Apply();
                }
            }
        }

        /**/

        [JsonIgnore]
        public CanvasGeometry ClosedCanvasPath
        {
            get
            {
                var device = CanvasDevice.GetSharedDevice();
                var builder = new CanvasPathBuilder(device);
                builder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);

                var layer = Layer;
                if (layer != null)
                {
                    builder.AddGeometry(layer.ClosedCanvasPath);
                }

                return CanvasGeometry.CreatePath(builder).Transform(Transformation);
            }
        }

        [JsonIgnore]
        public Layer Layer
        {
            get
            {
                try
                {
                    var font = Parent?.Parent.Parent;
                    return font.GetGlyph(GlyphName).Layers[0];
                }
                catch (Exception) { }
                return null;
            }
        }

        [JsonIgnore]
        public CanvasGeometry OpenCanvasPath
        {
            get
            {
                var device = CanvasDevice.GetSharedDevice();
                var builder = new CanvasPathBuilder(device);
                builder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);

                var layer = Layer;
                if (layer != null)
                {
                    builder.AddGeometry(layer.OpenCanvasPath);
                }

                return CanvasGeometry.CreatePath(builder).Transform(Transformation);
            }
        }



        [JsonIgnore]
        public Vector2 Origin => Vector2.Transform(Vector2.Zero, Transformation);

        [JsonConstructor]
        public Component(string glyphName, Matrix3x2? transformation = null)
        {
            _glyphName = glyphName;
            _transformation = transformation ?? Matrix3x2.Identity;
        }

        public void Decompose()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"{nameof(Component)}({GlyphName}, {Transformation})";
        }
    }
}
