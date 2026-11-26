using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Color entry inside a scene palette.
/// </summary>
public class HueScenePaletteColorItem
{
    /// <summary>
    /// Gets or sets the color XY state.
    /// </summary>
    [JsonPropertyName("color")]
    public HueColorState? Color { get; set; }

    /// <summary>
    /// Gets or sets the dimming state for this palette entry.
    /// </summary>
    [JsonPropertyName("dimming")]
    public HueDimmingState? Dimming { get; set; }
}
