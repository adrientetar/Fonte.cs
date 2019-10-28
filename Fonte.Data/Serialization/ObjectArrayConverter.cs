// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.Data.Converters
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class ObjectArrayConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(T).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var contract = serializer.ContractResolver.ResolveContract(objectType) as JsonObjectContract;
            if (contract == null)
                throw new JsonSerializationException(string.Format("Invalid type {0}.", objectType.FullName));

            if (reader.MoveToContentAndAssert().TokenType == JsonToken.Null)
                return null;
            if (reader.TokenType != JsonToken.StartArray)
                throw new JsonSerializationException(string.Format("Token {0} was not {1}.", reader.TokenType, nameof(JsonToken.StartArray)));

            existingValue = existingValue ?? contract.DefaultCreator();

            using (var enumerator = SerializableProperties(contract).GetEnumerator())
            {
                while (true)
                {
                    switch (reader.ReadToContentAndAssert().TokenType)
                    {
                        case JsonToken.EndArray:
                            return existingValue;

                        default:
                            if (!enumerator.MoveNext())
                            {
                                reader.Skip();
                                break;
                            }
                            var property = enumerator.Current;

                            object propertyValue;
                            if (property.Converter != null && property.Converter.CanRead)
                                propertyValue = property.Converter.ReadJson(reader, property.PropertyType, property.ValueProvider.GetValue(existingValue), serializer);
                            else
                                propertyValue = serializer.Deserialize(reader, property.PropertyType);
                            property.ValueProvider.SetValue(existingValue, propertyValue);
                            break;
                    }
                }
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var objectType = value.GetType();
            var contract = serializer.ContractResolver.ResolveContract(objectType) as JsonObjectContract;
            if (contract == null)
                throw new JsonSerializationException(string.Format("Invalid type {0}.", objectType.FullName));

            writer.WriteStartArray();
            foreach (var property in SerializableProperties(contract))
            {
                var propertyValue = property.ValueProvider.GetValue(value);
                if (property.Converter != null && property.Converter.CanWrite)
                    property.Converter.WriteJson(writer, propertyValue, serializer);
                else
                    serializer.Serialize(writer, propertyValue);
            }
            writer.WriteEndArray();
        }

        static IEnumerable<JsonProperty> SerializableProperties(JsonObjectContract contract)
        {
            return contract.Properties.Where(p => !p.Ignored && p.Readable && p.Writable);
        }
    }

    internal static partial class JsonExtensions
    {
        public static JsonReader ReadToContentAndAssert(this JsonReader reader)
        {
            return reader.ReadAndAssert().MoveToContentAndAssert();
        }

        public static JsonReader MoveToContentAndAssert(this JsonReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (reader.TokenType == JsonToken.None)       // Skip past beginning of stream.
                reader.ReadAndAssert();
            while (reader.TokenType == JsonToken.Comment) // Skip past comments.
                reader.ReadAndAssert();
            return reader;
        }

        public static JsonReader ReadAndAssert(this JsonReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            if (!reader.Read())
                throw new JsonReaderException("Unexpected end of JSON stream.");
            return reader;
        }
    }
}
