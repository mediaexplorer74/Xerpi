using System;
using System.Text.Json.Serialization;

namespace Xerpi.Models.API
{
    public class ApiUser
    {
        public uint Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonPropertyName("comment_count")]
        public int CommentCount { get; set; }

        [JsonPropertyName("uploads_count")]
        public int UploadsCount { get; set; }

        [JsonPropertyName("post_count")]
        public int PostCount { get; set; }

        public object[] Links { get; set; } = Array.Empty<object>();
        public object[] Awards { get; set; } = Array.Empty<object>();
    }
}
