using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Core.Interfaces.Services.Customs;

public interface ICustomsInvoiceService
{
    Task<(IReadOnlyList<CustomsInvoiceListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        CustomsInvoiceStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? customerId = null,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default);

    Task<CustomsInvoiceDetail?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<string> PreviewNextInvoiceNumberAsync(CancellationToken cancellationToken = default);

    Task<CustomsInvoiceDetail> CreateAsync(
        CustomsInvoiceSaveRequest request,
        CancellationToken cancellationToken = default);

    Task<CustomsInvoiceDetail> UpdateAsync(
        int id,
        CustomsInvoiceSaveRequest request,
        CancellationToken cancellationToken = default);

    Task ApproveAsync(int id, string userName, CancellationToken cancellationToken = default);

    Task CancelAsync(int id, string userName, string? reason = null, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
}
