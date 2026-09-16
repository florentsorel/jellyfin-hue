using System;
using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Hue.Configuration;

/// <summary>
/// Plugin configuration for Jellyfin Philips Hue integration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        BridgeIp = string.Empty;
        BridgeUsername = string.Empty;
        BridgeClientKey = string.Empty;
        TimeZoneId = string.Empty;
        Profiles = Array.Empty<HueProfile>();
    }

    /// <summary>
    /// Gets or sets the configured IANA or Windows time zone identifier (e.g. "Europe/Paris") for time filter evaluation.
    /// </summary>
    public string TimeZoneId { get; set; }

    /// <summary>
    /// Gets or sets the IP address or hostname of the Philips Hue Bridge.
    /// </summary>
    public string BridgeIp { get; set; }

    /// <summary>
    /// Gets or sets the application key (username) for authenticating with the Hue Bridge.
    /// </summary>
    public string BridgeUsername { get; set; }

    /// <summary>
    /// Gets or sets the client key generated during pairing.
    /// </summary>
    public string BridgeClientKey { get; set; }

    /// <summary>
    /// Gets or sets the list of configured lighting profiles.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Needed for XML serialization")]
    public HueProfile[] Profiles { get; set; }
}
