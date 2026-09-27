namespace AlMuhasib.Core.Interfaces;

/// <summary>
/// سياق الفرع الحالي للجلسة (Desktop) أو الطلب (عند الحقن).
/// مصدر BranchId الموحّد — لا تقرأ BranchId من الشاشات مباشرةً للكتابة.
/// </summary>
public interface IBranchContext
{
    /// <summary>الفرع التشغيلي الحالي. null = لم يُحدَّد بعد.</summary>
    int? CurrentBranchId { get; }

    /// <summary>وضع التقارير: كل الفروع المسموحة (للفراءة فقط).</summary>
    bool IsAllBranchesMode { get; }

    /// <summary>الفروع المسموح للمستخدم الوصول إليها.</summary>
    IReadOnlyList<int> AllowedBranchIds { get; }

    /// <summary>هل يملك صلاحية عرض كل الفروع في التقارير.</summary>
    bool CanViewAllBranches { get; }

    /// <summary>هل يملك إدارة كل الفروع.</summary>
    bool CanManageAllBranches { get; }

    string? CurrentBranchName { get; }
    string? CurrentBranchCode { get; }

    /// <summary>هل يوجد سياق فرع صالح للكتابة (فرع محدد وليس All).</summary>
    bool HasWriteBranchContext { get; }

    void SetAllowedBranches(IEnumerable<int> branchIds, bool canViewAll, bool canManageAll);
    void SetCurrentBranch(int branchId, string name, string code);
    void SetAllBranchesMode();
    void Clear();

    /// <summary>BranchId الواجب استخدامه للكتابة. يرمي إن لم يتوفر.</summary>
    int RequireWriteBranchId();

    /// <summary>هل الفرع ضمن المسموح.</summary>
    bool IsBranchAllowed(int branchId);
}
