// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.Data
{
    using Fonte.Data.Changes;
    using Fonte.Data.Geometry;
    using Fonte.Data.Interfaces;
    using Microsoft.Graphics.Canvas;
    using Microsoft.Graphics.Canvas.Geometry;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using System;
    using System.Numerics;

    public partial class Component : ILayerElement
    {
        internal string _glyphName;
        internal Matrix3x2 _transformation;

        internal bool _isSelected;

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
        [JsonConverter(typeof(Matrix3x2Converter))]
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
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    new ComponentIsSelectedChange(this, value).Apply();
                }
            }
        }

        /**/

        [JsonIgnore]
        public Rect Bounds => Layer != null ? Rect.Transform(Layer.Bounds, Transformation) : Rect.Empty;

        [JsonIgnore]
        public CanvasGeometry ClosedCanvasPath => CollectPaths(layer => layer.ClosedCanvasPath);

        [JsonIgnore]
        public Layer Layer
        {
            get
            {
                var font = Parent?.Parent.Parent;

                if (font != null && font.TryGetGlyph(GlyphName, out Glyph glyph))
                {
                    return glyph.Layers[0];  // XXX
                }
                return null;
            }
        }

        [JsonIgnore]
        public CanvasGeometry OpenCanvasPath => CollectPaths(layer => layer.OpenCanvasPath);

        [JsonIgnore]
        public Vector2 Origin => Vector2.Transform(Vector2.Zero, Transformation);

        [JsonConstructor]
        public Component(string glyphName, Matrix3x2 transformation = default)
        {
            _glyphName = glyphName;
            _transformation = transformation != default ? transformation : Matrix3x2.Identity;
        }

        public void Decompose()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"{nameof(Component)}({GlyphName}, {Transformation})";
        }

        CanvasGeometry CollectPaths(Func<Layer, CanvasGeometry> predicate)
        {
            var device = CanvasDevice.GetSharedDevice();
            using var builder = new CanvasPathBuilder(device);
            builder.SetFilledRegionDetermination(CanvasFilledRegionDetermination.Winding);

            if (Layer is Layer layer)
            {
                if (layer == Parent)
                    throw new InvalidOperationException($"Component of glyph '{Parent.Name}' is recursive.");

                builder.AddGeometry(predicate.Invoke(layer));
            }

            return CanvasGeometry.CreatePath(builder)
                                 .Transform(Transformation);
        }
    }

    internal class Matrix3x2Converter : JsonConverter<Matrix3x2>
    {
        public override void WriteJson(JsonWriter writer, Matrix3x2 value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.M11);
            writer.WriteValue(value.M12);
            writer.WriteValue(value.M21);
            writer.WriteValue(value.M22);
            writer.WriteValue(value.M31);
            writer.WriteValue(value.M32);
            writer.WriteEndArray();
        }

        public override Matrix3x2 ReadJson(JsonReader reader, Type objectType, Matrix3x2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var array = JArray.Load(reader);

            return new Matrix3x2(
                array[0].ToObject<float>(),
                array[1].ToObject<float>(),
                array[2].ToObject<float>(),
                array[3].ToObject<float>(),
                array[4].ToObject<float>(),
                array[5].ToObject<float>());
        }
    }
}
