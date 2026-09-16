using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Reference to another Hue resource.
/// </summary>
public class HueResourceIdentifier
{
    /// <summary>
    /// Gets or sets the referenced resource identifier.
    /// </summary>
    [JsonPropertyName("rid")]
    public string Rid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the referenced resource type.
    /// </summary>
    [JsonPropertyName("rtype")]
    public string Rtype { get; set; } = string.Empty;
}
