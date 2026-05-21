using System.Text.Json.Serialization;

namespace Api.Models
{
    public sealed class HackerNewsStory
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("by")]
        public string? By { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }

        [JsonPropertyName("score")]
        public int Score { get; init; }

        [JsonPropertyName("time")]
        public long Time { get; init; }

        [JsonPropertyName("descendants")]
        public int? Descendants { get; init; }

        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("kids")]
        public int[]? Kids { get; init; }
    }
}
