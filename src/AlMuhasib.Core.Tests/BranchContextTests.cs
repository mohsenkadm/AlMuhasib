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
    public void ManageAll_does_not_bypass_assigned_branch_list_for_current_branch()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1], canViewAll: true, canManageAll: true);

        Assert.Throws<UnauthorizedAccessException>(() =>
            ctx.SetCurrentBranch(2, "Other", "B2"));

        ctx.SetCurrentBranch(1, "Main", "MAIN");
        Assert.Equal(1, ctx.CurrentBranchId);
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

    [Fact]
    public void IsBranchAllowed_requires_explicit_allowed_list()
    {
        var ctx = new BranchContext();
        // Startup seed must call SetAllowedBranches before SetCurrentBranch.
        ctx.SetAllowedBranches([1], canViewAll: true, canManageAll: true);
        ctx.SetCurrentBranch(1, "Main", "MAIN");

        Assert.True(ctx.IsBranchAllowed(1));
        Assert.False(ctx.IsBranchAllowed(2));
    }

    [Fact]
    public void ManageAll_does_not_mark_unassigned_branch_as_allowed()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1], canViewAll: true, canManageAll: true);

        Assert.True(ctx.IsBranchAllowed(1));
        Assert.False(ctx.IsBranchAllowed(2));
    }
}
