using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Palette definition inside a Hue scene resource.
/// </summary>
public class HueScenePalette
{
    /// <summary>
    /// Gets the color entries in the scene palette.
    /// </summary>
    [JsonPropertyName("color")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<HueScenePaletteColorItem> Color { get; } = new();

    /// <summary>
    /// Gets the color temperature entries in the scene palette.
    /// </summary>
    [JsonPropertyName("color_temperature")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<HueScenePaletteColorTemperatureItem> ColorTemperature { get; } = new();

    /// <summary>
    /// Gets the dimming entries in the scene palette.
    /// </summary>
    [JsonPropertyName("dimming")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<HueDimmingState> Dimming { get; } = new();
}
