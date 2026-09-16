using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// CIE XY coordinate pair.
/// </summary>
public class HueXyPoint
{
    /// <summary>
    /// Gets or sets the X coordinate.
    /// </summary>
    [JsonPropertyName("x")]
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y coordinate.
    /// </summary>
    [JsonPropertyName("y")]
    public double Y { get; set; }
}
