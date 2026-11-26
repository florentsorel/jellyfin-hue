using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Scene resource in CLIP API v2.
/// </summary>
public class HueSceneResource : HueResource
{
    /// <summary>
    /// Gets or sets the group (room or zone) that owns this scene.
    /// </summary>
    [JsonPropertyName("group")]
    public HueResourceIdentifier? Group { get; set; }
}
