using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Hue.Configuration;
using Jellyfin.Plugin.Hue.Hue;
using Jellyfin.Plugin.Hue.Services;
using Jellyfin.Plugin.Hue.Tests.Mocks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.Hue.Tests;

public class PlaybackListenerTests
{
    private readonly Mock<ISessionManager> _sessionManagerMock = new();

    [Fact]
    public async Task PlaybackEvents_TriggerOrchestratorAppropriately()
    {
        var config = new PluginConfiguration
        {
            BridgeIp = "192.168.1.100",
            BridgeUsername = "valid-user",
            Profiles = new[]
            {
                new HueProfile
                {
                    Id = "p1",
                    Name = "Test",
                    Enabled = true,
                    TriggerOnMovies = true,
                    PlayBrightness = 0,
                    PauseBrightness = 20,
                    ResumeBrightness = 0,
                    RestoreStateOnStop = false,
                    TargetLightIds = new[] { "light-1" },
                },
            },
        };
        PluginTestHelper.CreateMockPlugin(config);

        var handler = new MockHttpMessageHandler();
        var hueClient = new HueClient(new System.Net.Http.HttpClient(handler), NullLogger<HueClient>.Instance);
        var orchestrator = new HueOrchestrator(hueClient, NullLogger<HueOrchestrator>.Instance);

        using var listener = new PlaybackListener(_sessionManagerMock.Object, orchestrator, NullLogger<PlaybackListener>.Instance);
        await listener.StartAsync(CancellationToken.None);

        var session = new SessionInfo(null!, null!)
        {
            Id = "session-1",
            UserId = Guid.NewGuid(),
        };
        var movie = new Movie { Name = "Test Movie" };

        // 1. Playback start
        _sessionManagerMock.Raise(m => m.PlaybackStart += null, new PlaybackProgressEventArgs
        {
            Session = session,
            Item = movie,
        });

        await Task.Delay(100);
        Assert.True(handler.Requests.Count >= 1);

        var initialCount = handler.Requests.Count;

        // 2. Playback progress (paused)
        _sessionManagerMock.Raise(m => m.PlaybackProgress += null, new PlaybackProgressEventArgs
        {
            Session = session,
            Item = movie,
            IsPaused = true,
        });

        await Task.Delay(100);
        Assert.True(handler.Requests.Count > initialCount);

        // 3. Playback progress again with same paused state (should not trigger another request storm)
        var countAfterPause = handler.Requests.Count;
        _sessionManagerMock.Raise(m => m.PlaybackProgress += null, new PlaybackProgressEventArgs
        {
            Session = session,
            Item = movie,
            IsPaused = true,
        });

        await Task.Delay(50);
        Assert.Equal(countAfterPause, handler.Requests.Count);

        // 4. Playback stopped
        _sessionManagerMock.Raise(m => m.PlaybackStopped += null, new PlaybackStopEventArgs
        {
            Session = session,
            Item = movie,
        });

        await Task.Delay(100);
        Assert.True(handler.Requests.Count > countAfterPause);

        await listener.StopAsync(CancellationToken.None);
    }
}
