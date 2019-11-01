// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.Data
{
    using Fonte.Data.Changes;
    using Fonte.Data.Collections;
    using Fonte.Data.Converters;
    using Newtonsoft.Json;

    using System.Collections.Generic;

    [JsonConverter(typeof(ObjectArrayConverter<AlignmentZone>))]
    public struct AlignmentZone
    {
        [JsonProperty("position", Order = 1)]
        public int Position { get; set; }

        [JsonProperty("size", Order = 2)]
        public int Size { get; set; }
    }

    public partial class Master
    {
        internal List<Guideline> _guidelines;

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("location")]
        public Dictionary<string, int> Location { get; }

        [JsonProperty("alignmentZones")]
        public List<AlignmentZone> AlignmentZones { get; }

        [JsonProperty("guidelines")]
        public ObserverList<Guideline> Guidelines
        {
            get
            {
                var items = new ObserverList<Guideline>(_guidelines);
                items.ChangeRequested += (sender, args) =>
                {
                    if (args.Action == NotifyChangeRequestedAction.Add)
                    {
                        new MasterGuidelinesChange(this, args.NewStartingIndex, args.NewItems, true).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Remove)
                    {
                        new MasterGuidelinesChange(this, args.OldStartingIndex, args.OldItems, false).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Replace)
                    {
                        new MasterGuidelinesReplaceChange(this, args.NewStartingIndex, args.NewItems).Apply();
                    }
                    else if (args.Action == NotifyChangeRequestedAction.Reset)
                    {
                        new MasterGuidelinesResetChange(this).Apply();
                    }
                };
                return items;
            }
        }

        [JsonProperty("hStems")]
        public List<float> HStems { get; }

        [JsonProperty("vStems")]
        public List<float> VStems { get; }

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

        [JsonIgnore]
        public bool Visible { get; set; }

        [JsonConstructor]
        public Master(string name = default, Dictionary<string, int> location = null, List<AlignmentZone> alignmentZones = null,
                      List<Guideline> guidelines = null, List<float> hStems = null, List<float> vStems = null,
                      int ascender = 800, int capHeight = 700, int descender = -200, float italicAngle = 0f, int xHeight = 500)
        {
            Name = name ?? string.Empty;
            Location = location ?? new Dictionary<string, int>();
            AlignmentZones = alignmentZones ?? new List<AlignmentZone>();
            _guidelines = guidelines ?? new List<Guideline>();
            HStems = hStems ?? new List<float>();
            VStems = vStems ?? new List<float>();

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
            var more = string.Empty; // XXX
            return $"{nameof(Master)}({Name}{more})";
        }
    }
}
