using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlMuhasib.Core.Tests;

/// <summary>
/// Security-focused isolation tests: BranchId must be stamped from context and
/// cross-branch mutations must be denied even when entity Id is known.
/// </summary>
public class BranchIsolationSecurityTests
{
    private static AppDbContext CreateDb(IBranchContext branchContext)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options, currentUserService: null, branchContext: branchContext);
    }

    private static Category Cat(string name, int branchId) => new()
    {
        Name = name,
        BranchId = branchId,
        CreatedBy = "test",
        RowVersion = new byte[] { 1 }
    };

    [Fact]
    public async Task New_entity_gets_BranchId_from_context()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([10], false, false);
        ctx.SetCurrentBranch(10, "Baghdad", "BGW");

        await using var db = CreateDb(ctx);
        db.Categories.Add(new Category { Name = "Cat A", CreatedBy = "test", RowVersion = new byte[] { 1 } });
        await db.SaveChangesAsync();

        var cat = await db.Categories.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(10, cat.BranchId);
    }

    [Fact]
    public async Task Cross_branch_mutation_is_denied()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1, 2], false, false);
        ctx.SetCurrentBranch(1, "Main", "MAIN");

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Categories.Add(Cat("Other", 2));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var entity = await db.Categories.IgnoreQueryFilters().SingleAsync(c => c.BranchId == 2);
        entity.Name = "Hacked";
        db.Entry(entity).State = EntityState.Modified;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Query_filter_hides_other_branch_rows()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1, 2], false, false);
        ctx.SetCurrentBranch(1, "Main", "MAIN");

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Categories.AddRange(Cat("B1", 1), Cat("B2", 2));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var visible = await db.Categories.ToListAsync();
        Assert.Single(visible);
        Assert.Equal("B1", visible[0].Name);
    }

    [Fact]
    public async Task Legacy_main_branch_behavior_keeps_all_rows_visible_for_main_user()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1], true, true);
        ctx.SetCurrentBranch(1, "الفرع الرئيسي", "MAIN");

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Categories.Add(Cat("Legacy", 1));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var rows = await db.Categories.ToListAsync();
        Assert.Single(rows);
        Assert.Equal(1, rows[0].BranchId);
    }
}
