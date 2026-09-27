using AlMuhasib.Infrastructure.Services;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class BranchContextTests
{
    [Fact]
    public void Single_branch_write_context_is_required()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1], canViewAll: false, canManageAll: false);
        ctx.SetCurrentBranch(1, "Main", "MAIN");

        Assert.True(ctx.HasWriteBranchContext);
        Assert.Equal(1, ctx.RequireWriteBranchId());
        Assert.False(ctx.IsAllBranchesMode);
    }

    [Fact]
    public void All_branches_mode_blocks_writes()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1, 2], canViewAll: true, canManageAll: true);
        ctx.SetAllBranchesMode();

        Assert.True(ctx.IsAllBranchesMode);
        Assert.False(ctx.HasWriteBranchContext);
        Assert.Throws<InvalidOperationException>(() => ctx.RequireWriteBranchId());
    }

    [Fact]
    public void Unauthorized_branch_switch_is_rejected()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1], canViewAll: false, canManageAll: false);

        Assert.Throws<UnauthorizedAccessException>(() =>
            ctx.SetCurrentBranch(2, "Other", "B2"));
    }

    [Fact]
    public void All_branches_without_permission_is_rejected()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1, 2], canViewAll: false, canManageAll: false);

        Assert.Throws<UnauthorizedAccessException>(() => ctx.SetAllBranchesMode());
    }

    [Fact]
    public void IsBranchAllowed_respects_assignments()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1, 3], canViewAll: false, canManageAll: false);

        Assert.True(ctx.IsBranchAllowed(1));
        Assert.False(ctx.IsBranchAllowed(2));
        Assert.True(ctx.IsBranchAllowed(3));
    }
}
