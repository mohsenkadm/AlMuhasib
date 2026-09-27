namespace AlMuhasib.Cloud.Core.Interfaces;

public interface ITenantContext
{
    int? TenantId { get; }
    int? TenantAccountId { get; }
    int? BranchId { get; }
    bool IsAllBranchesMode { get; }
    IReadOnlyList<int> AllowedBranchIds { get; }
    bool CanViewAllBranches { get; }

    void SetTenant(int tenantId, int? accountId = null);
    void SetBranch(int branchId, IEnumerable<int> allowedBranchIds, bool canViewAllBranches = false);
    void SetAllBranchesMode(IEnumerable<int> allowedBranchIds);
    void ClearBranch();

    int RequireWriteBranchId();
    bool IsBranchAllowed(int branchId);
}
