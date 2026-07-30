using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IPackagingTypeService
{
    Task<PackagingType> CreateAsync(PackagingType packagingType);
    Task<PackagingType?> GetByIdAsync(int id);
    Task<(IEnumerable<PackagingType> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? searchTerm = null, bool? activeOnly = null);
    Task UpdateAsync(PackagingType packagingType);
    Task DeleteAsync(int id);
    Task<IReadOnlyList<PackagingType>> GetActiveAsync();
    Task EnsureDefaultExistsAsync();
}
