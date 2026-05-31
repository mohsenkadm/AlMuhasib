using AlMuhasib.Core.Models.Updates;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IAppUpdateService
{
    Version GetCurrentVersion();

    /// <summary>Returns true when the device can reach the update server.</summary>
    Task<bool> IsOnlineAsync(CancellationToken cancellationToken = default);

    Task<AppUpdateCheckResult> CheckForUpdateAsync(bool ignoreInterval = false, CancellationToken cancellationToken = default);

    /// <summary>Downloads the package, launches the updater, and prepares the app to exit.</summary>
    Task ApplyUpdateAsync(AppUpdateManifest manifest, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}
