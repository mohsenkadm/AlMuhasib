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

    public ProductService(IDbContextFactory<AppDbContext> contextFactory, ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<Product> CreateAsync(Product product)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        product.CreatedBy = _currentUserService.Username;
        product.CreatedAt = DateTime.UtcNow;
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();
        return product;
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
        var query = context.Products
            .Include(p => p.Category)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(p =>
                p.Name.Contains(term) ||
                (p.Barcode != null && p.Barcode.Contains(term)) ||
                (p.Description != null && p.Description.Contains(term)));
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
        existing.Description = product.Description;
        existing.CategoryId = product.CategoryId;
        existing.Weight = product.Weight;
        existing.WeightUnit = product.WeightUnit;
        existing.DiscountType = product.DiscountType;
        existing.DiscountValue = product.DiscountValue;
        existing.DiscountExpiresAt = product.DiscountExpiresAt;
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
        return await context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Barcode == barcode);
    }

    public async Task<IEnumerable<Product>> SearchByNameAsync(string name)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Products
            .Include(p => p.Category)
            .Where(p => p.Name.Contains(name))
            .Take(20)
            .ToListAsync();
    }
}
