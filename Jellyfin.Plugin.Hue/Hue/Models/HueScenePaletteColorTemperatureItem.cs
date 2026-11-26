using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Color temperature entry inside a scene palette.
/// </summary>
public class HueScenePaletteColorTemperatureItem
{
    /// <summary>
    /// Gets or sets the color temperature state.
    /// </summary>
    [JsonPropertyName("color_temperature")]
    public HueColorTemperatureState? ColorTemperature { get; set; }

    /// <summary>
    /// Gets or sets the dimming state for this palette entry.
    /// </summary>
    [JsonPropertyName("dimming")]
    public HueDimmingState? Dimming { get; set; }
}
