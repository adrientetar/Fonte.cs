/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Newtonsoft.Json;

    using System.Collections.Generic;

    public partial class Master
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("location")]
        public Dictionary<string, int> Location { get; set; }

        //[JsonProperty("alignmentZones")]
        //public string AlignmentZones { get; set; }

        [JsonProperty("guidelines")]
        public List<Guideline> Guidelines { get; }

        [JsonProperty("hStems")]
        public List<int> HStems { get; }

        [JsonProperty("vStems")]
        public List<int> VStems { get; }

        [JsonProperty("ascender")]
        public int Ascender { get; set; }

        [JsonProperty("capHeight")]
        public int CapHeight { get; set; }

        [JsonProperty("descender")]
        public int Descender { get; set; }

        [JsonProperty("italicAngle")]
        public float ItalicAngle { get; set; }

        [JsonProperty("xHeight")]
        public int XHeight { get; set; }

        /**/

        [JsonIgnore]
        public Font Parent { get; internal set; }

        [JsonConstructor]
        public Master(string name = default, Dictionary<string, int> location = default, List<Guideline> guidelines = default,
                      List<int> hStems = default, List<int> vStems = default, int ascender = 800, int capHeight = 700,
                      int descender = -200, float italicAngle = 0f, int xHeight = 500)
        {
            Name = name ?? string.Empty;
            Location = location ?? new Dictionary<string, int>();
            Guidelines = guidelines ?? new List<Guideline>();
            HStems = hStems ?? new List<int>();
            VStems = vStems ?? new List<int>();

            Ascender = ascender;
            CapHeight = capHeight;
            Descender = descender;
            ItalicAngle = italicAngle;
            XHeight = xHeight;

            foreach (var guideline in Guidelines)
            {
                guideline.Parent = this;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Master)}(...)";  // XXX
        }
    }
}
