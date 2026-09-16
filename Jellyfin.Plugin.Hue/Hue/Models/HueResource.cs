using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Base resource representation in CLIP API v2.
/// </summary>
public class HueResource
{
    /// <summary>
    /// Gets or sets the resource identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resource type.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resource metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public HueMetadata? Metadata { get; set; }
}
