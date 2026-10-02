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
/// Catalog entities (Category/Product/…) are shared company-wide — isolation uses Warehouse.
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

    private static Warehouse Wh(string name, int branchId) => new()
    {
        Name = name,
        BranchId = branchId,
        CreatedBy = "test",
        RowVersion = new byte[] { 1 }
    };

    private static Category Cat(string name) => new()
    {
        Name = name,
        CreatedBy = "test",
        RowVersion = new byte[] { 1 }
    };

    [Fact]
    public async Task New_warehouse_gets_BranchId_from_context()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([10], false, false);
        ctx.SetCurrentBranch(10, "Baghdad", "BGW");

        await using var db = CreateDb(ctx);
        db.Warehouses.Add(new Warehouse { Name = "مخزن أ", CreatedBy = "test", RowVersion = new byte[] { 1 } });
        await db.SaveChangesAsync();

        var wh = await db.Warehouses.IgnoreQueryFilters().SingleAsync();
        Assert.Equal(10, wh.BranchId);
    }

    [Fact]
    public async Task Shared_catalog_entities_have_no_BranchId_filter()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1], false, false);
        ctx.SetCurrentBranch(1, "Main", "MAIN");

        await using var db = CreateDb(ctx);
        db.Categories.Add(Cat("عام"));
        db.Products.Add(new Product
        {
            Name = "منتج",
            Category = db.Categories.Local.First(),
            CreatedBy = "test",
            RowVersion = new byte[] { 1 }
        });
        await db.SaveChangesAsync();

        // Categories/Products are not IBranchEntity — visible without branch match
        Assert.Single(await db.Categories.ToListAsync());
        Assert.Single(await db.Products.ToListAsync());
        Assert.False(typeof(IBranchEntity).IsAssignableFrom(typeof(Category)));
        Assert.False(typeof(IBranchEntity).IsAssignableFrom(typeof(Product)));
        Assert.False(typeof(IBranchEntity).IsAssignableFrom(typeof(PricingType)));
        Assert.False(typeof(IBranchEntity).IsAssignableFrom(typeof(ProductPrice)));
        Assert.True(typeof(IBranchEntity).IsAssignableFrom(typeof(Warehouse)));
        Assert.True(typeof(IBranchEntity).IsAssignableFrom(typeof(WarehouseStock)));
    }

    [Fact]
    public async Task Cross_branch_warehouse_mutation_is_denied()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1, 2], false, false);
        ctx.SetCurrentBranch(1, "Main", "MAIN");

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Warehouses.Add(Wh("Other", 2));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var entity = await db.Warehouses.IgnoreQueryFilters().SingleAsync(c => c.BranchId == 2);
        entity.Name = "Hacked";
        db.Entry(entity).State = EntityState.Modified;

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Query_filter_hides_other_branch_warehouses()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1, 2], false, false);
        ctx.SetCurrentBranch(1, "Main", "MAIN");

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Warehouses.AddRange(Wh("B1", 1), Wh("B2", 2));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var visible = await db.Warehouses.ToListAsync();
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
        db.Warehouses.Add(Wh("Legacy", 1));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var rows = await db.Warehouses.ToListAsync();
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
        db.Warehouses.AddRange(Wh("A", 1), Wh("B", 2), Wh("Secret", 3));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var visible = await db.Warehouses.OrderBy(c => c.BranchId).Select(c => c.Name).ToListAsync();
        Assert.Equal(new[] { "A", "B" }, visible);
        Assert.DoesNotContain("Secret", visible);
    }

    [Fact]
    public async Task No_branch_bound_fail_closed_returns_empty()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1, 2], false, false);

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Warehouses.Add(Wh("A", 1));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var visible = await db.Warehouses.ToListAsync();
        Assert.Empty(visible);
    }

    [Fact]
    public async Task FindAsync_must_not_be_trusted_for_branch_isolation_use_LINQ()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1], false, false);
        ctx.SetCurrentBranch(1, "Main", "MAIN");

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Warehouses.AddRange(Wh("Mine", 1), Wh("Other", 2));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var otherId = await db.Warehouses.IgnoreQueryFilters()
            .Where(c => c.BranchId == 2).Select(c => c.Id).SingleAsync();

        var viaFind = await db.Warehouses.FindAsync(otherId);
        Assert.NotNull(viaFind);

        var viaLinq = await db.Warehouses.FirstOrDefaultAsync(c => c.Id == otherId);
        Assert.Null(viaLinq);
    }

    [Fact]
    public async Task Repository_GetByIdAsync_respects_branch_filter_for_warehouses()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1], false, false);
        ctx.SetCurrentBranch(1, "Main", "MAIN");

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Warehouses.AddRange(Wh("Mine", 1), Wh("Other", 2));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var otherId = await db.Warehouses.IgnoreQueryFilters()
            .Where(c => c.BranchId == 2).Select(c => c.Id).SingleAsync();
        var mineId = await db.Warehouses.Where(c => c.BranchId == 1).Select(c => c.Id).SingleAsync();

        var factory = new TestDbContextFactory(db, ctx);
        var repo = new Repository<Warehouse>(factory, () => db, ctx);

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
        db.Warehouses.Add(new Warehouse
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
        var repo = new Repository<Warehouse>(factory, () => db, ctx);
        var found = await repo.FindSoftDeletedFirstAsync(c => c.Name == "SharedName");
        Assert.Null(found);
    }

    [Fact]
    public async Task Single_branch_company_sees_all_its_legacy_main_rows()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1], false, false);
        ctx.SetCurrentBranch(1, "الفرع الرئيسي", "MAIN");

        await using var db = CreateDb(ctx);
        db.BypassBranchFilter = true;
        db.Warehouses.AddRange(Wh("Legacy1", 1), Wh("Legacy2", 1));
        await db.SaveChangesAsync();
        db.BypassBranchFilter = false;

        var rows = await db.Warehouses.OrderBy(c => c.Name).Select(c => c.Name).ToListAsync();
        Assert.Equal(new[] { "Legacy1", "Legacy2" }, rows);
    }

    [Fact]
    public async Task Cross_branch_stock_transfer_updates_destination_BranchId()
    {
        var ctx = new BranchContext();
        ctx.SetAllowedBranches([1, 2], true, true);
        ctx.SetCurrentBranch(1, "Main", "MAIN");

        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using (var seed = new AppDbContext(options, null, ctx))
        {
            seed.BypassBranchFilter = true;
            var cat = Cat("عام");
            seed.Categories.Add(cat);
            await seed.SaveChangesAsync();
            var product = new Product
            {
                Name = "P1",
                CategoryId = cat.Id,
                CreatedBy = "test",
                RowVersion = new byte[] { 1 }
            };
            seed.Products.Add(product);
            seed.Warehouses.AddRange(Wh("From", 1), Wh("To", 2));
            await seed.SaveChangesAsync();
            seed.WarehouseStocks.Add(new WarehouseStock
            {
                WarehouseId = seed.Warehouses.Local.First(w => w.Name == "From").Id,
                ProductId = product.Id,
                Quantity = 10,
                BranchId = 1,
                CreatedBy = "test",
                RowVersion = new byte[] { 1 }
            });
            await seed.SaveChangesAsync();
        }

        var factory = new SharedDbContextFactory(options, ctx);
        var service = new WarehouseTransferService(factory);
        await using var db = factory.CreateDbContext();
        db.BypassBranchFilter = true;
        var fromId = await db.Warehouses.Where(w => w.Name == "From").Select(w => w.Id).SingleAsync();
        var toId = await db.Warehouses.Where(w => w.Name == "To").Select(w => w.Id).SingleAsync();
        var productId = await db.Products.Select(p => p.Id).SingleAsync();

        await service.CreateTransferAsync(
            new WarehouseTransfer { FromWarehouseId = fromId, ToWarehouseId = toId, Date = DateTime.Now },
            [new WarehouseTransferItem { ProductId = productId, Quantity = 4 }]);

        await using var verify = factory.CreateDbContext();
        verify.BypassBranchFilter = true;
        var fromStock = await verify.WarehouseStocks.SingleAsync(s => s.WarehouseId == fromId);
        var toStock = await verify.WarehouseStocks.SingleAsync(s => s.WarehouseId == toId);
        Assert.Equal(6, fromStock.Quantity);
        Assert.Equal(4, toStock.Quantity);
        Assert.Equal(2, toStock.BranchId);
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

    private sealed class SharedDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly IBranchContext _branchContext;

        public SharedDbContextFactory(DbContextOptions<AppDbContext> options, IBranchContext branchContext)
        {
            _options = options;
            _branchContext = branchContext;
        }

        public AppDbContext CreateDbContext() => new(_options, null, _branchContext);
    }
}
