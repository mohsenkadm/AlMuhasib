using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ICategoryService
{
    /// <summary>معرّفات الفروع التي يظهر فيها الصنف.</summary>
    Task<IReadOnlyList<int>> GetBranchIdsForCategoryAsync(int categoryId, CancellationToken ct = default);

    /// <summary>أسماء الفروع المرتبطة بعدة أصناف (للعرض في الجدول).</summary>
    Task<IReadOnlyDictionary<int, string>> GetBranchNamesByCategoryIdsAsync(
        IEnumerable<int> categoryIds,
        CancellationToken ct = default);

    /// <summary>استبدال فروع ظهور الصنف بالكامل.</summary>
    Task SetCategoryBranchesAsync(int categoryId, IEnumerable<int> branchIds, CancellationToken ct = default);

    /// <summary>أصناف ظاهرة في فرع معيّن.</summary>
    Task<IReadOnlyList<Category>> GetVisibleInBranchAsync(int branchId, CancellationToken ct = default);

    /// <summary>أصناف ظاهرة في الفرع الحالي للجلسة.</summary>
    Task<IReadOnlyList<Category>> GetVisibleInCurrentBranchAsync(CancellationToken ct = default);

    /// <summary>هل الصنف ظاهر في الفرع؟ أصناف بلا أي ربط تُعامل كظاهرة (توافق قديم).</summary>
    Task<bool> IsVisibleInBranchAsync(int categoryId, int branchId, CancellationToken ct = default);

    /// <summary>ربط افتراضي بالفرع الرئيسي إن لم تُضبط الفروع بعد.</summary>
    Task EnsureDefaultCategoryBranchAsync(int categoryId, CancellationToken ct = default);
}
