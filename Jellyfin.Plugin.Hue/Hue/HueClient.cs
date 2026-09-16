using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Hue.Configuration;
using Jellyfin.Plugin.Hue.Hue.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Hue.Hue;

/// <summary>
/// Client communicating with a Philips Hue Bridge using CLIP API v2.
/// </summary>
public class HueClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<HueClient> _logger;

    // Stores saved state per light ID to restore on playback stop
    private readonly ConcurrentDictionary<string, HueLightUpdatePayload> _savedLightStates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="HueClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client configured for local Bridge communication.</param>
    /// <param name="logger">The logger instance.</param>
    public HueClient(HttpClient httpClient, ILogger<HueClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to pair with the Hue Bridge by requesting an application key (link button must be pressed).
    /// </summary>
    /// <param name="bridgeIp">The IP address of the Hue Bridge.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing success status, username/appkey, clientkey, and error message if any.</returns>
    public async Task<(bool Success, string? Username, string? ClientKey, string? ErrorMessage)> PairAsync(string bridgeIp, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = new Uri($"https://{bridgeIp}/api");
            var body = new
            {
                devicetype = "jellyfin#server",
                generateclientkey = true,
            };

            var request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var items = JsonSerializer.Deserialize<List<HuePairResponseItem>>(content, JsonOptions);
            if (items != null && items.Count > 0)
            {
                var first = items[0];
                if (first.Success != null && !string.IsNullOrWhiteSpace(first.Success.Username))
                {
                    _logger.LogInformation("Successfully paired with Hue Bridge at {BridgeIp}", bridgeIp);
                    return (true, first.Success.Username, first.Success.ClientKey, null);
                }

                if (first.Error != null)
                {
                    return (false, null, null, first.Error.Description);
                }
            }

            return (false, null, null, "Unexpected response from Hue Bridge");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while attempting to pair with Hue Bridge at {BridgeIp}", bridgeIp);
            return (false, null, null, ex.Message);
        }
    }

    /// <summary>
    /// Fetches all rooms from the Hue Bridge.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of room group resources.</returns>
    public async Task<Collection<HueGroupResource>> GetRoomsAsync(CancellationToken cancellationToken = default)
    {
        return await GetResourcesAsync<HueGroupResource>("room", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches all zones from the Hue Bridge.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of zone group resources.</returns>
    public async Task<Collection<HueGroupResource>> GetZonesAsync(CancellationToken cancellationToken = default)
    {
        return await GetResourcesAsync<HueGroupResource>("zone", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches all individual lights from the Hue Bridge.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of light resources.</returns>
    public async Task<Collection<HueLightResource>> GetLightsAsync(CancellationToken cancellationToken = default)
    {
        return await GetResourcesAsync<HueLightResource>("light", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches all grouped light resources from the Hue Bridge.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of grouped light resources.</returns>
    public async Task<Collection<HueGroupedLightResource>> GetGroupedLightsAsync(CancellationToken cancellationToken = default)
    {
        return await GetResourcesAsync<HueGroupedLightResource>("grouped_light", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies light settings to the specified targets (rooms, zones, individual lights) based on profile action.
    /// </summary>
    /// <param name="profile">The active profile.</param>
    /// <param name="actionType">The action type (Play, Pause, Resume, Stop).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task ApplyActionAsync(HueProfile profile, string actionType, CancellationToken cancellationToken = default)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || string.IsNullOrWhiteSpace(config.BridgeIp) || string.IsNullOrWhiteSpace(config.BridgeUsername))
        {
            _logger.LogWarning("Cannot apply Hue action: Bridge IP or Application Key is missing.");
            return;
        }

        try
        {
            if (string.Equals(actionType, "Play", StringComparison.OrdinalIgnoreCase))
            {
                if (profile.RestoreStateOnStop)
                {
                    await CaptureCurrentStateAsync(profile, cancellationToken).ConfigureAwait(false);
                }

                var payload = BuildPlayPayload(profile);
                await DispatchPayloadAsync(profile, payload, cancellationToken).ConfigureAwait(false);
            }
            else if (string.Equals(actionType, "Pause", StringComparison.OrdinalIgnoreCase))
            {
                var payload = BuildPausePayload(profile);
                await DispatchPayloadAsync(profile, payload, cancellationToken).ConfigureAwait(false);
            }
            else if (string.Equals(actionType, "Resume", StringComparison.OrdinalIgnoreCase))
            {
                var payload = BuildResumePayload(profile);
                await DispatchPayloadAsync(profile, payload, cancellationToken).ConfigureAwait(false);
            }
            else if (string.Equals(actionType, "Stop", StringComparison.OrdinalIgnoreCase))
            {
                if (profile.RestoreStateOnStop && !_savedLightStates.IsEmpty)
                {
                    await RestoreCapturedStatesAsync(profile, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var payload = BuildStopPayload(profile);
                    await DispatchPayloadAsync(profile, payload, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply action {ActionType} for profile {ProfileName}", actionType, profile.Name);
        }
    }

    /// <summary>
    /// Tests a light or group by applying a brief pulse/dim.
    /// </summary>
    /// <param name="targetId">Resource ID.</param>
    /// <param name="isGroup">Indicates whether the resource is a group or light.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if test succeeded.</returns>
    public async Task<bool> TestTargetAsync(string targetId, bool isGroup, CancellationToken cancellationToken = default)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || string.IsNullOrWhiteSpace(config.BridgeIp) || string.IsNullOrWhiteSpace(config.BridgeUsername))
        {
            return false;
        }

        var endpoint = isGroup ? $"grouped_light/{targetId}" : $"light/{targetId}";
        var payload = new HueLightUpdatePayload
        {
            On = new HueOnState { On = true },
            Dimming = new HueDimmingState { Brightness = 100.0 },
            Color = new HueColorState { Xy = ColorUtils.HexToXy("#FFFFFF") },
            Dynamics = new HueDynamics { Duration = 400 },
        };

        return await PutResourceAsync(endpoint, payload, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Tests a profile by capturing the current state of lights, applying the Play settings,
    /// waiting briefly so the user can preview the effect, and then restoring the initial state.
    /// </summary>
    /// <param name="profile">The profile to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if test executed successfully.</returns>
    public async Task<bool> TestProfileAsync(HueProfile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var config = Plugin.Instance?.Configuration;
        if (config == null || string.IsNullOrWhiteSpace(config.BridgeIp) || string.IsNullOrWhiteSpace(config.BridgeUsername))
        {
            return false;
        }

        try
        {
            // 1. Capture current state
            await CaptureCurrentStateAsync(profile, cancellationToken).ConfigureAwait(false);

            // 2. Step 1: Lecture (Play)
            var playPayload = BuildPlayPayload(profile);
            await DispatchPayloadAsync(profile, playPayload, cancellationToken).ConfigureAwait(false);
            var playWaitMs = Math.Max(profile.PlayTransitionDurationMs + 2000, 2500);
            await Task.Delay(playWaitMs, cancellationToken).ConfigureAwait(false);

            // 3. Step 2: Pause
            var pausePayload = BuildPausePayload(profile);
            await DispatchPayloadAsync(profile, pausePayload, cancellationToken).ConfigureAwait(false);
            var pauseWaitMs = Math.Max(profile.PauseTransitionDurationMs + 2000, 2500);
            await Task.Delay(pauseWaitMs, cancellationToken).ConfigureAwait(false);

            // 4. Step 3: Reprise (Resume)
            var resumePayload = BuildResumePayload(profile);
            await DispatchPayloadAsync(profile, resumePayload, cancellationToken).ConfigureAwait(false);
            var resumeDuration = profile.ResumeSameAsPlay ? profile.PlayTransitionDurationMs : profile.ResumeTransitionDurationMs;
            var resumeWaitMs = Math.Max(resumeDuration + 2000, 2500);
            await Task.Delay(resumeWaitMs, cancellationToken).ConfigureAwait(false);

            // 5. Step 4: Arrêt (Stop)
            if (profile.RestoreStateOnStop)
            {
                // Rétablissement direct de l'état initial des lampes
                await RestoreCapturedStatesAsync(profile, cancellationToken).ConfigureAwait(false);
                var stopWaitMs = Math.Max(profile.StopTransitionDurationMs, 1000);
                await Task.Delay(stopWaitMs, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Appliquer l'état d'arrêt configuré (luminosité/couleur)
                var stopPayload = BuildStopPayload(profile);
                await DispatchPayloadAsync(profile, stopPayload, cancellationToken).ConfigureAwait(false);
                var stopWaitMs = Math.Max(profile.StopTransitionDurationMs + 2000, 2500);
                await Task.Delay(stopWaitMs, cancellationToken).ConfigureAwait(false);

                // Puis rétablir l'état initial pré-test
                await RestoreCapturedStatesAsync(profile, cancellationToken, 1000).ConfigureAwait(false);
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test profile {ProfileName}", profile.Name);
            return false;
        }
    }

    private static HueLightUpdatePayload BuildPlayPayload(HueProfile profile)
    {
        if (profile.PlayBrightness <= 0)
        {
            return new HueLightUpdatePayload
            {
                On = new HueOnState { On = false },
                Dynamics = new HueDynamics { Duration = profile.PlayTransitionDurationMs },
            };
        }

        var payload = new HueLightUpdatePayload
        {
            On = new HueOnState { On = true },
            Dimming = new HueDimmingState { Brightness = Math.Clamp(profile.PlayBrightness, 1, 100) },
            Dynamics = new HueDynamics { Duration = profile.PlayTransitionDurationMs },
        };

        if (!string.IsNullOrWhiteSpace(profile.PlayColorHex))
        {
            payload.Color = new HueColorState { Xy = ColorUtils.HexToXy(profile.PlayColorHex) };
            var mirek = ColorUtils.HexToMirek(profile.PlayColorHex);
            if (mirek.HasValue)
            {
                payload.ColorTemperature = new HueColorTemperatureState { Mirek = mirek.Value };
            }
        }

        return payload;
    }

    private static HueLightUpdatePayload BuildPausePayload(HueProfile profile)
    {
        var xy = ColorUtils.HexToXy(profile.PauseColorHex);
        var mirek = ColorUtils.HexToMirek(profile.PauseColorHex);
        return new HueLightUpdatePayload
        {
            On = new HueOnState { On = true },
            Dimming = new HueDimmingState { Brightness = Math.Clamp(profile.PauseBrightness, 1, 100) },
            Color = new HueColorState { Xy = xy },
            ColorTemperature = mirek.HasValue ? new HueColorTemperatureState { Mirek = mirek.Value } : null,
            Dynamics = new HueDynamics { Duration = profile.PauseTransitionDurationMs },
        };
    }

    private static HueLightUpdatePayload BuildResumePayload(HueProfile profile)
    {
        if (profile.ResumeSameAsPlay)
        {
            return BuildPlayPayload(profile);
        }

        if (profile.ResumeBrightness <= 0)
        {
            return new HueLightUpdatePayload
            {
                On = new HueOnState { On = false },
                Dynamics = new HueDynamics { Duration = profile.ResumeTransitionDurationMs },
            };
        }

        return new HueLightUpdatePayload
        {
            On = new HueOnState { On = true },
            Dimming = new HueDimmingState { Brightness = Math.Clamp(profile.ResumeBrightness, 1, 100) },
            Dynamics = new HueDynamics { Duration = profile.ResumeTransitionDurationMs },
        };
    }

    private static HueLightUpdatePayload BuildStopPayload(HueProfile profile)
    {
        var xy = ColorUtils.HexToXy(profile.StopColorHex);
        var mirek = ColorUtils.HexToMirek(profile.StopColorHex);
        return new HueLightUpdatePayload
        {
            On = new HueOnState { On = true },
            Dimming = new HueDimmingState { Brightness = Math.Clamp(profile.StopBrightness, 1, 100) },
            Color = new HueColorState { Xy = xy },
            ColorTemperature = mirek.HasValue ? new HueColorTemperatureState { Mirek = mirek.Value } : null,
            Dynamics = new HueDynamics { Duration = profile.StopTransitionDurationMs },
        };
    }

    private async Task CaptureCurrentStateAsync(HueProfile profile, CancellationToken cancellationToken)
    {
        try
        {
            var lights = await GetLightsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var light in lights)
            {
                if (light.On == null)
                {
                    continue;
                }

                _savedLightStates[light.Id] = new HueLightUpdatePayload
                {
                    On = new HueOnState { On = light.On.On },
                    Dimming = light.Dimming != null ? new HueDimmingState { Brightness = light.Dimming.Brightness } : null,
                    Color = light.Color != null ? new HueColorState { Xy = light.Color.Xy } : null,
                    ColorTemperature = light.ColorTemperature != null && light.ColorTemperature.Mirek.HasValue
                        ? new HueColorTemperatureState { Mirek = light.ColorTemperature.Mirek.Value }
                        : null,
                    Dynamics = new HueDynamics { Duration = profile.StopTransitionDurationMs },
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture current lights state before playback start");
        }
    }

    private async Task RestoreCapturedStatesAsync(HueProfile profile, CancellationToken cancellationToken, int? overrideDurationMs = null)
    {
        var duration = overrideDurationMs ?? profile.StopTransitionDurationMs;
        foreach (var entry in _savedLightStates)
        {
            var lightId = entry.Key;
            var payload = entry.Value;
            payload.Dynamics = new HueDynamics { Duration = duration };
            await PutResourceAsync($"light/{lightId}", payload, cancellationToken).ConfigureAwait(false);
        }

        _savedLightStates.Clear();
    }

    private async Task DispatchPayloadAsync(HueProfile profile, HueLightUpdatePayload payload, CancellationToken cancellationToken)
    {
        // 1. Dispatch to Groups (Rooms/Zones)
        if (profile.TargetGroupIds != null)
        {
            foreach (var groupId in profile.TargetGroupIds)
            {
                if (string.IsNullOrWhiteSpace(groupId))
                {
                    continue;
                }

                await PutResourceAsync($"grouped_light/{groupId}", payload, cancellationToken).ConfigureAwait(false);
            }
        }

        // 2. Dispatch to Individual Lights
        if (profile.TargetLightIds != null)
        {
            foreach (var lightId in profile.TargetLightIds)
            {
                if (string.IsNullOrWhiteSpace(lightId))
                {
                    continue;
                }

                await PutResourceAsync($"light/{lightId}", payload, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<Collection<T>> GetResourcesAsync<T>(string resourcePath, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || string.IsNullOrWhiteSpace(config.BridgeIp) || string.IsNullOrWhiteSpace(config.BridgeUsername))
        {
            _logger.LogWarning(
                "Cannot get Hue resource {Resource}: Bridge not configured (BridgeIp={BridgeIp}, HasKey={HasKey})",
                resourcePath,
                config?.BridgeIp,
                !string.IsNullOrWhiteSpace(config?.BridgeUsername));
            return new Collection<T>();
        }

        try
        {
            var uri = new Uri($"https://{config.BridgeIp}/clip/v2/resource/{resourcePath}");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("hue-application-key", config.BridgeUsername);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Request to Hue resource {Resource} failed with status {StatusCode}", resourcePath, response.StatusCode);
                return new Collection<T>();
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<HueResponse<T>>(content, JsonOptions);
            _logger.LogInformation(
                "Hue {Resource} response parsed: {ItemCount} items, contentLen={Len}",
                resourcePath,
                result?.Data?.Count ?? 0,
                content.Length);
            return result?.Data ?? new Collection<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Hue resources from {Resource}", resourcePath);
            return new Collection<T>();
        }
    }

    private async Task<bool> PutResourceAsync(string resourceSubPath, HueLightUpdatePayload payload, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || string.IsNullOrWhiteSpace(config.BridgeIp) || string.IsNullOrWhiteSpace(config.BridgeUsername))
        {
            return false;
        }

        try
        {
            var uri = new Uri($"https://{config.BridgeIp}/clip/v2/resource/{resourceSubPath}");
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            using var request = new HttpRequestMessage(HttpMethod.Put, uri)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("hue-application-key", config.BridgeUsername);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Hue resource at {ResourceSubPath}", resourceSubPath);
            return false;
        }
    }
}
