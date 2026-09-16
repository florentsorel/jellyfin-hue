using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Hue.Hue.Models;

/// <summary>
/// Response element from pairing attempt.
/// </summary>
public class HuePairResponseItem
{
    /// <summary>
    /// Gets or sets success data if button was pressed.
    /// </summary>
    [JsonPropertyName("success")]
    public HuePairSuccess? Success { get; set; }

    /// <summary>
    /// Gets or sets error data if button was not pressed or other error occurred.
    /// </summary>
    [JsonPropertyName("error")]
    public HuePairError? Error { get; set; }
}
