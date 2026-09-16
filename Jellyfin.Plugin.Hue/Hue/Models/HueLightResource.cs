using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Light resource in CLIP API v2.
/// </summary>
public class HueLightResource : HueResource
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

    /// <summary>
    /// Gets or sets the color temperature state.
    /// </summary>
    [JsonPropertyName("color_temperature")]
    public HueColorTemperatureState? ColorTemperature { get; set; }

    /// <summary>
    /// Gets or sets the owner device reference.
    /// </summary>
    [JsonPropertyName("owner")]
    public HueResourceIdentifier? Owner { get; set; }
}
