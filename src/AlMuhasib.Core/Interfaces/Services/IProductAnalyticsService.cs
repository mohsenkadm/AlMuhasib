using AlMuhasib.Core.Models;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IProductAnalyticsService
{
    Task<ProductAnalyticsReportResult> GetReportAsync(ProductAnalyticsQuery query, CancellationToken cancellationToken = default);

    Task<ProductAnalyticsDetail> GetDetailAsync(
        int productId,
        DateTime? from,
        DateTime? to,
        int? warehouseId,
        CancellationToken cancellationToken = default);
}
