using System.Text.Json.Serialization;

namespace Skills.Plugins;

internal sealed class MarketplaceMetadata
{
    [JsonPropertyName("pluginRoot")]
    public string? PluginRoot { get; set; }
}
