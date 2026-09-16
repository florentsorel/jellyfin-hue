using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Room or Zone group resource in CLIP API v2.
/// </summary>
public class HueGroupResource : HueResource
{
    /// <summary>
    /// Gets the services associated with this group.
    /// </summary>
    [JsonPropertyName("services")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<HueResourceIdentifier> Services { get; } = new();

    /// <summary>
    /// Gets the children resources (e.g. devices).
    /// </summary>
    [JsonPropertyName("children")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<HueResourceIdentifier> Children { get; } = new();
}
