using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBranchContext _branchContext;

    public ProductService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService,
        IBranchContext branchContext)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
        _branchContext = branchContext;
    }

    public async Task<Product> CreateAsync(Product product)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var username = _currentUserService.Username;
        var name = product.Name.Trim();
        var barcode = string.IsNullOrWhiteSpace(product.Barcode) ? null : product.Barcode.Trim();
        // المنتجات مشتركة على الشركة — لا تُقيَّد بـ BranchId
        _ = _branchContext.HasWriteBranchContext;

        Product? softDeleted = null;
        if (barcode is not null)
        {
            softDeleted = await context.Products
                .IgnoreQueryFilters()
                .Where(p => p.IsDeleted && p.Barcode == barcode)
                .OrderByDescending(p => p.DeletedAt)
                .FirstOrDefaultAsync();
        }

        if (softDeleted is null
            && !await context.Products.AnyAsync(p => p.Name == name))
        {
            softDeleted = await context.Products
                .IgnoreQueryFilters()
                .Where(p => p.IsDeleted && p.Name == name)
                .OrderByDescending(p => p.DeletedAt)
                .FirstOrDefaultAsync();
        }

        if (softDeleted is not null)
        {
            softDeleted.RestoreFromSoftDelete(username);
            softDeleted.Name = name;
            softDeleted.Description = product.Description;
            softDeleted.Barcode = barcode;
            softDeleted.ScientificName = product.ScientificName;
            softDeleted.UsageInstructions = product.UsageInstructions;
            softDeleted.VehicleType = product.VehicleType;
            softDeleted.ChassisNumber = product.ChassisNumber;
            softDeleted.CarModel = product.CarModel;
            softDeleted.VehicleColor = product.VehicleColor;
            softDeleted.PassengerCount = product.PassengerCount;
            softDeleted.PlateNumber = product.PlateNumber;
            softDeleted.PlateType = product.PlateType;
            softDeleted.CategoryId = product.CategoryId;
            softDeleted.Weight = product.Weight;
            softDeleted.WeightUnit = product.WeightUnit;
            softDeleted.DiscountType = product.DiscountType;
            softDeleted.DiscountValue = product.DiscountValue;
            softDeleted.DiscountExpiresAt = product.DiscountExpiresAt;
            softDeleted.CustomFieldsJson = product.CustomFieldsJson;
            await context.SaveChangesAsync();
            await EnsureDefaultProductBranchAsync(context, softDeleted.Id);
            return softDeleted;
        }

        product.Name = name;
        product.Barcode = barcode;
        product.CreatedBy = username;
        product.CreatedAt = DateTime.UtcNow;
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // ربط افتراضي بالفرع الرئيسي (أو الحالي) إن لم تُضبط الفروع من الواجهة بعد.
        await EnsureDefaultProductBranchAsync(context, product.Id);
        return product;
    }

    private static async Task EnsureDefaultProductBranchAsync(AppDbContext context, int productId)
    {
        if (await context.ProductBranches.AnyAsync(pb => pb.ProductId == productId))
            return;

        var mainId = await context.Branches.AsNoTracking()
            .Where(b => b.IsMain && b.IsActive)
            .Select(b => (int?)b.Id)
            .FirstOrDefaultAsync();
        if (mainId is null)
        {
            mainId = await context.Branches.AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Id)
                .Select(b => (int?)b.Id)
                .FirstOrDefaultAsync();
        }
        if (mainId is null) return;

        context.ProductBranches.Add(new ProductBranch
        {
            ProductId = productId,
            BranchId = mainId.Value,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? categoryId = null,
        string? searchTerm = null,
        string? sizeName = null,
        string? colorName = null,
        bool? hasBatches = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = ProductBranchVisibility.WhereVisibleInCurrentScope(
                context.Products.Include(p => p.Category),
                context,
                _branchContext)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var plateType = AlMuhasib.Core.Helpers.VehiclePlateTypeHelper.Parse(term);
            query = query.Where(p =>
                p.Name.Contains(term) ||
                (p.Barcode != null && p.Barcode.Contains(term)) ||
                (p.ScientificName != null && p.ScientificName.Contains(term)) ||
                (p.Description != null && p.Description.Contains(term)) ||
                (p.VehicleType != null && p.VehicleType.Contains(term)) ||
                (p.ChassisNumber != null && p.ChassisNumber.Contains(term)) ||
                (p.VehicleColor != null && p.VehicleColor.Contains(term)) ||
                (p.PlateNumber != null && p.PlateNumber.Contains(term)) ||
                (p.PassengerCount != null && p.PassengerCount.ToString()!.Contains(term)) ||
                (plateType != AlMuhasib.Core.Enums.VehiclePlateType.None && p.PlateType == plateType));
        }

        if (!string.IsNullOrWhiteSpace(sizeName))
        {
            var size = sizeName.Trim();
            query = query.Where(p => context.ProductSizes.Any(s =>
                s.ProductId == p.Id && s.SizeName == size));
        }

        if (!string.IsNullOrWhiteSpace(colorName))
        {
            var color = colorName.Trim();
            query = query.Where(p => context.ProductColors.Any(c =>
                c.ProductId == p.Id && c.ColorName == color));
        }

        if (hasBatches == true)
        {
            query = query.Where(p => context.ProductBatches.Any(b => b.ProductId == p.Id));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task UpdateAsync(Product product)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var existing = await context.Products
            .FirstOrDefaultAsync(p => p.Id == product.Id)
            ?? throw new InvalidOperationException("المنتج غير موجود");

        existing.Name = product.Name;
        existing.Barcode = product.Barcode;
        existing.ScientificName = product.ScientificName;
        existing.UsageInstructions = product.UsageInstructions;
        existing.Description = product.Description;
        existing.CategoryId = product.CategoryId;
        existing.Weight = product.Weight;
        existing.WeightUnit = product.WeightUnit;
        existing.DiscountType = product.DiscountType;
        existing.DiscountValue = product.DiscountValue;
        existing.DiscountExpiresAt = product.DiscountExpiresAt;
        existing.CustomFieldsJson = product.CustomFieldsJson;
        existing.VehicleType = product.VehicleType;
        existing.ChassisNumber = product.ChassisNumber;
        existing.CarModel = product.CarModel;
        existing.VehicleColor = product.VehicleColor;
        existing.PassengerCount = product.PassengerCount;
        existing.PlateNumber = product.PlateNumber;
        existing.PlateType = product.PlateType;
        existing.UpdatedBy = _currentUserService.Username;
        existing.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }

    public async Task ApplyDiscountToProductsAsync(
        IEnumerable<int> productIds,
        DiscountType discountType,
        decimal discountValue,
        DateTime? discountExpiresAt)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0) return;

        await using var context = await _contextFactory.CreateDbContextAsync();
        var products = await context.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        var username = _currentUserService.Username;
        var now = DateTime.UtcNow;

        foreach (var product in products)
        {
            product.DiscountType = discountType;
            product.DiscountValue = discountType == DiscountType.None ? 0m : Math.Max(0m, discountValue);
            product.DiscountExpiresAt = discountType == DiscountType.None ? null : discountExpiresAt;
            product.UpdatedBy = username;
            product.UpdatedAt = now;
        }

        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products.FindAsync(id);
        if (product is null) return;

        product.MarkSoftDeleted(_currentUserService.Username);
        await context.SaveChangesAsync();
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await ProductBranchVisibility.WhereVisibleInCurrentScope(
                context.Products.Include(p => p.Category),
                context,
                _branchContext)
            .FirstOrDefaultAsync(p => p.Barcode == barcode);
    }

    public async Task<IEnumerable<Product>> SearchByNameAsync(string name)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var term = name.Trim();
        var plateType = AlMuhasib.Core.Helpers.VehiclePlateTypeHelper.Parse(term);
        return await ProductBranchVisibility.WhereVisibleInCurrentScope(
                context.Products.Include(p => p.Category),
                context,
                _branchContext)
            .Where(p => p.Name.Contains(term)
                        || (p.ScientificName != null && p.ScientificName.Contains(term))
                        || (p.Barcode != null && p.Barcode.Contains(term))
                        || (p.VehicleType != null && p.VehicleType.Contains(term))
                        || (p.ChassisNumber != null && p.ChassisNumber.Contains(term))
                        || (p.VehicleColor != null && p.VehicleColor.Contains(term))
                        || (p.PlateNumber != null && p.PlateNumber.Contains(term))
                        || (p.PassengerCount != null && p.PassengerCount.ToString()!.Contains(term))
                        || (plateType != AlMuhasib.Core.Enums.VehiclePlateType.None && p.PlateType == plateType))
            .Take(20)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<int>> GetBranchIdsForProductAsync(int productId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.ProductBranches.AsNoTracking()
            .Where(pb => pb.ProductId == productId)
            .Select(pb => pb.BranchId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<int, string>> GetBranchNamesByProductIdsAsync(
        IEnumerable<int> productIds,
        CancellationToken ct = default)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, string>();

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var rows = await context.ProductBranches.AsNoTracking()
            .Where(pb => ids.Contains(pb.ProductId))
            .Select(pb => new { pb.ProductId, pb.Branch.Name, pb.Branch.IsMain })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.ProductId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(" · ", g.OrderByDescending(x => x.IsMain).ThenBy(x => x.Name).Select(x => x.Name)));
    }

    public async Task SetProductBranchesAsync(
        int productId,
        IEnumerable<int> branchIds,
        CancellationToken ct = default)
    {
        var ids = branchIds.Distinct().Where(id => id > 0).ToList();
        if (ids.Count == 0)
            throw new InvalidOperationException("يجب اختيار فرع واحد على الأقل للمنتج");

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var productExists = await context.Products.AnyAsync(p => p.Id == productId, ct);
        if (!productExists)
            throw new InvalidOperationException("المنتج غير موجود");

        var validBranchIds = await context.Branches.AsNoTracking()
            .Where(b => ids.Contains(b.Id) && b.IsActive)
            .Select(b => b.Id)
            .ToListAsync(ct);
        if (validBranchIds.Count == 0)
            throw new InvalidOperationException("لا يوجد فرع نشط صالح من الفروع المختارة");

        var existing = await context.ProductBranches.Where(pb => pb.ProductId == productId).ToListAsync(ct);
        context.ProductBranches.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var branchId in validBranchIds)
        {
            context.ProductBranches.Add(new ProductBranch
            {
                ProductId = productId,
                BranchId = branchId,
                CreatedAt = now
            });
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Product>> GetVisibleInBranchAsync(int branchId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await ProductBranchVisibility.WhereVisibleInBranches(
                context.Products.Include(p => p.Category),
                context,
                [branchId])
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Product>> GetVisibleInCurrentBranchAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await ProductBranchVisibility.WhereVisibleInCurrentScope(
                context.Products.Include(p => p.Category),
                context,
                _branchContext)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlySet<int>> GetVisibleProductIdsForCurrentScopeAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await ProductBranchVisibility.GetVisibleProductIdsAsync(context, _branchContext, ct);
    }

    public async Task<bool> IsVisibleInBranchAsync(int productId, int branchId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var hasAny = await context.ProductBranches.AnyAsync(pb => pb.ProductId == productId, ct);
        if (!hasAny)
            return true;
        return await context.ProductBranches.AnyAsync(
            pb => pb.ProductId == productId && pb.BranchId == branchId, ct);
    }

    public async Task UpsertWarehouseStocksAsync(
        int productId,
        IEnumerable<(int WarehouseId, decimal Quantity, decimal MinQuantity)> rows,
        CancellationToken ct = default)
    {
        var list = rows.ToList();
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        context.BypassBranchFilter = true;

        var productExists = await context.Products.AnyAsync(p => p.Id == productId, ct);
        if (!productExists)
            throw new InvalidOperationException("المنتج غير موجود");

        var warehouseIds = list.Select(r => r.WarehouseId).Distinct().ToList();
        var warehouses = await context.Warehouses.AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, ct);

        var existing = await context.WarehouseStocks
            .Where(s => s.ProductId == productId && warehouseIds.Contains(s.WarehouseId))
            .ToDictionaryAsync(s => s.WarehouseId, ct);

        var username = _currentUserService.Username ?? "system";
        var now = DateTime.UtcNow;

        foreach (var row in list)
        {
            if (!warehouses.TryGetValue(row.WarehouseId, out var warehouse))
                continue;

            var qty = row.Quantity < 0 ? 0 : row.Quantity;
            var minQty = row.MinQuantity < 0 ? 0 : row.MinQuantity;

            if (existing.TryGetValue(row.WarehouseId, out var stock))
            {
                // عند التعديل: حدّث الحد الأدنى دائماً؛ وحدّث الكمية فقط إن تغيّرت من الواجهة
                // (الكمية الحالية قد تتأثر بالفواتير — عند الإنشاء نضبط الافتتاحية).
                stock.MinQuantity = minQty;
                if (qty != stock.Quantity)
                {
                    // إن كانت الكمية السابقة صفراً نعتبرها كمية افتتاحية
                    if (stock.Quantity == 0 && qty > 0 && stock.OpeningQuantity == 0)
                        stock.OpeningQuantity = qty;
                    stock.Quantity = qty;
                }
                stock.UpdatedAt = now;
                stock.UpdatedBy = username;
            }
            else if (qty > 0 || minQty > 0)
            {
                await context.WarehouseStocks.AddAsync(new WarehouseStock
                {
                    WarehouseId = row.WarehouseId,
                    ProductId = productId,
                    BranchId = warehouse.BranchId,
                    Quantity = qty,
                    OpeningQuantity = qty,
                    UnitCost = 0,
                    MinQuantity = minQty,
                    CreatedAt = now,
                    CreatedBy = username
                }, ct);
            }
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<(int WarehouseId, string WarehouseName, int BranchId, string BranchName, decimal Quantity, decimal MinQuantity, int? StockId)>>
        GetWarehouseStockRowsForBranchesAsync(int? productId, IEnumerable<int> branchIds, CancellationToken ct = default)
    {
        var ids = branchIds.Distinct().Where(id => id > 0).ToList();
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        context.BypassBranchFilter = true;

        var warehouses = await context.Warehouses.AsNoTracking()
            .Include(w => w.Branch)
            .Where(w => ids.Count == 0 || ids.Contains(w.BranchId))
            .OrderBy(w => w.Branch!.Name)
            .ThenBy(w => w.Name)
            .ToListAsync(ct);

        Dictionary<int, WarehouseStock> stocks = new();
        if (productId is int pid)
        {
            stocks = await context.WarehouseStocks.AsNoTracking()
                .Where(s => s.ProductId == pid)
                .ToDictionaryAsync(s => s.WarehouseId, ct);
        }

        return warehouses.Select(w =>
        {
            stocks.TryGetValue(w.Id, out var stock);
            return (
                w.Id,
                w.Name,
                w.BranchId,
                w.Branch?.Name ?? $"#{w.BranchId}",
                stock?.Quantity ?? 0m,
                stock?.MinQuantity ?? 0m,
                (int?)stock?.Id);
        }).ToList();
    }
}
