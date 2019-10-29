// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.Data
{
    using Newtonsoft.Json;

    using System;
    using System.Collections.Generic;

    public partial class Font
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("familyName")]
        public string FamilyName { get; set; }

        [JsonProperty("glyphs")]
        public List<Glyph> Glyphs { get; }

        [JsonProperty("masters")]
        public List<Master> Masters { get; }

        [JsonProperty("copyright")]
        public string Copyright { get; set; }

        [JsonProperty("designer")]
        public string Designer { get; set; }

        [JsonProperty("designerURL")]
        public string DesignerURL { get; set; }

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

        /**/

        [JsonIgnore]
        public bool IsModified
        {
            get
            {
                // TODO: We can avoid recomputing all the time if the Glyph passes change information to Font
                foreach (var glyph in Glyphs)
                {
                    if (glyph.IsModified)
                    {
                        return true;
                    }
                }

                return false;
            }
            set
            {
                if (value)
                    throw new ArgumentException($"Cannot set {nameof(IsModified)} to true.");

                foreach (var glyph in Glyphs)
                {
                    glyph.IsModified = false;
                }
            }
        }

        [JsonConstructor]
        public Font(List<Glyph> glyphs = null, List<Master> masters = null, string copyright = null, DateTime? date = null, string designer = null, string designerURL = null,
                    string familyName = null, string manufacturer = null, string manufacturerURL = null, int unitsPerEm = 1000,
                    int versionMajor = 1, int versionMinor = 0)
        {
            Glyphs = glyphs ?? new List<Glyph>();
            Masters = masters ?? new List<Master>() { new Master(name: "Regular") };

            Copyright = copyright ?? string.Empty;
            Date = date ?? DateTime.UtcNow;
            Designer = designer ?? string.Empty;
            DesignerURL = designerURL ?? string.Empty;
            FamilyName = familyName ?? "New Font";
            Manufacturer = manufacturer ?? string.Empty;
            ManufacturerURL = manufacturerURL ?? string.Empty;

            UnitsPerEm = unitsPerEm;
            VersionMajor = versionMajor;
            VersionMinor = versionMinor;

            foreach (var glyph in Glyphs)
            {
                glyph.Parent = this;
            }
            foreach (var master in Masters)
            {
                master.Parent = this;
            }
        }

        public bool TryGetGlyph(string name, out Glyph glyph)
        {
            // TODO: add accelerator
            foreach (var g in Glyphs)
            {
                if (g.Name == name)
                {
                    glyph = g;
                    return true;
                }
            }

            glyph = new Glyph(name);
            return false;
        }

        public bool TryGetMaster(string name, out Master master)
        {
            foreach (var m in Masters)
            {
                if (m.Name == name)
                {
                    master = m;
                    return true;
                }
            }

            master = new Master(name);
            return false;
        }

        public override string ToString()
        {
            return $"{nameof(Font)}({FamilyName}, v{VersionMajor}.{VersionMinor})";
        }
    }
}
