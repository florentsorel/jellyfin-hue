using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Light action entry inside a Hue scene resource.
/// </summary>
public class HueSceneActionEntry
{
    /// <summary>
    /// Gets or sets the action applied to the target light.
    /// </summary>
    [JsonPropertyName("action")]
    public HueLightUpdatePayload? Action { get; set; }
}
