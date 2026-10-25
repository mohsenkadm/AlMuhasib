using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Core.Interfaces.Services.Customs;

public interface ICustomsReportService
{
    Task<IReadOnlyList<CustomsReportCustomerOption>> GetCustomerOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<CustomsDailyReportResult> GetDailyReportAsync(
        DateTime day,
        int? customerId = null,
        CancellationToken cancellationToken = default);

    Task<CustomsMonthlyReportResult> GetMonthlyReportAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    Task<CustomsCustomerReportResult> GetCustomerReportAsync(
        DateTime fromDate,
        DateTime toDate,
        int? customerId = null,
        CancellationToken cancellationToken = default);

    Task<CustomsInvoiceReportResult> GetInvoiceReportAsync(
        DateTime fromDate,
        DateTime toDate,
        int? customerId = null,
        CustomsInvoiceStatus? status = null,
        AccountingCurrency? currency = null,
        CustomsServiceType? serviceType = null,
        string? searchText = null,
        bool? hasRemaining = null,
        CancellationToken cancellationToken = default);

    Task<CustomsVehiclesReportResult> GetVehiclesReportAsync(
        DateTime fromDate,
        DateTime toDate,
        int? customerId = null,
        CustomsVehicleStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<CustomsDebtsReportResult> GetDebtsReportAsync(
        DateTime asOfDate,
        int? customerId = null,
        AccountingCurrency? currency = null,
        CancellationToken cancellationToken = default);

    Task<CustomsProfitReportResult> GetProfitReportAsync(
        DateTime fromDate,
        DateTime toDate,
        int? customerId = null,
        AccountingCurrency? currency = null,
        CancellationToken cancellationToken = default);
}
