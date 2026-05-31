namespace AlMuhasib.Core.Models.Updates;

public sealed class AppUpdateCheckResult
{
    public bool IsUpdateAvailable { get; init; }
    public AppUpdateManifest? Manifest { get; init; }
    public Version? CurrentVersion { get; init; }
    public Version? AvailableVersion { get; init; }
    public string? ErrorMessage { get; init; }
    public bool SkippedBecauseOffline { get; init; }
    public bool SkippedBecauseDisabled { get; init; }
    public bool SkippedBecauseRecentCheck { get; init; }

    public static AppUpdateCheckResult NoUpdate(Version current) => new()
    {
        CurrentVersion = current,
        IsUpdateAvailable = false
    };

    public static AppUpdateCheckResult Available(Version current, Version available, AppUpdateManifest manifest) => new()
    {
        CurrentVersion = current,
        AvailableVersion = available,
        Manifest = manifest,
        IsUpdateAvailable = true
    };
}
