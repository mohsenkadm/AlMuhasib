namespace AlMuhasib.Core.Models.Updates;

/// <summary>
/// Describes a published application update (hosted as JSON on your server).
/// </summary>
public sealed class AppUpdateManifest
{
    public string Version { get; set; } = "1.0.0";
    public string? ReleaseDate { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? ReleaseNotes { get; set; }
    public bool IsMandatory { get; set; }
    public string? MinSupportedVersion { get; set; }
}
