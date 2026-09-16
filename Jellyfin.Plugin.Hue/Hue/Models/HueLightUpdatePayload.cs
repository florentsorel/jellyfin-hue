using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Command payload sent to update light or grouped_light state.
/// </summary>
public class HueLightUpdatePayload
{
    /// <summary>
    /// Gets or sets the on/off state to apply.
    /// </summary>
    [JsonPropertyName("on")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HueOnState? On { get; set; }

    /// <summary>
    /// Gets or sets the dimming state to apply.
    /// </summary>
    [JsonPropertyName("dimming")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HueDimmingState? Dimming { get; set; }

    /// <summary>
    /// Gets or sets the color state to apply.
    /// </summary>
    [JsonPropertyName("color")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HueColorState? Color { get; set; }

    /// <summary>
    /// Gets or sets the color temperature state to apply.
    /// </summary>
    [JsonPropertyName("color_temperature")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HueColorTemperatureState? ColorTemperature { get; set; }

    /// <summary>
    /// Gets or sets the dynamics settings (e.g. transition duration).
    /// </summary>
    [JsonPropertyName("dynamics")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HueDynamics? Dynamics { get; set; }
}
