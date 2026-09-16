namespace Jellyfin.Plugin.Hue.Api.Models;

/// <summary>
/// Request payload to test a target light or group.
/// </summary>
public class TestTargetRequest
{
    /// <summary>
    /// Gets or sets the target resource ID.
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the target is a group (room/zone).
    /// </summary>
    public bool IsGroup { get; set; }
}
