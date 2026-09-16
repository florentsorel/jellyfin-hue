using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Hue.Configuration;
using Jellyfin.Plugin.Hue.Hue;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Hue.Services;

/// <summary>
/// Orchestrates matching active playback sessions to user profiles and dispatching light updates.
/// </summary>
public class HueOrchestrator
{
    private readonly HueClient _hueClient;
    private readonly ILogger<HueOrchestrator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HueOrchestrator"/> class.
    /// </summary>
    /// <param name="hueClient">The Hue client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public HueOrchestrator(HueClient hueClient, ILogger<HueOrchestrator> logger)
    {
        _hueClient = hueClient;
        _logger = logger;
    }

    /// <summary>
    /// Handles PlaybackStart event.
    /// </summary>
    /// <param name="session">Session information.</param>
    /// <param name="item">Media item playing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task HandlePlayAsync(SessionInfo? session, BaseItem? item, CancellationToken cancellationToken = default)
    {
        await EvaluateAndApplyAsync(session, item, "Play", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles PlaybackProgress event when paused.
    /// </summary>
    /// <param name="session">Session information.</param>
    /// <param name="item">Media item playing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task HandlePauseAsync(SessionInfo? session, BaseItem? item, CancellationToken cancellationToken = default)
    {
        await EvaluateAndApplyAsync(session, item, "Pause", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles PlaybackProgress event when resumed.
    /// </summary>
    /// <param name="session">Session information.</param>
    /// <param name="item">Media item playing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task HandleResumeAsync(SessionInfo? session, BaseItem? item, CancellationToken cancellationToken = default)
    {
        await EvaluateAndApplyAsync(session, item, "Resume", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles PlaybackStopped event.
    /// </summary>
    /// <param name="session">Session information.</param>
    /// <param name="item">Media item playing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public async Task HandleStopAsync(SessionInfo? session, BaseItem? item, CancellationToken cancellationToken = default)
    {
        await EvaluateAndApplyAsync(session, item, "Stop", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Evaluates which profiles match the given session and item.
    /// </summary>
    /// <param name="session">The session information.</param>
    /// <param name="item">The media item.</param>
    /// <returns>A collection of matching enabled profiles.</returns>
    public Collection<HueProfile> FindMatchingProfiles(SessionInfo? session, BaseItem? item)
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null || config.Profiles == null || config.Profiles.Length == 0)
        {
            return new Collection<HueProfile>();
        }

        var matching = new Collection<HueProfile>();
        foreach (var profile in config.Profiles)
        {
            if (!profile.Enabled)
            {
                continue;
            }

            if (!MatchesMediaFilter(profile, item))
            {
                continue;
            }

            if (!MatchesUserFilter(profile, session))
            {
                continue;
            }

            if (!MatchesDeviceFilter(profile, session))
            {
                continue;
            }

            if (!MatchesTimeFilter(profile))
            {
                continue;
            }

            matching.Add(profile);
        }

        return matching;
    }

    /// <summary>
    /// Checks if the current time matches the profile's active time window filter.
    /// </summary>
    /// <param name="profile">The Hue profile.</param>
    /// <param name="currentTime">Optional specific time for unit testing.</param>
    /// <returns>True if within the allowed window or if time filter is disabled.</returns>
    public static bool MatchesTimeFilter(HueProfile profile, TimeOnly? currentTime = null)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!profile.EnableTimeFilter)
        {
            return true;
        }

        if (!TimeOnly.TryParse(profile.TimeFilterStart, CultureInfo.InvariantCulture, out var start) ||
            !TimeOnly.TryParse(profile.TimeFilterEnd, CultureInfo.InvariantCulture, out var end))
        {
            return true;
        }

        if (start == end)
        {
            return true;
        }

        var now = currentTime ?? TimeOnly.FromDateTime(DateTime.Now);

        if (start < end)
        {
            // Same day range (e.g., 14:00 to 18:00)
            return now >= start && now <= end;
        }

        // Overnight wrap-around range (e.g., 20:00 to 06:00)
        return now >= start || now <= end;
    }

    private static bool MatchesMediaFilter(HueProfile profile, BaseItem? item)
    {
        if (item == null)
        {
            return false;
        }

        var isMovie = item is Movie;
        var isEpisode = item is Episode;

        if (isMovie && profile.TriggerOnMovies)
        {
            return true;
        }

        if (isEpisode && profile.TriggerOnEpisodes)
        {
            return true;
        }

        return false;
    }

    private static bool MatchesUserFilter(HueProfile profile, SessionInfo? session)
    {
        if (profile.TargetUserIds == null || profile.TargetUserIds.Length == 0)
        {
            return true;
        }

        if (session == null || session.UserId.Equals(Guid.Empty))
        {
            return false;
        }

        var userIdString = session.UserId.ToString("N");
        return profile.TargetUserIds.Any(u =>
            string.Equals(u.Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase), userIdString, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesTarget(string target, string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && string.Equals(target, value, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesDeviceFilter(HueProfile profile, SessionInfo? session)
    {
        if (profile.TargetDeviceIds == null || profile.TargetDeviceIds.Length == 0)
        {
            return true;
        }

        if (session == null)
        {
            return false;
        }

        var deviceId = session.DeviceId;
        var client = session.Client;
        var deviceName = session.DeviceName;

        return profile.TargetDeviceIds.Any(target =>
            MatchesTarget(target, deviceId) ||
            MatchesTarget(target, client) ||
            MatchesTarget(target, deviceName));
    }

    private async Task EvaluateAndApplyAsync(SessionInfo? session, BaseItem? item, string actionType, CancellationToken cancellationToken)
    {
        try
        {
            var matchingProfiles = FindMatchingProfiles(session, item);
            if (matchingProfiles.Count == 0)
            {
                return;
            }

            _logger.LogInformation(
                "Triggering Hue action {ActionType} for item {ItemName} across {Count} matched profile(s)",
                actionType,
                item?.Name,
                matchingProfiles.Count);

            foreach (var profile in matchingProfiles)
            {
                await _hueClient.ApplyActionAsync(profile, actionType, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing {ActionType} in HueOrchestrator", actionType);
        }
    }
}
