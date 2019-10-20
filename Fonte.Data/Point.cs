/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Changes;
    using Fonte.Data.Interfaces;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;

    using System;
    using System.Collections.Generic;
    using System.Numerics;
    using System.Runtime.Serialization;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum PointType
    {
        None = 0,
        [EnumMember(Value = "move")]
        Move,
        [EnumMember(Value = "line")]
        Line,
        [EnumMember(Value = "curve")]
        Curve,
    };

    [JsonConverter(typeof(PointConverter))]
    public partial class Point : ILayerElement, ILocatable
    {
        internal float _x;
        internal float _y;
        internal PointType _type;
        internal bool _isSmooth;

        internal Dictionary<string, object> _extraData;

        internal bool _isSelected;

        public float X
        {
            get => _x;
            set
            {
                if (value != _x)
                {
                    new PointXChange(this, value).Apply();
                }
            }
        }

        public float Y
        {
            get => _y;
            set
            {
                if (value != _y)
                {
                    new PointYChange(this, value).Apply();
                }
            }
        }

        public PointType Type
        {
            get => _type;
            set
            {
                if (value != _type)
                {
                    new PointTypeChange(this, value).Apply();
                }
            }
        }

        public bool IsSmooth
        {
            get => _isSmooth;
            set
            {
                if (value != _isSmooth)
                {
                    new PointIsSmoothChange(this, value).Apply();
                }
            }
        }

        public Dictionary<string, object> ExtraData
        {
            get
            {
                if (_extraData == null)
                {
                    _extraData = new Dictionary<string, object>();
                }
                return _extraData;
            }
        }

        /**/

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value != _isSelected)
                {
                    new PointIsSelectedChange(this, value).Apply();
                }
            }
        }

        public Path Parent
        { get; internal set; }

        public Point(float x, float y, PointType type = default, bool isSmooth = default,
                     Dictionary<string, object> extraData = default)
        {
            _x = x;
            _y = y;
            _type = type;
            _isSmooth = isSmooth;
            _extraData = extraData;
        }

        public Point Clone()
        {
            return new Point(X, Y, Type, IsSmooth);
        }

        public override string ToString()
        {
            string more;
            if (Type != PointType.None)
            {
                more = $", {Type}";
                if (IsSmooth)
                {
                    more += $", smooth: {IsSmooth}";
                }
            }
            else
            {
                more = string.Empty;
            }
            return $"{nameof(Point)}({X}, {Y}{more})";
        }

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
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

            var size = array.Count;
            var last = array[size - 1];
            Dictionary<string, object> extraData = null;
            if (last.Type == JTokenType.Object)
            {
                extraData = last.ToObject<Dictionary<string, object>>();
                --size;
            }

            return new Point(
                    array[0].ToObject<float>(),
                    array[1].ToObject<float>(),
                    size > 2 ? array[2].ToObject<PointType>() : PointType.None,
                    size > 3 ? array[3].ToObject<bool>() : false,
                    extraData
                );
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // https://www.newtonsoft.com/json/help/html/T_Newtonsoft_Json_DefaultValueHandling.htm
            var point = (Point)value;

            var formatting = writer.Formatting;
            writer.WriteStartArray();
            try
            {
                writer.Formatting = Formatting.None;

                writer.WriteValue(point.X);
                writer.WriteValue(point.Y);
                if (point.Type != PointType.None) {
                    serializer.Serialize(writer, point.Type);
                    if (point.IsSmooth) {
                        writer.WriteValue(point.IsSmooth);
                    }
                }
                if (point.ExtraData != null && point.ExtraData.Count > 0)
                {
                    serializer.Serialize(writer, point.ExtraData);
                }
            }
            finally
            {
                writer.WriteEndArray();
                writer.Formatting = formatting;
            }
        }
    }
}
