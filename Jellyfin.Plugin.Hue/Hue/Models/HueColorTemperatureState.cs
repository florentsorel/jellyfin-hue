using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Color temperature state in Mirek.
/// </summary>
public class HueColorTemperatureState
{
    /// <summary>
    /// Gets or sets the Mirek value (153 = coldest ~6500K, 500 = warmest ~2000K).
    /// </summary>
    [JsonPropertyName("mirek")]
    public int? Mirek { get; set; }
}
