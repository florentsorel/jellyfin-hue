using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Hue.Services;

/// <summary>
/// Hosted service listening to Jellyfin playback events from <see cref="ISessionManager"/>.
/// </summary>
public class PlaybackListener : IHostedService, IDisposable
{
    private readonly ISessionManager _sessionManager;
    private readonly HueOrchestrator _orchestrator;
    private readonly ILogger<PlaybackListener> _logger;

    // Track pause state per session ID to avoid redundant trigger storms during progress reports
    private readonly ConcurrentDictionary<string, bool> _sessionPauseStates = new(StringComparer.OrdinalIgnoreCase);

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackListener"/> class.
    /// </summary>
    /// <param name="sessionManager">The session manager instance.</param>
    /// <param name="orchestrator">The Hue orchestrator instance.</param>
    /// <param name="logger">The logger instance.</param>
    public PlaybackListener(ISessionManager sessionManager, HueOrchestrator orchestrator, ILogger<PlaybackListener> logger)
    {
        _sessionManager = sessionManager;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Philips Hue playback listener started, subscribing to playback events.");
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackProgress += OnPlaybackProgress;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Philips Hue playback listener stopping, unsubscribing from events.");
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackProgress -= OnPlaybackProgress;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        _sessionPauseStates.Clear();
        return Task.CompletedTask;
    }

    private void OnPlaybackStart(object? sender, PlaybackProgressEventArgs e)
    {
        if (e.Session != null)
        {
            _sessionPauseStates[e.Session.Id] = false;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _orchestrator.HandlePlayAsync(e.Session, e.Item).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling playback start event");
            }
        });
    }

    private void OnPlaybackProgress(object? sender, PlaybackProgressEventArgs e)
    {
        if (e.Session == null)
        {
            return;
        }

        var isPaused = e.IsPaused;
        var hasPrevious = _sessionPauseStates.TryGetValue(e.Session.Id, out var prevPaused);

        // Only trigger when state actually toggled (play <-> pause)
        if (!hasPrevious || prevPaused != isPaused)
        {
            _sessionPauseStates[e.Session.Id] = isPaused;

            _ = Task.Run(async () =>
            {
                try
                {
                    if (isPaused)
                    {
                        await _orchestrator.HandlePauseAsync(e.Session, e.Item).ConfigureAwait(false);
                    }
                    else
                    {
                        await _orchestrator.HandleResumeAsync(e.Session, e.Item).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling playback progress pause/resume event");
                }
            });
        }
    }

    private void OnPlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        if (e.Session != null)
        {
            _sessionPauseStates.TryRemove(e.Session.Id, out _);
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _orchestrator.HandleStopAsync(e.Session, e.Item).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling playback stopped event");
            }
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected dispose pattern.
    /// </summary>
    /// <param name="disposing">Whether managed resources are disposing.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _sessionManager.PlaybackStart -= OnPlaybackStart;
                _sessionManager.PlaybackProgress -= OnPlaybackProgress;
                _sessionManager.PlaybackStopped -= OnPlaybackStopped;
                _sessionPauseStates.Clear();
            }

            _disposed = true;
        }
    }
}
