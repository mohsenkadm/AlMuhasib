using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldCustomerService
{
    Task<(IReadOnlyList<GoldCustomerListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? activeOnly = true,
        bool creditOnly = false,
        CancellationToken cancellationToken = default);

    Task<GoldCustomer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<GoldCustomer> CreateAsync(GoldCustomer customer, CancellationToken cancellationToken = default);
    Task<GoldCustomer> UpdateAsync(GoldCustomer customer, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldInvoiceListItem>> GetCustomerInvoicesAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoldCustomerListItem>> GetOverdueCreditCustomersAsync(
        CancellationToken cancellationToken = default);
}
