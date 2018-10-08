/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using System;
    using System.Collections.Generic;
    using System.Reflection;

    [JsonConverter(typeof(PathConverter))]
    public partial class Path
    {
        public bool Open
        {
            get
            {
                return Points.Count == 0 || Points[0].Type == PointType.Move;
            }
        }

        [JsonProperty("points")]
        public List<Point> Points { get; set; }

        public Path(List<Point> points = null)
        {
            Points = points ?? new List<Point>();
        }
    }

    internal class PathConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Path).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var array = JArray.Load(reader);

            return new Path(
                    array.ToObject<List<Point>>()
                );
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var path = value as Path;

            writer.WriteValue(path.Points);
        }
    }
}
