using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Color state representation in CIE XY space.
/// </summary>
public class HueColorState
{
    /// <summary>
    /// Gets or sets the XY coordinates.
    /// </summary>
    [JsonPropertyName("xy")]
    public HueXyPoint? Xy { get; set; }
}
