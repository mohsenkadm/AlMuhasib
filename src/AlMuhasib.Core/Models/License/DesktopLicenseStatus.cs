namespace AlMuhasib.Core.Models.License;

public sealed class DesktopLicenseStatus
{
    public required Guid InstallationId { get; init; }
    public required DesktopLicenseMode Mode { get; init; }
    public DateTime? TrialEndsAt { get; init; }
    public int? DaysRemaining { get; init; }
    public bool IsUsable { get; init; }
    public bool IsTrial { get; init; }
    public bool ShowsTrialBanner => IsTrial && IsUsable;
    public string Summary { get; init; } = string.Empty;
}
