using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldDashboardService
{
    Task<GoldDashboardData> GetDashboardAsync(
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default);
}
