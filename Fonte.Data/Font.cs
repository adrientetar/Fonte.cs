/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Newtonsoft.Json;

    using System.Collections.Generic;

    public partial class Font
    {
        [JsonProperty("glyphs")]
        public List<Glyph> Glyphs { get; }

        [JsonProperty("copyright")]
        public string Copyright { get; set; }

        [JsonProperty("designer")]
        public string Designer { get; set; }

        [JsonProperty("designerURL")]
        public string DesignerURL { get; set; }

        [JsonProperty("familyName")]
        public string FamilyName { get; set; }

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

        [JsonConstructor]
        public Font(List<Glyph> glyphs = null, string copyright = null, string designer = null, string designerURL = null,
                    string familyName = null, string manufacturer = null, string manufacturerURL = null, int unitsPerEm = 1000,
                    int versionMajor = 1, int versionMinor = 0)
        {
            Glyphs = glyphs ?? new List<Glyph>();

            Copyright = copyright ?? string.Empty;
            Designer = designer ?? string.Empty;
            DesignerURL = designerURL ?? string.Empty;
            FamilyName = familyName ?? string.Empty;
            Manufacturer = manufacturer ?? string.Empty;
            ManufacturerURL = manufacturerURL ?? string.Empty;

            UnitsPerEm = unitsPerEm;
            VersionMajor = versionMajor;
            VersionMinor = versionMinor;

            foreach (var glyph in Glyphs)
            {
                glyph.Parent = this;
            }
        }

        // TODO: add accelerator
        public Glyph GetGlyph(string name)
        {
            foreach (var glyph in Glyphs)
            {
                if (glyph.Name == name)
                {
                    return glyph;
                }
            }
            return null;
        }

        public override string ToString()
        {
            return $"{nameof(Font)}({FamilyName}, v{VersionMajor}.{VersionMinor})";
        }
    }
}
