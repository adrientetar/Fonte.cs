/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.App.Serialization
{
    using Newtonsoft.Json;

    using System.IO;

    public static class JsonBroker
    {
        public static string SerializeFont(Data.Font font)
        {
            var serializer = new JsonSerializer()
            {
                ContractResolver = ElidingContractResolver.Instance,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                Converters = { new ElidingDecimalConverter() }
            };
            using var sw = new StringWriter();
            using var writer = new JsonTextWriter(sw)
            {
                DateFormatString = "yyyy-MM-dd HH:mm:ss",
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Formatting = Formatting.Indented,
                Indentation = 0
            };

            serializer.Serialize(writer, font);
            
            return sw.ToString();
        }
    }
}
