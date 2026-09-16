using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Cloud discovery response item from discovery.meethue.com.
/// </summary>
public class HueDiscoveredBridge
{
    /// <summary>
    /// Gets or sets the bridge ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the internal IP address.
    /// </summary>
    [JsonPropertyName("internalipaddress")]
    public string InternalIpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = 443;
}
