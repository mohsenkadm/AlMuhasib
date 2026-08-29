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

    Task<IReadOnlyList<GoldCashMovementRow>> GetCashBoxMovementReportAsync(
        int? cashBoxId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldUserPerformanceRow>> GetUserPerformanceReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? userName = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldDeletedInvoiceRow>> GetDeletedInvoicesReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<GoldExchangeReportRow> Rows, GoldExchangeReportSummary Summary)> GetExchangeReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? customerId = null,
        int? warehouseId = null,
        GoldPaymentMethod? paymentMethod = null,
        GoldCurrency? paymentCurrency = null,
        GoldInvoiceStatus? status = null,
        decimal? cashDiffFrom = null,
        decimal? cashDiffTo = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<GoldSaleReturnReportRow> Rows, GoldSaleReturnReportSummary Summary)> GetSaleReturnsReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? customerId = null,
        int? warehouseId = null,
        GoldInvoiceStatus? status = null,
        string? relatedInvoiceNumber = null,
        string? userName = null,
        CancellationToken cancellationToken = default);
}
