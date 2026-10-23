using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Core.Interfaces.Services.Customs;

public interface ICustomsReceiptService
{
    Task<(IReadOnlyList<CustomsReceiptListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? customerId = null,
        AccountingCurrency? currency = null,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default);

    Task<CustomsReceiptDetail?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<string> PreviewNextReceiptNumberAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomsOpenInvoiceItem>> GetOpenInvoicesAsync(
        int customerId,
        AccountingCurrency currency,
        int? excludeReceiptId = null,
        CancellationToken cancellationToken = default);

    Task<CustomsReceiptDetail> CreateAsync(
        CustomsReceiptSaveRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomsReceiptDetail> UpdateAsync(
        int id,
        CustomsReceiptSaveRequest request,
        CancellationToken cancellationToken = default);

    Task CancelAsync(int id, string userName, CancellationToken cancellationToken = default);
}
