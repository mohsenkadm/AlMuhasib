namespace AlMuhasib.Core.Models.License;

/// <summary>
/// Persisted local desktop license record (%LocalAppData%\AlMuhasib\desktop-license.json).
/// </summary>
public sealed class DesktopLicenseState
{
    public Guid InstallationId { get; set; }
    public DesktopLicenseMode Mode { get; set; }
    public DateTime? TrialStartedAt { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public string? ActivationPayload { get; set; }
    public DateTime LastSeenUtc { get; set; }
    /// <summary>SHA-256 integrity of critical fields to deter naive tampering.</summary>
    public string IntegrityHash { get; set; } = string.Empty;
}
