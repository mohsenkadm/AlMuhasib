using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class ProductOfferService : IProductOfferService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public ProductOfferService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<ProductOffer?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.ProductOffers
            .Include(o => o.TriggerProduct)
            .Include(o => o.GiftProduct)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public async Task<(IReadOnlyList<ProductOffer> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        bool? activeOnly = null,
        CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var query = context.ProductOffers
            .Include(o => o.TriggerProduct)
            .Include(o => o.GiftProduct)
            .AsQueryable();

        if (activeOnly == true)
            query = query.Where(o => o.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(o =>
                o.Name.Contains(term)
                || (o.TriggerProduct != null && o.TriggerProduct.Name.Contains(term))
                || (o.GiftProduct != null && o.GiftProduct.Name.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.IsActive)
            .ThenBy(o => o.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<ProductOffer>> GetActiveOffersAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.ProductOffers
            .Include(o => o.TriggerProduct)
            .Include(o => o.GiftProduct)
            .Where(o => o.IsActive)
            .OrderBy(o => o.Name)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ProductOffer>> GetActiveOffersForTriggerProductsAsync(
        IEnumerable<int> triggerProductIds,
        CancellationToken ct = default)
    {
        var ids = triggerProductIds.Distinct().ToList();
        if (ids.Count == 0)
            return [];

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.ProductOffers
            .Include(o => o.TriggerProduct)
            .Include(o => o.GiftProduct)
            .Where(o => o.IsActive && ids.Contains(o.TriggerProductId))
            .ToListAsync(ct);
    }

    public async Task<ProductOffer> CreateAsync(ProductOffer offer, CancellationToken ct = default)
    {
        Validate(offer);
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        await EnsureProductsExistAsync(context, offer.TriggerProductId, offer.GiftProductId, ct);

        offer.Name = offer.Name.Trim();
        offer.CreatedBy = _currentUserService.Username;
        offer.CreatedAt = DateTime.UtcNow;
        context.ProductOffers.Add(offer);
        await context.SaveChangesAsync(ct);

        return (await GetByIdAsync(offer.Id, ct))!;
    }

    public async Task UpdateAsync(ProductOffer offer, CancellationToken ct = default)
    {
        Validate(offer);
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var existing = await context.ProductOffers.FirstOrDefaultAsync(o => o.Id == offer.Id, ct)
            ?? throw new InvalidOperationException("العرض غير موجود.");

        await EnsureProductsExistAsync(context, offer.TriggerProductId, offer.GiftProductId, ct);

        existing.Name = offer.Name.Trim();
        existing.IsActive = offer.IsActive;
        existing.TriggerProductId = offer.TriggerProductId;
        existing.TriggerQuantity = offer.TriggerQuantity;
        existing.GiftProductId = offer.GiftProductId;
        existing.GiftQuantity = offer.GiftQuantity;
        existing.Notes = offer.Notes;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = _currentUserService.Username;

        await context.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var existing = await context.ProductOffers.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new InvalidOperationException("العرض غير موجود.");

        existing.MarkSoftDeleted(_currentUserService.Username);
        await context.SaveChangesAsync(ct);
    }

    private static void Validate(ProductOffer offer)
    {
        if (string.IsNullOrWhiteSpace(offer.Name))
            throw new InvalidOperationException("اسم العرض مطلوب.");
        if (offer.TriggerProductId <= 0)
            throw new InvalidOperationException("يجب اختيار المنتج المشغّل.");
        if (offer.GiftProductId <= 0)
            throw new InvalidOperationException("يجب اختيار منتج الهدية.");
        if (offer.TriggerProductId == offer.GiftProductId)
            throw new InvalidOperationException("منتج الهدية يجب أن يختلف عن المنتج المشغّل.");
        if (offer.TriggerQuantity <= 0)
            throw new InvalidOperationException("كمية التفعيل يجب أن تكون أكبر من صفر.");
        if (offer.GiftQuantity <= 0)
            throw new InvalidOperationException("كمية الهدية يجب أن تكون أكبر من صفر.");
    }

    private static async Task EnsureProductsExistAsync(
        AppDbContext context, int triggerProductId, int giftProductId, CancellationToken ct)
    {
        var ids = new[] { triggerProductId, giftProductId };
        var count = await context.Products.CountAsync(p => ids.Contains(p.Id), ct);
        if (count != 2)
            throw new InvalidOperationException("أحد المنتجات المحددة غير موجود.");
    }
}
