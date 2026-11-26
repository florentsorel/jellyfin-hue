using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Recall command inside a scene update payload in CLIP API v2.
/// </summary>
public class HueSceneRecallState
{
    /// <summary>
    /// Gets or sets the recall action (e.g. "active").
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "active";

    /// <summary>
    /// Gets or sets the transition duration in milliseconds.
    /// </summary>
    [JsonPropertyName("duration")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Duration { get; set; }

    /// <summary>
    /// Gets or sets the optional dimming override for the recalled scene.
    /// </summary>
    [JsonPropertyName("dimming")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HueDimmingState? Dimming { get; set; }
}
