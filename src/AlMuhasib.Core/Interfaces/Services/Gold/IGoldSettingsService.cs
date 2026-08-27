using AlMuhasib.Core.Entities.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldSettingsService
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);
    Task MarkConfiguredAsync(CancellationToken cancellationToken = default);
    Task<GoldSettings> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<GoldSettings> SaveSettingsAsync(GoldSettings settings, CancellationToken cancellationToken = default);
    Task EnsureDefaultsAsync(CancellationToken cancellationToken = default);
}
