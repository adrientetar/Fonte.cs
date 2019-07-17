/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

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
        public Dictionary<string, int> Location { get; set; }

        [JsonProperty("alignmentZones")]
        public List<AlignmentZone> AlignmentZones { get; set; }

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

        [JsonIgnore]
        public bool Visible { get; set; }

        [JsonConstructor]
        public Master(string name = default, Dictionary<string, int> location = default, List<AlignmentZone> alignmentZones = null,
                      List<Guideline> guidelines = default, List<int> hStems = default, List<int> vStems = default,
                      int ascender = 800, int capHeight = 700, int descender = -200, float italicAngle = 0f, int xHeight = 500)
        {
            Name = name ?? string.Empty;
            Location = location ?? new Dictionary<string, int>();
            AlignmentZones = alignmentZones ?? new List<AlignmentZone>();
            _guidelines = guidelines ?? new List<Guideline>();
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
            var more = string.Empty; // XXX
            return $"{nameof(Master)}({Name}{more})";
        }
    }
}
