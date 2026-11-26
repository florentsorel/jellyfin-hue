using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Hue.Api.Models;
using Jellyfin.Plugin.Hue.Configuration;
using Jellyfin.Plugin.Hue.Hue;
using MediaBrowser.Common.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Hue.Api;

/// <summary>
/// API endpoints for Philips Hue Bridge discovery, pairing, resource listing, and test control.
/// </summary>
[ApiController]
[Authorize(Policy = "RequiresElevation")]
[Route("Hue")]
[Produces(MediaTypeNames.Application.Json)]
public class HueApiController : ControllerBase
{
    private readonly HueClient _hueClient;
    private readonly HueDiscovery _discovery;
    private readonly ILogger<HueApiController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HueApiController"/> class.
    /// </summary>
    /// <param name="hueClient">Hue client instance.</param>
    /// <param name="discovery">Hue discovery instance.</param>
    /// <param name="logger">Logger instance.</param>
    public HueApiController(HueClient hueClient, HueDiscovery discovery, ILogger<HueApiController> logger)
    {
        _hueClient = hueClient;
        _discovery = discovery;
        _logger = logger;
    }

    /// <summary>
    /// Discovers Philips Hue bridges on the network.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of discovered bridges.</returns>
    [HttpGet("Discover")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<object>>> DiscoverBridges(CancellationToken cancellationToken)
    {
        var bridges = await _discovery.DiscoverBridgesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(bridges);
    }

    /// <summary>
    /// Attempts to pair with a Hue bridge at the specified IP address.
    /// </summary>
    /// <param name="request">Pairing request containing bridge IP.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Pairing result.</returns>
    [HttpPost("Pair")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> PairBridge([FromBody] PairRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.BridgeIp))
        {
            return BadRequest(new { success = false, message = "Bridge IP address is required." });
        }

        var result = await _hueClient.PairAsync(request.BridgeIp.Trim(), cancellationToken).ConfigureAwait(false);
        if (result.Success)
        {
            var bridgeTz = await _hueClient.GetBridgeTimeZoneAsync(cancellationToken).ConfigureAwait(false);
            if (Plugin.Instance is { } plugin)
            {
                plugin.Configuration.BridgeIp = request.BridgeIp.Trim();
                plugin.Configuration.BridgeUsername = result.Username ?? string.Empty;
                plugin.Configuration.BridgeClientKey = result.ClientKey ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(bridgeTz) && string.IsNullOrWhiteSpace(plugin.Configuration.TimeZoneId))
                {
                    plugin.Configuration.TimeZoneId = bridgeTz;
                }

                plugin.SaveConfiguration();
            }

            return Ok(new
            {
                success = true,
                username = result.Username,
                clientKey = result.ClientKey,
                bridgeTimeZone = bridgeTz,
            });
        }

