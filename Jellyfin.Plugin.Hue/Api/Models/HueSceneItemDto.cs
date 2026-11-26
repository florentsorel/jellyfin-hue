using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Api.Models;

/// <summary>
/// Simplified Hue scene item DTO for UI selection cards.
/// </summary>
public class HueSceneItemDto
{
    /// <summary>
    /// Gets or sets the scene resource ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scene friendly name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the associated group ID (matches grouped_light ID used in TargetGroupIds).
    /// </summary>
    [JsonPropertyName("groupId")]
    public string? GroupId { get; set; }

    /// <summary>
    /// Gets or sets the friendly name of the room or zone owning this scene.
    /// </summary>
    [JsonPropertyName("groupName")]
    public string? GroupName { get; set; }

    /// <summary>
    /// Gets the extracted hex colors representing the scene palette for UI preview.
    /// </summary>
    [JsonPropertyName("paletteColors")]
    public Collection<string> PaletteColors { get; } = new();
}
