namespace AlMuhasib.Core.Models.Updates;

public sealed class AppUpdateOptions
{
    public const string SectionName = "Updates";

    /// <summary>Master switch for online updates.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>URL of version.json manifest (e.g. https://yourserver.com/almahasib/version.json).</summary>
    public string ManifestUrl { get; set; } = string.Empty;

    /// <summary>Check for updates when the app starts (requires internet).</summary>
    public bool CheckOnStartup { get; set; } = true;

    /// <summary>Minimum hours between automatic checks (reduces server load).</summary>
    public int CheckIntervalHours { get; set; } = 6;

    /// <summary>Download timeout in minutes.</summary>
    public int DownloadTimeoutMinutes { get; set; } = 30;
}