        return Ok(new
        {
            success = false,
            message = result.ErrorMessage ?? "Link button not pressed. Please press the button on your Hue Bridge and try again.",
        });
    }

    /// <summary>
    /// Gets all available rooms, zones, lights, and scenes from the configured bridge.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Available lighting resources.</returns>
    [HttpGet("Resources")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<HueResourcesResponse>> GetResources(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GetResources called");
        var rooms = await _hueClient.GetRoomsAsync(cancellationToken).ConfigureAwait(false);
        var zones = await _hueClient.GetZonesAsync(cancellationToken).ConfigureAwait(false);
        var lights = await _hueClient.GetLightsAsync(cancellationToken).ConfigureAwait(false);
        var scenes = await _hueClient.GetScenesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Hue resources fetched: rooms={RoomCount}, zones={ZoneCount}, lights={LightCount}, scenes={SceneCount}",
            rooms.Count,
            zones.Count,
            lights.Count,
            scenes.Count);

        var colorDeviceIds = lights
            .Where(l => l.Color != null && l.Owner?.Rid != null)
            .Select(l => l.Owner!.Rid)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var colorTemperatureDeviceIds = lights
            .Where(l => l.ColorTemperature != null && l.Owner?.Rid != null)
            .Select(l => l.Owner!.Rid)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var groupMap = new Dictionary<string, (string GroupedLightId, string Name)>(StringComparer.OrdinalIgnoreCase);

        var response = new HueResourcesResponse();
        foreach (var r in rooms.OrderBy(r => r.Metadata?.Name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase))
        {
            var hasColor = r.Children.Count == 0 || r.Children.Any(c => colorDeviceIds.Contains(c.Rid));
            var hasCt = r.Children.Count == 0 || r.Children.Any(c => colorTemperatureDeviceIds.Contains(c.Rid));
            var groupedLightId = r.Services.FirstOrDefault(s => s.Rtype == "grouped_light")?.Rid ?? r.Id;
            var roomName = r.Metadata?.Name ?? "Room";

            groupMap[r.Id] = (groupedLightId, roomName);

            response.Rooms.Add(new HueResourceItemDto
            {
                Id = groupedLightId,
                Name = roomName,
                Archetype = r.Metadata?.Archetype,
                SupportsColor = hasColor,
                SupportsColorTemperature = hasCt,
                SupportsDimming = true,
            });
        }

        foreach (var z in zones.OrderBy(z => z.Metadata?.Name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase))
        {
            var hasColor = z.Children.Count == 0 || z.Children.Any(c => colorDeviceIds.Contains(c.Rid));
            var hasCt = z.Children.Count == 0 || z.Children.Any(c => colorTemperatureDeviceIds.Contains(c.Rid));
            var groupedLightId = z.Services.FirstOrDefault(s => s.Rtype == "grouped_light")?.Rid ?? z.Id;
            var zoneName = z.Metadata?.Name ?? "Zone";

            groupMap[z.Id] = (groupedLightId, zoneName);

            response.Zones.Add(new HueResourceItemDto
            {
                Id = groupedLightId,
                Name = zoneName,
                Archetype = z.Metadata?.Archetype,
                SupportsColor = hasColor,
                SupportsColorTemperature = hasCt,
                SupportsDimming = true,
            });
        }

        foreach (var l in lights.OrderBy(l => l.Metadata?.Name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase))
        {
            response.Lights.Add(new HueResourceItemDto
            {
                Id = l.Id,
                Name = l.Metadata?.Name ?? "Light",
                Archetype = l.Metadata?.Archetype,
                SupportsColor = l.Color != null,
                SupportsColorTemperature = l.ColorTemperature != null,
                SupportsDimming = l.Dimming != null,
            });
        }

        var mappedScenes = scenes
            .Select(s =>
            {
                string? groupedLightId = null;
                string? groupName = null;
                if (s.Group?.Rid != null && groupMap.TryGetValue(s.Group.Rid, out var grp))
                {
                    groupedLightId = grp.GroupedLightId;
                    groupName = grp.Name;
                }
                else if (s.Group?.Rid != null)
                {
                    groupedLightId = s.Group.Rid;
                }

                var dto = new HueSceneItemDto
                {
                    Id = s.Id,
                    Name = s.Metadata?.Name ?? "Scene",
                    GroupId = groupedLightId,
                    GroupName = groupName,
                };

                foreach (var hex in ExtractScenePaletteColors(s))
                {
                    dto.PaletteColors.Add(hex);
                }

                return dto;
            })
            .OrderBy(s => s.GroupName ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase);

        foreach (var sceneDto in mappedScenes)
        {
            response.Scenes.Add(sceneDto);
        }

        response.BridgeTimeZone = await _hueClient.GetBridgeTimeZoneAsync(cancellationToken).ConfigureAwait(false);
        return Ok(response);
    }

    private static List<string> ExtractScenePaletteColors(Jellyfin.Plugin.Hue.Hue.Models.HueSceneResource scene)
    {
        var colors = new List<string>();

        if (scene.Palette != null)
        {
            foreach (var c in scene.Palette.Color)
            {
                if (c.Color?.Xy != null)
                {
                    var hex = ColorUtils.XyToHex(c.Color.Xy);
                    if (!colors.Contains(hex, StringComparer.OrdinalIgnoreCase))
                    {
                        colors.Add(hex);
                    }
                }
            }

            if (colors.Count == 0)
            {
                foreach (var ct in scene.Palette.ColorTemperature)
                {
                    if (ct.ColorTemperature?.Mirek != null)
                    {
                        var hex = ColorUtils.MirekToHex(ct.ColorTemperature.Mirek.Value);
                        if (!colors.Contains(hex, StringComparer.OrdinalIgnoreCase))
                        {
                            colors.Add(hex);
                        }
                    }
                }
            }
        }

        if (colors.Count == 0 && scene.Actions.Count > 0)
        {
            foreach (var act in scene.Actions)
            {
                if (act.Action?.Color?.Xy != null)
                {
                    var hex = ColorUtils.XyToHex(act.Action.Color.Xy);
                    if (!colors.Contains(hex, StringComparer.OrdinalIgnoreCase))
                    {
                        colors.Add(hex);
                    }
                }
                else if (act.Action?.ColorTemperature?.Mirek != null)
                {
                    var hex = ColorUtils.MirekToHex(act.Action.ColorTemperature.Mirek.Value);
                    if (!colors.Contains(hex, StringComparer.OrdinalIgnoreCase))
                    {
                        colors.Add(hex);
                    }
                }
            }
        }

        if (colors.Count == 0)
        {
            colors.Add("#FFB366");
        }

        return colors.Take(5).ToList();
    }

    /// <summary>
    /// Tests a light or group by triggering a pulse.
    /// </summary>
    /// <param name="request">Target request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the test trigger.</returns>
    [HttpPost("Test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> TestTarget([FromBody] TestTargetRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.TargetId))
        {
            return BadRequest(new { success = false, message = "Target ID is required." });
        }

        var ok = await _hueClient.TestTargetAsync(request.TargetId, request.IsGroup, cancellationToken).ConfigureAwait(false);
        return Ok(new { success = ok });
    }

    /// <summary>
    /// Tests a profile by temporarily applying its play state and restoring previous state.
    /// </summary>
    /// <param name="profileId">The ID of the profile to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the profile test.</returns>
    [HttpPost("TestProfile/{profileId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> TestProfile(string profileId, CancellationToken cancellationToken)
    {
        var config = Plugin.Instance?.Configuration;
        var profile = config?.Profiles?.FirstOrDefault(p => p.Id == profileId);
        if (profile == null)
        {
            return NotFound(new { success = false, message = "Profile not found." });
        }

        var ok = await _hueClient.TestProfileAsync(profile, cancellationToken).ConfigureAwait(false);
        return Ok(new { success = ok });
    }
}
