using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IPricingTypeService
{
    Task<PricingType> CreateAsync(PricingType pricingType);
    Task<PricingType?> GetByIdAsync(int id);
    Task<(IEnumerable<PricingType> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? searchTerm = null, bool? activeOnly = null);
    Task UpdateAsync(PricingType pricingType);
    Task DeleteAsync(int id);
    Task<IReadOnlyList<PricingType>> GetActiveAsync();
    Task EnsureDefaultExistsAsync();
}
