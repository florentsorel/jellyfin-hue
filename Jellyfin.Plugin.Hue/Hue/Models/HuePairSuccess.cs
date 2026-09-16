using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Response returned from Hue Bridge pairing endpoint upon success.
/// </summary>
public class HuePairSuccess
{
    /// <summary>
    /// Gets or sets the generated username (application key).
    /// </summary>
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the generated client key.
    /// </summary>
    [JsonPropertyName("clientkey")]
    public string? ClientKey { get; set; }
}
