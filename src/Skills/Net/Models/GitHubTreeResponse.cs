using System.Text.Json.Serialization;

namespace Skills.Net;

internal sealed class GitHubTreeResponse
{
    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;

    [JsonPropertyName("tree")]
    public List<TreeEntry>? Tree { get; set; }
}
