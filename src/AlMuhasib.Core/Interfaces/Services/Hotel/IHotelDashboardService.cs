using AlMuhasib.Core.Models.Hotel;

namespace AlMuhasib.Core.Interfaces.Services.Hotel;

public interface IHotelDashboardService
{
    Task<HotelDashboardStats> GetDashboardStatsAsync(
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default);
}
