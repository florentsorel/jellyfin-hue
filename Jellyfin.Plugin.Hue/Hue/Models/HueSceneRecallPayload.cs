using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Command payload sent to recall/activate a scene in CLIP API v2.
/// </summary>
public class HueSceneRecallPayload
{
    /// <summary>
    /// Gets or sets the recall action state.
    /// </summary>
    [JsonPropertyName("recall")]
    public HueSceneRecallState Recall { get; set; } = new();
}
