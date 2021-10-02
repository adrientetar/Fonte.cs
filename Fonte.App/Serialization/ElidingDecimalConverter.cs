// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

using Newtonsoft.Json;

using System;


namespace Fonte.App.Serialization
{
    public class ElidingDecimalConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException($"{typeof(ElidingDecimalConverter)} is write-only.");
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
