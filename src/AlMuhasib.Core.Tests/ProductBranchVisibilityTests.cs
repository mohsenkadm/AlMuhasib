using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class ProductBranchVisibilityTests
{
    private sealed class FakeUser : ICurrentUserService
    {
        public string Username => "test";
        public int? UserId => 1;
        public UserRole Role => UserRole.Admin;
        public bool IsAdmin => true;
        public bool CanView(string screenName) => true;
        public bool CanAdd(string screenName) => true;
        public bool CanEdit(string screenName) => true;
        public bool CanDelete(string screenName) => true;
        public bool CanPrint(string screenName) => true;
        public bool CanExport(string screenName) => true;
        public bool CanEditPrice(string screenName) => true;
        public bool IsViewOnly(string screenName) => false;
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

    private static (SharedDbContextFactory Factory, ProductService Service) Create()
    {
        var branch = new BranchContext();
        branch.SetAllowedBranches([1, 2], false, false);
        branch.SetCurrentBranch(1, "Main", "MAIN");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var factory = new SharedDbContextFactory(options, branch);
        var service = new ProductService(factory, new FakeUser(), branch);
        return (factory, service);
    }

    [Fact]
    public async Task Product_visible_only_in_assigned_branches()
    {
        var (factory, service) = Create();
        int mainId;
        int secondaryId;
        int categoryId;

        await using (var db = factory.CreateDbContext())
        {
            db.BypassBranchFilter = true;
            var main = new Branch
            {
                Name = "رئيسي", Code = "MAIN", IsMain = true, IsActive = true,
                CreatedBy = "t", RowVersion = [1]
            };
            var secondary = new Branch
            {
                Name = "فرعي", Code = "B2", IsMain = false, IsActive = true,
                CreatedBy = "t", RowVersion = [1]
            };
            db.Branches.AddRange(main, secondary);
            var cat = new Category { Name = "عام", CreatedBy = "t", RowVersion = [1] };
            db.Categories.Add(cat);
            await db.SaveChangesAsync();
            mainId = main.Id;
            secondaryId = secondary.Id;
            categoryId = cat.Id;
        }

        var product = await service.CreateAsync(new Product
        {
            Name = "منتج فرع واحد",
            CategoryId = categoryId,
            RowVersion = [1]
        });

        await service.SetProductBranchesAsync(product.Id, [secondaryId]);

        var inMain = await service.GetVisibleInBranchAsync(mainId);
        var inSecondary = await service.GetVisibleInBranchAsync(secondaryId);

        Assert.DoesNotContain(inMain, p => p.Id == product.Id);
        Assert.Contains(inSecondary, p => p.Id == product.Id);
    }

    [Fact]
    public async Task Create_links_product_to_main_branch_by_default()
    {
        var (factory, service) = Create();
        int mainId;
        int categoryId;

        await using (var db = factory.CreateDbContext())
        {
            db.BypassBranchFilter = true;
            var main = new Branch
            {
                Name = "رئيسي", Code = "MAIN", IsMain = true, IsActive = true,
                CreatedBy = "t", RowVersion = [1]
            };
            db.Branches.Add(main);
            var cat = new Category { Name = "عام", CreatedBy = "t", RowVersion = [1] };
            db.Categories.Add(cat);
            await db.SaveChangesAsync();
            mainId = main.Id;
            categoryId = cat.Id;
        }

        var product = await service.CreateAsync(new Product
        {
            Name = "منتج افتراضي",
            CategoryId = categoryId,
            RowVersion = [1]
        });

        var branches = await service.GetBranchIdsForProductAsync(product.Id);
        Assert.Equal([mainId], branches);
        Assert.Contains(await service.GetVisibleInBranchAsync(mainId), p => p.Id == product.Id);
    }
}
