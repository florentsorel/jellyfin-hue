using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Api.Models;

/// <summary>
/// Combined resources response containing rooms, zones, and individual lights for UI dropdowns and checkboxes.
/// </summary>
public class HueResourcesResponse
{
    /// <summary>
    /// Gets the available rooms.
    /// </summary>
    [JsonPropertyName("rooms")]
    public Collection<HueResourceItemDto> Rooms { get; } = new();

    /// <summary>
    /// Gets the available zones.
    /// </summary>
    [JsonPropertyName("zones")]
    public Collection<HueResourceItemDto> Zones { get; } = new();

    /// <summary>
    /// Gets the available lights.
    /// </summary>
    [JsonPropertyName("lights")]
    public Collection<HueResourceItemDto> Lights { get; } = new();
}
