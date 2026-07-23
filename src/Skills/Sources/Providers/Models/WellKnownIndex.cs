using System.Text.Json.Serialization;

namespace Skills.Sources.Providers;

internal sealed class WellKnownIndex
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    [JsonPropertyName("skills")]
    public List<WellKnownIndexEntry>? Skills { get; set; }
}
