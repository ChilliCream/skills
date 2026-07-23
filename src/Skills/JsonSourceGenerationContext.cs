using System.Text.Json.Serialization;
using Skills.Commands;
using Skills.Locking;
using Skills.Net;
using Skills.Plugins;
using Skills.Sources.Providers;

namespace Skills;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(object))]
[JsonSerializable(typeof(MarketplaceManifest))]
[JsonSerializable(typeof(SinglePluginManifest))]
[JsonSerializable(typeof(SkillLockFile))]
[JsonSerializable(typeof(SkillLockEntry))]
[JsonSerializable(typeof(LocalSkillLockFile))]
[JsonSerializable(typeof(LocalSkillLockEntry))]
[JsonSerializable(typeof(GitHubTreeResponse))]
[JsonSerializable(typeof(WellKnownIndex))]
[JsonSerializable(typeof(InstalledSkillJson[]))]
internal partial class JsonSourceGenerationContext : JsonSerializerContext;
