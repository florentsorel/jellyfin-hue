using System;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Generic CLIP API v2 response envelope.
/// </summary>
/// <typeparam name="T">The resource data type.</typeparam>
public class HueResponse<T>
{
    /// <summary>
    /// Gets the collection of returned resources.
    /// </summary>
    [JsonPropertyName("data")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<T> Data { get; } = new();

    /// <summary>
    /// Gets the collection of errors returned by the bridge.
    /// </summary>
    [JsonPropertyName("errors")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<HueError> Errors { get; } = new();
}
