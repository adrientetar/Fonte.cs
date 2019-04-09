/**
 * Copyright 2018, Adrien Tétar. All Rights Reserved.
 */

namespace Fonte.Data
{
    using Fonte.Data.Interfaces;
    using Fonte.Data.Utilities;
    using Newtonsoft.Json;

    using System.Collections.Generic;

    public partial class Glyph
    {
        private UndoStore _undoStore = new UndoStore();

        [JsonProperty("name")]
        public string Name { get; private set; }

        [JsonProperty("unicodes")]
        public IReadOnlyList<string> Unicodes { get; private set; }

        /* For kerning groups, make a struct kinda like a rect containing 4 strings? */

        [JsonProperty("layers")]
        public IReadOnlyList<Layer> Layers { get; }

        /**/

        [JsonIgnore]
        public bool Selected { get; private set; }

        [JsonIgnore]
        public IUndoProvider UndoStore => _undoStore;

        internal void OnChange(IChange change)
        {
            _undoStore.ProcessChange(change);
        }
    }
}
