/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;

    using System;
    using System.Numerics;
    using System.Reflection;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum PointType
    {
        None = 0,
        [JsonProperty("move")]
        Move,
        [JsonProperty("line")]
        Line,
        [JsonProperty("curve")]
        Curve,
    };

    [JsonConverter(typeof(PointConverter))]
    public partial class Point
    {
        public Vector2 Position;

        public PointType Type;

        public bool Smooth;

        public bool Selected;

        public float X => Position.X;

        public float Y => Position.Y;

        public Point(Vector2 position, PointType type = PointType.None, bool smooth = false)
        {
            Position = position;
            Type = type;
            Smooth = smooth;
        }

        public Point(float x, float y, PointType type = PointType.None, bool smooth = false) :
            this(new Vector2(x, y), type, smooth)
        {
        }
    }

    internal class PointConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Point).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var array = JArray.Load(reader);

            return new Point(
                    new Vector2(array[0].ToObject<float>(),
                                array[1].ToObject<float>()),
                    array.Count > 2 ? array[2].ToObject<PointType>() : PointType.None,
                    array.Count > 3 ? array[3].ToObject<bool>() : false
                );
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var point = value as Point;

            writer.WriteStartArray();
            WriteSingle(writer, point.Position.X);
            WriteSingle(writer, point.Position.Y);
            if (point.Type != PointType.None) {
                writer.WriteValue(point.Type);
                if (point.Smooth) {
                    writer.WriteValue(point.Smooth);
                }
            }
            writer.WriteEndArray();
        }

        private void WriteSingle(JsonWriter writer, float number)
        {
            if (number == Math.Truncate(number))
            {
                writer.WriteValue((int)number);
            }
            else
            {
                writer.WriteValue(number);
            }
        }
    }
}
