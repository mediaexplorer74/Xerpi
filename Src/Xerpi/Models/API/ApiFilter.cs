using System;
using System.Text.Json.Serialization;

namespace Xerpi.Models.API
{
    public class ApiFilter
    {
        public uint Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("hidden_tag_ids")]
        public uint[] HiddenTagIds { get; set; } = Array.Empty<uint>();

        [JsonPropertyName("hidden_tags")]
        public string[] HiddenTags { get; set; } = Array.Empty<string>();

        [JsonPropertyName("spoilered_tag_ids")]
        public uint[] SpoileredTagIds { get; set; } = Array.Empty<uint>();

        [JsonPropertyName("spoilered_tags")]
        public string[] SpoileredTags { get; set; } = Array.Empty<string>();

        [JsonPropertyName("hidden_complex")]
        public string HiddenComplex { get; set; } = string.Empty;

        [JsonPropertyName("spoilered_complex")]
        public string SpoileredComplex { get; set; } = string.Empty;
        public bool Public { get; set; }
        public bool System { get; set; }

        [JsonPropertyName("user_count")]
        public uint UserCount { get; set; }

        [JsonPropertyName("user_id")]
        public uint? UserId { get; set; }
    }
}
