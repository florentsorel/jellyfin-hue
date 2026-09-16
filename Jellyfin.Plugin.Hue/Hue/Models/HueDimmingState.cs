using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Dimming state representation.
/// </summary>
public class HueDimmingState
{
    /// <summary>
    /// Gets or sets the brightness percentage (0.0 to 100.0).
    /// </summary>
    [JsonPropertyName("brightness")]
    public double Brightness { get; set; }
}
