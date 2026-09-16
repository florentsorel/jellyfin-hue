using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Dynamics parameters for state transitions.
/// </summary>
public class HueDynamics
{
    /// <summary>
    /// Gets or sets the transition duration in milliseconds.
    /// </summary>
    [JsonPropertyName("duration")]
    public int Duration { get; set; }
}
