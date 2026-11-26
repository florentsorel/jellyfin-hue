using System.Collections.ObjectModel;
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

    /// <summary>
    /// Gets or sets the color and temperature palette of this scene.
    /// </summary>
    [JsonPropertyName("palette")]
    public HueScenePalette? Palette { get; set; }

    /// <summary>
    /// Gets the individual light actions configured in this scene.
    /// </summary>
    [JsonPropertyName("actions")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<HueSceneActionEntry> Actions { get; } = new();
}
