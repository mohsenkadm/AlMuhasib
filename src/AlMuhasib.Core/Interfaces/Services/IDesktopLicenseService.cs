using AlMuhasib.Core.Models.License;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IDesktopLicenseService
{
    const int DefaultTrialDays = 30;

    /// <summary>
    /// Load or create local license state. Grandfathering is only allowed for profiles
    /// configured before <see cref="AlMuhasib.Core.Licensing.DesktopLicenseKeys.FeatureIntroducedUtc"/>.
    /// Missing/corrupt license files for newer installs fail closed (expired trial).
    /// </summary>
    DesktopLicenseStatus EnsureInitialized(bool profileIsConfigured, DateTime? profileSelectedAtUtc = null);

    DesktopLicenseStatus StartTrial(int trialDays = DefaultTrialDays);
    DesktopLicenseStatus GetStatus();

    /// <summary>Drop in-memory cache and re-read the license file from disk.</summary>
    DesktopLicenseStatus RefreshFromDisk();

    bool IsUsable { get; }
    bool TryActivate(string activationKey, out string? error);
}
