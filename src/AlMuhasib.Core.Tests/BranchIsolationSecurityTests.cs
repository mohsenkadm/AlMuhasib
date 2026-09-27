using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.Infrastructure.Repositories;
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

    [Fact]
    public async Task AllBranches_mode_only_returns_AllowedBranchIds_not_all_company_branches()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1, 2], canViewAll: true, canManageAll: false);
        ctx.SetAllBranchesMode();

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Categories.AddRange(Cat("A", 1), Cat("B", 2), Cat("Secret", 3));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var visible = await db.Categories.OrderBy(c => c.BranchId).Select(c => c.Name).ToListAsync();
        Assert.Equal(new[] { "A", "B" }, visible);
        Assert.DoesNotContain("Secret", visible);
    }

    [Fact]
    public async Task No_branch_bound_fail_closed_returns_empty()
    {
        var ctx = new BranchContext();
        // Allowed set but no current branch / not all-branches
        ctx.SetAllowedBranches([1, 2], false, false);

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Categories.Add(Cat("A", 1));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var visible = await db.Categories.ToListAsync();
        Assert.Empty(visible);
    }

    [Fact]
    public async Task FindAsync_must_not_be_trusted_for_branch_isolation_use_LINQ()
    {
        // Documents EF FindAsync bypass of query filters — callers must use filtered LINQ.
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1], false, false);
        ctx.SetCurrentBranch(1, "Main", "MAIN");

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Categories.AddRange(Cat("Mine", 1), Cat("Other", 2));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var otherId = await db.Categories.IgnoreQueryFilters()
            .Where(c => c.BranchId == 2).Select(c => c.Id).SingleAsync();

        var viaFind = await db.Categories.FindAsync(otherId);
        Assert.NotNull(viaFind); // FindAsync ignores filters — known EF behavior

        var viaLinq = await db.Categories.FirstOrDefaultAsync(c => c.Id == otherId);
        Assert.Null(viaLinq); // Correct isolation path
    }

    [Fact]
    public async Task Repository_GetByIdAsync_respects_branch_filter()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1], false, false);
        ctx.SetCurrentBranch(1, "Main", "MAIN");

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Categories.AddRange(Cat("Mine", 1), Cat("Other", 2));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var otherId = await db.Categories.IgnoreQueryFilters()
            .Where(c => c.BranchId == 2).Select(c => c.Id).SingleAsync();
        var mineId = await db.Categories.Where(c => c.BranchId == 1).Select(c => c.Id).SingleAsync();

        var factory = new TestDbContextFactory(db, ctx);
        var repo = new Repository<Category>(factory, () => db, ctx);

        Assert.NotNull(await repo.GetByIdAsync(mineId));
        Assert.Null(await repo.GetByIdAsync(otherId));
    }

    [Fact]
    public async Task Soft_delete_revive_stays_inside_write_branch()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1, 2], false, false);
        ctx.SetCurrentBranch(1, "A", "A");

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Categories.Add(new Category
        {
            Name = "SharedName",
            BranchId = 2,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow,
            DeletedBy = "x",
            CreatedBy = "test",
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var factory = new TestDbContextFactory(db, ctx);
        var repo = new Repository<Category>(factory, () => db, ctx);
        var found = await repo.FindSoftDeletedFirstAsync(c => c.Name == "SharedName");
        Assert.Null(found); // must not revive branch-2 row while writing on branch 1
    }

    [Fact]
    public async Task Single_branch_company_sees_all_its_legacy_main_rows()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1], false, false);
        ctx.SetCurrentBranch(1, "الفرع الرئيسي", "MAIN");

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Categories.AddRange(Cat("Legacy1", 1), Cat("Legacy2", 1));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var rows = await db.Categories.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync();
        Assert.Equal(new[] { "Legacy1", "Legacy2" }, rows);
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly AppDbContext _db;
        private readonly IBranchContext _branchContext;

        public TestDbContextFactory(AppDbContext db, IBranchContext branchContext)
        {
            _db = db;
            _branchContext = branchContext;
        }

        public AppDbContext CreateDbContext() => _db;
    }
}
