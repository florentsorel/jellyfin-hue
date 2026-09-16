namespace Jellyfin.Plugin.Hue.Api.Models;

/// <summary>
/// Request payload to pair with a bridge.
/// </summary>
public class PairRequest
{
    /// <summary>
    /// Gets or sets the target bridge IP.
    /// </summary>
    public string BridgeIp { get; set; } = string.Empty;
}
