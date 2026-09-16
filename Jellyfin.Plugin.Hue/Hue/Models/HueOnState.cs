using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// On/Off state representation.
/// </summary>
public class HueOnState
{
    /// <summary>
    /// Gets or sets a value indicating whether the light is on.
    /// </summary>
    [JsonPropertyName("on")]
    public bool On { get; set; }
}
