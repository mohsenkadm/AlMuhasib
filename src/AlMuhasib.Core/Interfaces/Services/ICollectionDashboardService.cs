using AlMuhasib.Core.Models;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ICollectionDashboardService
{
    Task<CollectionDashboardSummary> GetDashboardAsync(string? bucketFilter = null, CancellationToken cancellationToken = default);
}
