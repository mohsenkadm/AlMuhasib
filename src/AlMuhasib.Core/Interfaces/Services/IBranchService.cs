using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IBranchService
{
    Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Branch>> GetActiveAsync(CancellationToken ct = default);
    Task<Branch?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Branch> GetMainBranchAsync(CancellationToken ct = default);
    Task<Branch> CreateAsync(string name, string code, CancellationToken ct = default);
    Task UpdateAsync(int id, string name, string code, bool isActive, CancellationToken ct = default);
    Task DeactivateAsync(int id, CancellationToken ct = default);

    /// <summary>فروع الدخول: الربط اليدوي، مع إصلاح خلفي فقط إن وُجد فرع نشط واحد بلا ربط.</summary>
    Task<IReadOnlyList<Branch>> GetBranchesForUserAsync(int userId, CancellationToken ct = default);

    /// <summary>الفروع المربوطة يدوياً في UserBranches فقط — لا تتأثر بصلاحية «كل الفروع».</summary>
    Task<IReadOnlyList<Branch>> GetAssignedBranchesForUserAsync(int userId, CancellationToken ct = default);

    Task<int?> GetDefaultBranchIdForUserAsync(int userId, CancellationToken ct = default);
    Task AssignUserBranchesAsync(int userId, IReadOnlyList<int> branchIds, int? defaultBranchId, CancellationToken ct = default);
    Task EnsureUserLinkedToMainAsync(int userId, CancellationToken ct = default);
}
