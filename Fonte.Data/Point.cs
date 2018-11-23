/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Interfaces;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Numerics;
    using System.Reflection;
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
    public partial class Point : ISelectable
    {
        private float _x;
        private float _y;
        private PointType _type;
        private bool _smooth;

        private ObservableDictionary<string, object> _extraData;

        private bool _selected;

        public float X
        {
            get => _x;
            set
            {
                if (value != _x)
                {
                    _x = value;
                    Parent?.ApplyChange(ChangeFlags.ShapeOutline, this);
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
                    _y = value;
                    Parent?.ApplyChange(ChangeFlags.ShapeOutline, this);
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
                    _type = value;
                    Parent?.ApplyChange(ChangeFlags.None);
                }
            }
        }

        public bool Smooth
        {
            get => _smooth;
            set
            {
                if (value != _smooth)
                {
                    _smooth = value;
                    Parent?.ApplyChange(ChangeFlags.None);
                }
            }
        }

        public IDictionary<string, object> ExtraData {
            get
            {
                if (_extraData == null)
                {
                    _extraData = new ObservableDictionary<string, object>();
                    _watchExtraData();
                }
                return _extraData;
            }
        }

        public Path Parent { get; internal set; }

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

        /**/

        public string UniqueId
        {
            get
            {
                if (ExtraData.TryGetValue("id", out object value) && (value as string != null))
                {
                    return (string)value;
                }
                ExtraData["id"] = Guid.NewGuid().ToString();
                return (string)ExtraData["id"];
            }
        }

        public Point(float x, float y, PointType type = PointType.None, bool smooth = false, IDictionary<string, object> extraData = null)
        {
            _x = x;
            _y = y;
            _type = type;
            _smooth = smooth;

            if (extraData != null)
            {
                _extraData = new ObservableDictionary<string, object>(extraData, copy: false);
                _watchExtraData();
            }
        }

        public override string ToString()
        {
            string more;
            if (_type != PointType.None)
            {
                more = $", {_type}";
                if (_smooth)
                {
                    more += $", smooth: {_smooth}";
                }
            }
            else
            {
                more = string.Empty;
            }
            return $"{nameof(Point)}({_x}, {_y}{more})";
        }

        public Vector2 ToVector2()
        {
            return new Vector2(_x, _y);
        }

        private void _watchExtraData()
        {
            _extraData.CollectionChanged += (sender, e) =>
            {
                Parent?.ApplyChange(ChangeFlags.None);
            };
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
            var point = (Point)value;

            var formatting = writer.Formatting;
            writer.WriteStartArray();
            try
            {
                writer.Formatting = Formatting.None;

                WriteSingle(writer, point.X);
                WriteSingle(writer, point.Y);
                if (point.Type != PointType.None) {
                    serializer.Serialize(writer, point.Type);
                    if (point.Smooth) {
                        writer.WriteValue(point.Smooth);
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
