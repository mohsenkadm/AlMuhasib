using AlMuhasib.Cloud.Core.Interfaces;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed class TenantContext : ITenantContext
{
    private readonly List<int> _allowed = [];

    public int? TenantId { get; private set; }
    public int? TenantAccountId { get; private set; }
    public int? BranchId { get; private set; }
    public bool IsAllBranchesMode { get; private set; }
    public IReadOnlyList<int> AllowedBranchIds => _allowed;
    public bool CanViewAllBranches { get; private set; }

    public void SetTenant(int tenantId, int? accountId = null)
    {
        TenantId = tenantId;
        TenantAccountId = accountId;
    }

    public void SetBranch(int branchId, IEnumerable<int> allowedBranchIds, bool canViewAllBranches = false)
    {
        if (branchId <= 0)
            throw new InvalidOperationException("Invalid branch id.");

        _allowed.Clear();
        _allowed.AddRange(allowedBranchIds.Where(id => id > 0).Distinct());
        CanViewAllBranches = canViewAllBranches;

        if (_allowed.Count > 0 && !_allowed.Contains(branchId) && !canViewAllBranches)
            throw new UnauthorizedAccessException("Branch not allowed for this account.");

        BranchId = branchId;
        IsAllBranchesMode = false;
    }

    public void SetAllBranchesMode(IEnumerable<int> allowedBranchIds)
    {
        _allowed.Clear();
        _allowed.AddRange(allowedBranchIds.Where(id => id > 0).Distinct());
        if (_allowed.Count == 0)
            throw new UnauthorizedAccessException("No branches allowed.");
        CanViewAllBranches = true;
        IsAllBranchesMode = true;
        BranchId = null;
    }

    public void ClearBranch()
    {
        BranchId = null;
        IsAllBranchesMode = false;
        CanViewAllBranches = false;
        _allowed.Clear();
    }

    public int RequireWriteBranchId()
    {
        if (IsAllBranchesMode || BranchId is not > 0)
            throw new InvalidOperationException("A specific branch context is required for write operations.");
        return BranchId.Value;
    }

    public bool IsBranchAllowed(int branchId) =>
        branchId > 0 && (_allowed.Contains(branchId) || CanViewAllBranches);
}
