using AlMuhasib.Core.Models.License;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IDesktopLicenseService
{
    const int DefaultTrialDays = 30;

    DesktopLicenseStatus EnsureInitialized(bool profileIsConfigured);
    DesktopLicenseStatus StartTrial(int trialDays = DefaultTrialDays);
    DesktopLicenseStatus GetStatus();
    bool IsUsable { get; }
    bool TryActivate(string activationKey, out string? error);
}
