using AlMuhasib.Core.Models;

namespace AlMuhasib.Cloud.Application.Abstractions;

public interface ICloudDashboardService
{
    Task<DashboardData> GetDashboardAsync(CancellationToken ct = default);
}
