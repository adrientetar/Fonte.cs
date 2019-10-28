// This Source Code Form is subject to the terms of the Mozilla Public License v2.0.
// See https://spdx.org/licenses/MPL-2.0.html for license information.

namespace Fonte.App.Serialization
{
    using Newtonsoft.Json;

    using System;
    using System.Linq;

    public class LayerSelectionConverter : JsonConverter<Data.Layer>
    {
        public override bool CanRead => false;

        public override Data.Layer ReadJson(JsonReader reader, Type objectType, Data.Layer existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException($"{typeof(LayerSelectionConverter)} is write-only");
        }

        public override void WriteJson(JsonWriter writer, Data.Layer layer, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            {
                var anchors = layer.Anchors.Where(anchor => anchor.IsSelected);
                if (anchors.Any())
                {
                    writer.WritePropertyName("anchors");
                    serializer.Serialize(writer, anchors);
                }
            }
            {
                var components = layer.Components.Where(component => component.IsSelected);
                if (components.Any())
                {
                    writer.WritePropertyName("components");
                    serializer.Serialize(writer, components);
                }
            }
            {
                var guidelines = layer.Guidelines.Where(guideline => guideline.IsSelected);
                if (guidelines.Any())
                {
                    writer.WritePropertyName("guidelines");
                    serializer.Serialize(writer, guidelines);
                }
            }
            {
                var paths = layer.SelectedPaths;
                if (paths.Any())
                {
                    writer.WritePropertyName("paths");
                    serializer.Serialize(writer, paths);
                }
            }
        }
    }
}
