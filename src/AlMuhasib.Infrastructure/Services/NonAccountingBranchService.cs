using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>
/// بديل IBranchService للأنظمة التي لا تدعم تعدد الفروع (عقود سيارات، فندق، …).
/// يوفّر فرعاً افتراضياً واحداً حتى يعمل مسار تسجيل الدخول دون اختيار فرع.
/// </summary>
public sealed class NonAccountingBranchService : IBranchService
{
    private static readonly Branch DefaultBranch = new()
    {
        Id = 1,
        Name = "الرئيسي",
        Code = Branch.MainBranchCode,
        IsActive = true,
        IsMain = true
    };

    private static readonly IReadOnlyList<Branch> Single = [DefaultBranch];

    public Task<IReadOnlyList<Branch>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult(Single);

    public Task<IReadOnlyList<Branch>> GetActiveAsync(CancellationToken ct = default) =>
        Task.FromResult(Single);

    public Task<Branch?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Task.FromResult(id == DefaultBranch.Id ? DefaultBranch : null);

    public Task<Branch> GetMainBranchAsync(CancellationToken ct = default) =>
        Task.FromResult(DefaultBranch);

    public Task<Branch> CreateAsync(string name, string code, CancellationToken ct = default) =>
        throw new NotSupportedException("تعدد الفروع غير متاح في هذا النظام.");

    public Task UpdateAsync(int id, string name, string code, bool isActive, CancellationToken ct = default) =>
        throw new NotSupportedException("تعدد الفروع غير متاح في هذا النظام.");

    public Task DeactivateAsync(int id, CancellationToken ct = default) =>
        throw new NotSupportedException("تعدد الفروع غير متاح في هذا النظام.");

    public Task<IReadOnlyList<Branch>> GetBranchesForUserAsync(int userId, CancellationToken ct = default) =>
        Task.FromResult(Single);

    public Task<IReadOnlyList<Branch>> GetAssignedBranchesForUserAsync(int userId, CancellationToken ct = default) =>
        Task.FromResult(Single);

    public Task<int?> GetDefaultBranchIdForUserAsync(int userId, CancellationToken ct = default) =>
        Task.FromResult<int?>(DefaultBranch.Id);

    public Task AssignUserBranchesAsync(
        int userId, IReadOnlyList<int> branchIds, int? defaultBranchId, CancellationToken ct = default) =>
        throw new NotSupportedException("تعدد الفروع غير متاح في هذا النظام.");

    public Task EnsureUserLinkedToMainAsync(int userId, CancellationToken ct = default) =>
        Task.CompletedTask;
}
