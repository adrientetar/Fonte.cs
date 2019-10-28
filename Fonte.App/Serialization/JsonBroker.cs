// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

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
