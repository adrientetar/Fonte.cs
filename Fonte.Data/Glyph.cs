/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public partial class Glyph
    {
        [JsonProperty("layers")]
        public Layer[] Layers { get; set; }
    }
}
