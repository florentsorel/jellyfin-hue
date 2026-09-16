using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Api.Models;

/// <summary>
/// Simplified Hue resource item DTO for UI dropdowns and checkboxes.
/// </summary>
public class HueResourceItemDto
{
    /// <summary>
    /// Gets or sets the resource ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the friendly name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the resource archetype / type icon.
    /// </summary>
    [JsonPropertyName("archetype")]
    public string? Archetype { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this resource supports color output.
    /// </summary>
    [JsonPropertyName("supportsColor")]
    public bool SupportsColor { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this resource supports color temperature.
    /// </summary>
    [JsonPropertyName("supportsColorTemperature")]
    public bool SupportsColorTemperature { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this resource supports dimming.
    /// </summary>
    [JsonPropertyName("supportsDimming")]
    public bool SupportsDimming { get; set; } = true;
}
