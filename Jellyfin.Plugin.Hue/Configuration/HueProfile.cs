using System;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.Hue.Configuration;

/// <summary>
/// Represents a user profile mapping Jellyfin playback states to Philips Hue lighting actions.
/// </summary>
public class HueProfile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HueProfile"/> class.
    /// </summary>
    public HueProfile()
    {
        Id = Guid.NewGuid().ToString("N");
        Name = "Default";
        Enabled = true;

        TargetUserIds = Array.Empty<string>();
        TargetDeviceIds = Array.Empty<string>();
        TriggerOnMovies = true;
        TriggerOnEpisodes = true;
        EnableTimeFilter = false;
        TimeFilterStart = "20:00";
        TimeFilterEnd = "06:00";

        TargetGroupIds = Array.Empty<string>();
        TargetLightIds = Array.Empty<string>();

        PlayBrightness = 0;
        PlayColorHex = "#FFB366";
        PlayTransitionDurationMs = 4000;

        PauseBrightness = 25;
        PauseColorHex = "#FFB366";
        PauseTransitionDurationMs = 1000;

        ResumeSameAsPlay = true;
        ResumeBrightness = 0;
        ResumeTransitionDurationMs = 3000;

        RestoreStateOnStop = true;
        StopBrightness = 80;
        StopColorHex = "#FFFFFF";
        StopTransitionDurationMs = 2000;
    }

    /// <summary>
    /// Gets or sets the unique identifier of the profile.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the friendly name of the profile.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this profile is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the targeted Jellyfin User IDs (empty means all users).
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Needed for XML serialization")]
    public string[] TargetUserIds { get; set; }

    /// <summary>
    /// Gets or sets the targeted Jellyfin Device IDs or Client Names (empty means all devices).
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Needed for XML serialization")]
    public string[] TargetDeviceIds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether movies trigger this profile.
    /// </summary>
    public bool TriggerOnMovies { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether series/episodes trigger this profile.
    /// </summary>
    public bool TriggerOnEpisodes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this profile is restricted to a time range.
    /// </summary>
    public bool EnableTimeFilter { get; set; }

    /// <summary>
    /// Gets or sets the start time of the active window in HH:mm format (e.g. "20:00").
    /// </summary>
    public string TimeFilterStart { get; set; }

    /// <summary>
    /// Gets or sets the end time of the active window in HH:mm format (e.g. "06:00").
    /// </summary>
    public string TimeFilterEnd { get; set; }

    /// <summary>
    /// Gets or sets the target Hue Group IDs (Rooms or Zones).
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Needed for XML serialization")]
    public string[] TargetGroupIds { get; set; }

    /// <summary>
    /// Gets or sets the target Hue individual Light IDs.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Needed for XML serialization")]
    public string[] TargetLightIds { get; set; }

    /// <summary>
    /// Gets or sets the brightness percentage on play (0 = turned off).
    /// </summary>
    public int PlayBrightness { get; set; }

    /// <summary>
    /// Gets or sets the hex color code on play (e.g. #FFB366).
    /// </summary>
    public string PlayColorHex { get; set; }

    /// <summary>
    /// Gets or sets the transition duration on play in milliseconds.
    /// </summary>
    public int PlayTransitionDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the brightness percentage on pause.
    /// </summary>
    public int PauseBrightness { get; set; }

    /// <summary>
    /// Gets or sets the hex color code on pause (e.g. #FFB366).
    /// </summary>
    public string PauseColorHex { get; set; }

    /// <summary>
    /// Gets or sets the transition duration on pause in milliseconds.
    /// </summary>
    public int PauseTransitionDurationMs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether playback start settings should be applied on resume.
    /// </summary>
    public bool ResumeSameAsPlay { get; set; }

    /// <summary>
    /// Gets or sets the brightness percentage on resume.
    /// </summary>
    public int ResumeBrightness { get; set; }

    /// <summary>
    /// Gets or sets the transition duration on resume in milliseconds.
    /// </summary>
    public int ResumeTransitionDurationMs { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether previous light states should be restored on stop.
    /// </summary>
    public bool RestoreStateOnStop { get; set; }

    /// <summary>
    /// Gets or sets the brightness percentage on stop when not restoring state.
    /// </summary>
    public int StopBrightness { get; set; }

    /// <summary>
    /// Gets or sets the hex color code on stop when not restoring state.
    /// </summary>
    public string StopColorHex { get; set; }

    /// <summary>
    /// Gets or sets the transition duration on stop in milliseconds.
    /// </summary>
    public int StopTransitionDurationMs { get; set; }
}
