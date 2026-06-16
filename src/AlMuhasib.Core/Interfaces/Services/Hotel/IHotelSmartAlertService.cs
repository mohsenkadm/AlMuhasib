using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IHotelSmartAlertService
{
    Task<IReadOnlyList<SmartAlert>> GetAlertsAsync(CancellationToken cancellationToken = default);
}
