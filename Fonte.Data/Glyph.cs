/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    using System;
    using System.Collections.Generic;

    public partial class Glyph
    {
        [JsonProperty("layers")]
        public List<Layer> Layers { get; set; }

        public long? LastModified { get; internal set; }

        internal void ApplyChange()
        {
            LastModified = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}
