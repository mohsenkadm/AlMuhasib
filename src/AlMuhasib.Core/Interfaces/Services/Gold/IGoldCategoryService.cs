using AlMuhasib.Core.Entities.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldCategoryService
{
    Task<IReadOnlyList<GoldCategory>> GetAllAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    Task<GoldCategory> CreateAsync(GoldCategory category, CancellationToken cancellationToken = default);
    Task<GoldCategory> UpdateAsync(GoldCategory category, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, string deletedBy, CancellationToken cancellationToken = default);
}
