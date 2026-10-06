using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>
/// فلترة ظهور المنتجات حسب ProductBranches للنطاق الحالي (فرع واحد أو اتحاد الفروع المسموحة).
/// منتجات بلا أي ربط تُعامل كظاهرة (توافق بيانات قديمة).
/// </summary>
public static class ProductBranchVisibility
{
    /// <summary>
    /// فروع النطاق: الفرع الحالي، أو AllowedBranchIds في وضع كل الفروع.
    /// قائمة فارغة = لا سياق فرع صالح (لا تظهر منتجات مربوطة).
    /// </summary>
    public static IReadOnlyList<int> ResolveScopeBranchIds(IBranchContext? branchContext)
    {
        if (branchContext is null)
            return [];

        if (branchContext.IsAllBranchesMode)
            return branchContext.AllowedBranchIds.Where(id => id > 0).Distinct().ToList();

        if (branchContext.CurrentBranchId is int bid && bid > 0)
            return [bid];

        return branchContext.AllowedBranchIds.Where(id => id > 0).Distinct().ToList();
    }

    public static IQueryable<Product> WhereVisibleInBranches(
        IQueryable<Product> products,
        AppDbContext context,
        IReadOnlyList<int> branchIds)
    {
        if (branchIds.Count == 0)
        {
            // بلا سياق فرع: أظهر فقط المنتجات غير المربوطة بأي فرع (توافق قديم).
            return products.Where(p => !context.ProductBranches.Any(pb => pb.ProductId == p.Id));
        }

        if (branchIds.Count == 1)
        {
            var branchId = branchIds[0];
            return products.Where(p =>
                context.ProductBranches.Any(pb => pb.ProductId == p.Id && pb.BranchId == branchId)
                || !context.ProductBranches.Any(pb => pb.ProductId == p.Id));
        }

        var ids = branchIds.ToList();
        return products.Where(p =>
            context.ProductBranches.Any(pb => pb.ProductId == p.Id && ids.Contains(pb.BranchId))
            || !context.ProductBranches.Any(pb => pb.ProductId == p.Id));
    }

    public static IQueryable<Product> WhereVisibleInCurrentScope(
        IQueryable<Product> products,
        AppDbContext context,
        IBranchContext? branchContext) =>
        WhereVisibleInBranches(products, context, ResolveScopeBranchIds(branchContext));

    public static async Task<HashSet<int>> GetVisibleProductIdsAsync(
        AppDbContext context,
        IBranchContext? branchContext,
        CancellationToken ct = default)
    {
        var branchIds = ResolveScopeBranchIds(branchContext);
        var query = WhereVisibleInBranches(context.Products.AsNoTracking(), context, branchIds);
        var ids = await query.Select(p => p.Id).ToListAsync(ct);
        return ids.ToHashSet();
    }

    public static bool IsProductIdVisible(
        int productId,
        IReadOnlySet<int> visibleProductIds) =>
        visibleProductIds.Contains(productId);
}
