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
    using Newtonsoft.Json.Linq;

    using System;
    using System.Numerics;

    public partial class Component : ISelectable
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
