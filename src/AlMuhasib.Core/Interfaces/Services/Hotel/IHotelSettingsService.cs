using AlMuhasib.Core.Entities.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IHotelSettingsService
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);
    Task<HotelSettings?> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<HotelSettings> SaveSettingsAsync(HotelSettings settings, CancellationToken cancellationToken = default);
    Task MarkConfiguredAsync(CancellationToken cancellationToken = default);
}
