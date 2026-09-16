using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Resource metadata (name, archetype, etc.).
/// </summary>
public class HueMetadata
{
    /// <summary>
    /// Gets or sets the resource display name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the archetype icon or kind.
    /// </summary>
    [JsonPropertyName("archetype")]
    public string? Archetype { get; set; }
}
