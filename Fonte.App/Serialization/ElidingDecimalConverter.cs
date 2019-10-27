/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Serialization
{
    using Newtonsoft.Json;

    using System;

    public class ElidingDecimalConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException($"{typeof(ElidingDecimalConverter)} is write-only");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(float) || objectType == typeof(double);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var doubleValue = value as float? ?? (double)value;

            if (doubleValue == Math.Truncate(doubleValue))
            {
                writer.WriteRawValue(JsonConvert.ToString((int)doubleValue));
            }
            else
            {
                writer.WriteRawValue(JsonConvert.ToString(value));
            }
        }
    }
}
