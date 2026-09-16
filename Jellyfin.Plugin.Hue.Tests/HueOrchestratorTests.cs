using System;
using System.Net.Http;
using Jellyfin.Plugin.Hue.Configuration;
using Jellyfin.Plugin.Hue.Hue;
using Jellyfin.Plugin.Hue.Services;
using Jellyfin.Plugin.Hue.Tests.Mocks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.Hue.Tests;

public class HueOrchestratorTests
{
    private readonly Guid _userId = Guid.NewGuid();

    private HueOrchestrator CreateOrchestrator(PluginConfiguration configuration, MockHttpMessageHandler? handler = null)
    {
        PluginTestHelper.CreateMockPlugin(configuration);
        var httpHandler = handler ?? new MockHttpMessageHandler();
        var httpClient = new HttpClient(httpHandler);
        var hueClient = new HueClient(httpClient, NullLogger<HueClient>.Instance);
        return new HueOrchestrator(hueClient, NullLogger<HueOrchestrator>.Instance);
    }

    [Fact]
    public void FindMatchingProfiles_MatchesUserAndDevice_WhenSpecified()
    {
        var profile = new HueProfile
        {
            Id = "p1",
            Name = "Salon Profile",
            Enabled = true,
            TargetUserIds = new[] { _userId.ToString("N") },
            TargetDeviceIds = new[] { "AppleTV_Salon" },
            TriggerOnMovies = true,
            TriggerOnEpisodes = false,
        };

        var config = new PluginConfiguration
        {
            Profiles = new[] { profile },
        };

        var orchestrator = CreateOrchestrator(config);

        var matchingSession = new SessionInfo(null!, null!)
        {
            UserId = _userId,
            DeviceId = "AppleTV_Salon",
        };

        var nonMatchingDeviceSession = new SessionInfo(null!, null!)
        {
            UserId = _userId,
            DeviceId = "Bedroom_FireStick",
        };

        var movie = new Movie { Name = "Inception" };
        var episode = new Episode { Name = "Pilot" };

        // Should match movie on salon Apple TV
        var matched = orchestrator.FindMatchingProfiles(matchingSession, movie);
        Assert.Single(matched);
        Assert.Equal("p1", matched[0].Id);

        // Should NOT match episode (TriggerOnEpisodes is false)
        var matchedEpisode = orchestrator.FindMatchingProfiles(matchingSession, episode);
        Assert.Empty(matchedEpisode);

        // Should NOT match other device
        var matchedOtherDevice = orchestrator.FindMatchingProfiles(nonMatchingDeviceSession, movie);
        Assert.Empty(matchedOtherDevice);
    }

    [Fact]
    public void FindMatchingProfiles_MatchesAll_WhenFilterListsAreEmpty()
    {
        var profile = new HueProfile
        {
            Id = "p_global",
            Name = "Global Profile",
            Enabled = true,
            TargetUserIds = Array.Empty<string>(),
            TargetDeviceIds = Array.Empty<string>(),
            TriggerOnMovies = true,
            TriggerOnEpisodes = true,
        };

        var config = new PluginConfiguration
        {
            Profiles = new[] { profile },
        };

        var orchestrator = CreateOrchestrator(config);

        var randomSession = new SessionInfo(null!, null!)
        {
            UserId = Guid.NewGuid(),
            DeviceId = "AnyDevice",
        };

        var movie = new Movie { Name = "Interstellar" };
        var matched = orchestrator.FindMatchingProfiles(randomSession, movie);

        Assert.Single(matched);
        Assert.Equal("p_global", matched[0].Id);
    }

    [Fact]
    public void FindMatchingProfiles_IgnoresDisabledProfiles()
    {
        var profile = new HueProfile
        {
            Id = "p_disabled",
            Name = "Disabled Profile",
            Enabled = false,
        };

        var config = new PluginConfiguration
        {
            Profiles = new[] { profile },
        };

        var orchestrator = CreateOrchestrator(config);
        var session = new SessionInfo(null!, null!);
        var movie = new Movie { Name = "Avatar" };

        var matched = orchestrator.FindMatchingProfiles(session, movie);
        Assert.Empty(matched);
    }

    [Fact]
    public void MatchesTimeFilter_ReturnsTrue_WhenDisabledOrInvalid()
    {
        var profile = new HueProfile { EnableTimeFilter = false };
        Assert.True(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(12, 0)));

        var invalidProfile = new HueProfile
        {
            EnableTimeFilter = true,
            TimeFilterStart = "invalid",
            TimeFilterEnd = "25:00",
        };
        Assert.True(HueOrchestrator.MatchesTimeFilter(invalidProfile, new TimeOnly(12, 0)));
    }

    [Fact]
    public void MatchesTimeFilter_DaytimeRange_MatchesOnlyWithinWindow()
    {
        var profile = new HueProfile
        {
            EnableTimeFilter = true,
            TimeFilterStart = "14:00",
            TimeFilterEnd = "18:00",
        };

        // Within window
        Assert.True(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(14, 0)));
        Assert.True(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(15, 30)));
        Assert.True(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(18, 0)));

        // Outside window
        Assert.False(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(13, 59)));
        Assert.False(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(18, 1)));
        Assert.False(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(22, 0)));
    }

    [Fact]
    public void MatchesTimeFilter_OvernightRange_MatchesAcrossMidnight()
    {
        var profile = new HueProfile
        {
            EnableTimeFilter = true,
            TimeFilterStart = "20:00",
            TimeFilterEnd = "06:00",
        };

        // Evening before midnight
        Assert.True(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(20, 0)));
        Assert.True(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(22, 30)));
        Assert.True(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(23, 59)));

        // Early morning after midnight
        Assert.True(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(0, 0)));
        Assert.True(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(3, 15)));
        Assert.True(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(6, 0)));

        // Daytime outside window
        Assert.False(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(6, 1)));
        Assert.False(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(12, 0)));
        Assert.False(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(19, 59)));
    }

    [Fact]
    public void MatchesTimeFilter_EqualStartAndEnd_ReturnsTrue()
    {
        var profile = new HueProfile
        {
            EnableTimeFilter = true,
            TimeFilterStart = "20:00",
            TimeFilterEnd = "20:00",
        };

        Assert.True(HueOrchestrator.MatchesTimeFilter(profile, new TimeOnly(15, 0)));
    }
}
