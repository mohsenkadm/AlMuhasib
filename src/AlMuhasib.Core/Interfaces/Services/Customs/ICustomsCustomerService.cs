using AlMuhasib.Core.Entities.Customs;
using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Core.Interfaces.Services.Customs;

public interface ICustomsCustomerService
{
    Task<(IReadOnlyList<CustomsCustomerListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? activeOnly = null,
        CancellationToken cancellationToken = default);

    Task<CustomsCustomer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<CustomsCustomer> CreateAsync(CustomsCustomer customer, CancellationToken cancellationToken = default);

    Task<CustomsCustomer> UpdateAsync(CustomsCustomer customer, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);

    Task<CustomsCustomerFile?> GetCustomerFileAsync(int customerId, CancellationToken cancellationToken = default);
}
