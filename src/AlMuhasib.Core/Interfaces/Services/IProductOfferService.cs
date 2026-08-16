using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IProductOfferService
{
    Task<ProductOffer?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<(IReadOnlyList<ProductOffer> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        bool? activeOnly = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<ProductOffer>> GetActiveOffersAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ProductOffer>> GetActiveOffersForTriggerProductsAsync(
        IEnumerable<int> triggerProductIds,
        CancellationToken ct = default);

    Task<ProductOffer> CreateAsync(ProductOffer offer, CancellationToken ct = default);

    Task UpdateAsync(ProductOffer offer, CancellationToken ct = default);

    Task SoftDeleteAsync(int id, CancellationToken ct = default);
}
