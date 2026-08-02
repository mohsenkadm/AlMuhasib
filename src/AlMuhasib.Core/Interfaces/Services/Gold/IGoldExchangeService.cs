using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldExchangeService
{
    Task<GoldInvoice> CreateExchangeAsync(
        GoldExchangeRequest request,
        CancellationToken cancellationToken = default);

    Task<string> GetNextInvoiceNumberAsync(CancellationToken cancellationToken = default);

    Task<GoldInvoice?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<GoldInvoiceListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default);
}
