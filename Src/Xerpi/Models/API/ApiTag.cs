using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Xerpi.Converters;

namespace Xerpi.Models.API
{
    public class ApiTag : IEquatable<ApiTag>
    {
        public uint Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("short_description")]
        public string ShortDescription { get; set; } = string.Empty;

        public uint Images { get; set; }

        [JsonPropertyName("spoiler_image_url")]
        public string SpoilerImageUri { get; set; } = string.Empty;

        [JsonPropertyName("aliased_to")]
        public string? AliasedTo { get; set; }

        [JsonPropertyName("aliased_to_id")]
        public uint? AlisedToId { get; set; }

        public string Namespace { get; set; } = string.Empty;

        [JsonPropertyName("name_in_namespace")]
        public string NameInNamespace { get; set; } = string.Empty;

        [JsonPropertyName("implied_tags")]
        public string[] ImpliedTags { get; set; } = Array.Empty<string>();

        [JsonPropertyName("implied_tag_ids")]
        public uint[] ImpliedTagIds { get; set; } = Array.Empty<uint>();

        [JsonConverter(typeof(TagCategoryConverter))]
        public TagCategory Category { get; set; }

        [JsonIgnore]
        public string TagString => $"{Name} ({Images})";

        public override bool Equals(object? obj)
        {
            return Equals(obj as ApiTag);
        }

        public bool Equals(ApiTag? other)
        {
            return other != null &&
                   Id == other.Id &&
                   Images == other.Images;
        }

        public override int GetHashCode()
        {
            var hashCode = -1784744757;
            hashCode = hashCode * -1521134295 + Id.GetHashCode();
            hashCode = hashCode * -1521134295 + Images.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(ApiTag? left, ApiTag? right)
        {
            if (left is null)
            {
                return right is null;
            }
            return left.Equals(right);
        }

        public static bool operator !=(ApiTag? left, ApiTag? right)
        {
            return !(left == right);
        }
    }

    public enum TagCategory
    {
        Rating = 0,
        Origin = 1,
        Character = 2,
        Species = 3,
        ContentOfficial = 4,
        ContentFanmade = 5,
        Spoiler = 6,
        OC = 7,
        None = 8,

        Unmapped = 99,
    }
}
