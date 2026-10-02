using AlMuhasib.Core.Interfaces;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>تنفيذ جلسة Branch Context — مصدر BranchId الموحّد على سطح المكتب.</summary>
public sealed class BranchContext : IBranchContext
{
    private readonly List<int> _allowed = [];

    public int? CurrentBranchId { get; private set; }
    public bool IsAllBranchesMode { get; private set; }
    public IReadOnlyList<int> AllowedBranchIds => _allowed;
    public bool CanViewAllBranches { get; private set; }
    public bool CanManageAllBranches { get; private set; }
    public string? CurrentBranchName { get; private set; }
    public string? CurrentBranchCode { get; private set; }

    public bool HasWriteBranchContext =>
        CurrentBranchId is > 0 && !IsAllBranchesMode;

    public void SetAllowedBranches(IEnumerable<int> branchIds, bool canViewAll, bool canManageAll)
    {
        _allowed.Clear();
        _allowed.AddRange(branchIds.Where(id => id > 0).Distinct());
        CanViewAllBranches = canViewAll || canManageAll;
        CanManageAllBranches = canManageAll;
    }

    public void SetCurrentBranch(int branchId, string name, string code)
    {
        if (branchId <= 0)
            throw new InvalidOperationException("Invalid branch id.");

        // لا يُسمح بدخول فرع غير موجود في قائمة المسموح — حتى لو ManageAll
        // (ManageAll يفعّل وضع «كل الفروع» للقراءة عبر SetAllBranchesMode فقط).
        if (_allowed.Count > 0 && !_allowed.Contains(branchId))
            throw new UnauthorizedAccessException("لا تملك صلاحية الدخول إلى هذا الفرع.");

        CurrentBranchId = branchId;
        CurrentBranchName = name;
        CurrentBranchCode = code;
        IsAllBranchesMode = false;
    }

    public void SetAllBranchesMode()
    {
        if (!CanViewAllBranches)
            throw new UnauthorizedAccessException("لا تملك صلاحية عرض كل الفروع.");

        IsAllBranchesMode = true;
        CurrentBranchId = null;
        CurrentBranchName = "كل الفروع";
        CurrentBranchCode = "*";
    }

    public void Clear()
    {
        CurrentBranchId = null;
        IsAllBranchesMode = false;
        CurrentBranchName = null;
        CurrentBranchCode = null;
        CanViewAllBranches = false;
        CanManageAllBranches = false;
        _allowed.Clear();
    }

    public int RequireWriteBranchId()
    {
        if (!HasWriteBranchContext)
            throw new InvalidOperationException(
                "يجب اختيار فرع محدد قبل تنفيذ أي عملية إدخال أو تعديل. وضع «كل الفروع» للقراءة فقط.");
        return CurrentBranchId!.Value;
    }

    public bool IsBranchAllowed(int branchId) =>
        branchId > 0 && _allowed.Contains(branchId);
}
