using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Grouped light resource in CLIP API v2 (controls entire rooms/zones simultaneously).
/// </summary>
public class HueGroupedLightResource : HueResource
{
    /// <summary>
    /// Gets or sets the on/off state.
    /// </summary>
    [JsonPropertyName("on")]
    public HueOnState? On { get; set; }

    /// <summary>
    /// Gets or sets the dimming state.
    /// </summary>
    [JsonPropertyName("dimming")]
    public HueDimmingState? Dimming { get; set; }

    /// <summary>
    /// Gets or sets the color state.
    /// </summary>
    [JsonPropertyName("color")]
    public HueColorState? Color { get; set; }
}
