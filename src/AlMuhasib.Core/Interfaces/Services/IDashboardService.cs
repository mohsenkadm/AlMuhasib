using AlMuhasib.Core.Models;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardData> GetDashboardDataAsync();
}
