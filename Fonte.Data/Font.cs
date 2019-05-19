/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Newtonsoft.Json;

    using System.Collections.Generic;

    public partial class Font
    {

        [JsonProperty("copyright")]
        public string Copyright { get; set; }

        [JsonProperty("designer")]
        public string Designer { get; set; }

        [JsonProperty("designerURL")]
        public string DesignerURL { get; set; }

        [JsonProperty("familyName")]
        public string FamilyName { get; set; }

        [JsonProperty("glyphs")]
        public List<Glyph> Glyphs { get; }

        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }

        [JsonProperty("manufacturerURL")]
        public string ManufacturerURL { get; set; }

        [JsonProperty("unitsPerEm")]
        public int UnitsPerEm { get; set; }

        [JsonProperty("versionMajor")]
        public int VersionMajor { get; set; }

        [JsonProperty("versionMinor")]
        public int VersionMinor { get; set; }

        public override string ToString()
        {
            return $"{nameof(Font)}({FamilyName}, v{VersionMajor}.{VersionMinor})";
        }
    }
}
