using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldSupplierService
{
    Task<(IReadOnlyList<GoldSupplierListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? activeOnly = true,
        bool creditOnly = false,
        CancellationToken cancellationToken = default);

    Task<GoldSupplier?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GoldSupplier> CreateAsync(GoldSupplier supplier, CancellationToken cancellationToken = default);
    Task<GoldSupplier> UpdateAsync(GoldSupplier supplier, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldInvoiceListItem>> GetSupplierInvoicesAsync(
        int supplierId,
        CancellationToken cancellationToken = default);
}
