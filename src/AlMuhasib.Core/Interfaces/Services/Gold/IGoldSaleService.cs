using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldSaleService
{
    Task<(IReadOnlyList<GoldInvoiceListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        GoldInvoiceStatus? status = null,
        int? customerId = null,
        CancellationToken cancellationToken = default);

    Task<GoldInvoice?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GoldInvoice> CreateSaleAsync(GoldSaleRequest request, CancellationToken cancellationToken = default);
    Task<GoldInvoice> CreateSaleReturnAsync(GoldSaleReturnRequest request, CancellationToken cancellationToken = default);
    Task<GoldInvoice> RecordPaymentAsync(GoldPaymentRequest request, CancellationToken cancellationToken = default);
    Task CancelAsync(int id, string cancelledBy, string? reason = null, CancellationToken cancellationToken = default);
    Task<string> GetNextInvoiceNumberAsync(CancellationToken cancellationToken = default);
    Task<string> GetNextSaleReturnNumberAsync(CancellationToken cancellationToken = default);
}
