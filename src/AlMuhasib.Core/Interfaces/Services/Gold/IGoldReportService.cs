using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldReportService
{
    Task<GoldReportSummary> GetSummaryAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldInvoiceListItem>> GetSalesReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        GoldInvoiceStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldInvoiceListItem>> GetPurchasesReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        GoldInvoiceStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldStockRow>> GetStockReportAsync(
        int? warehouseId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldCustomerListItem>> GetCreditReportAsync(
        bool overdueOnly = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldAgingRow>> GetAgingReportAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldKaratMovementRow>> GetKaratMovementReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? warehouseId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldProfitabilityRow>> GetProfitabilityReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldAuditReportRow>> GetAuditReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? entityName = null,
        CancellationToken cancellationToken = default);
}
