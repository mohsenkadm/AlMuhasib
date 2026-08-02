using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.GoldShop;

[ApiController]
[Route("api/gold-shop/master")]
[Authorize(Policy = "Tenant")]
public sealed class GoldShopMasterDataController : GoldShopApiControllerBase
{
    public GoldShopMasterDataController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet("prices")]
    public async Task<ActionResult<List<GoldPriceDto>>> GetPrices(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var query = Db.GoldMithqalPrices.AsNoTracking().Where(p => p.TenantId == TenantId);
        if (from.HasValue)
            query = query.Where(p => p.PriceDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(p => p.PriceDate <= to.Value.Date);

        var items = await query
            .OrderByDescending(p => p.PriceDate)
            .ThenByDescending(p => p.KaratValue)
            .Take(500)
            .ToListAsync(ct);

        return Ok(items.Select(p => new GoldPriceDto
        {
            SyncId = p.SyncId,
            PriceDate = p.PriceDate,
            KaratValue = p.KaratValue,
            PricePerMithqal = p.PricePerMithqal,
            Currency = p.Currency.ToString(),
            FxRateUsed = p.FxRateUsed
        }).ToList());
    }

    [HttpGet("fx")]
    public async Task<ActionResult<List<GoldFxRateDto>>> GetFxRates(
        [FromQuery] int take = 30,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        take = Math.Clamp(take, 1, 200);

        var items = await Db.GoldFxRates.AsNoTracking()
            .Where(r => r.TenantId == TenantId)
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.Id)
            .Take(take)
            .ToListAsync(ct);

        return Ok(items.Select(r => new GoldFxRateDto
        {
            SyncId = r.SyncId,
            RateDate = r.RateDate,
            UsdToIqd = r.UsdToIqd,
            Notes = r.Notes
        }).ToList());
    }

    [HttpGet("stock")]
    public async Task<ActionResult<List<GoldStockRowDto>>> GetStock(CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var settings = await Db.GoldSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == TenantId, ct);
        var lowThreshold = settings?.LowStockAlertGrams ?? 10m;

        var stocks = await Db.GoldStockBalances.AsNoTracking()
            .Where(s => s.TenantId == TenantId)
            .OrderByDescending(s => s.KaratValue)
            .ToListAsync(ct);

        return Ok(stocks.Select(s => new GoldStockRowDto
        {
            KaratValue = s.KaratValue,
            GramsOnHand = s.GramsOnHand,
            AverageCostPerGram = s.AverageCostPerGram,
            StockValue = s.GramsOnHand * s.AverageCostPerGram,
            IsLowStock = s.GramsOnHand <= lowThreshold
        }).ToList());
    }

    [HttpGet("customers")]
    public async Task<ActionResult<List<GoldCustomerDto>>> GetCustomers(
        [FromQuery] string? search = null,
        [FromQuery] bool? activeOnly = true,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var query = Db.GoldCustomers.AsNoTracking().Where(c => c.TenantId == TenantId);
        if (activeOnly == true)
            query = query.Where(c => c.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Name.Contains(term) || c.Phone.Contains(term));
        }

        var items = await query.OrderBy(c => c.Name).Take(500).ToListAsync(ct);
        return Ok(items.Select(c => new GoldCustomerDto
        {
            SyncId = c.SyncId,
            Name = c.Name,
            Phone = c.Phone,
            Address = c.Address,
            CreditBalanceIqd = c.CreditBalanceIqd,
            CreditBalanceUsd = c.CreditBalanceUsd,
            IsActive = c.IsActive
        }).ToList());
    }

    [HttpGet("warehouses")]
    public async Task<ActionResult<List<GoldWarehouseDto>>> GetWarehouses(CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var items = await Db.GoldWarehouses.AsNoTracking()
            .Where(w => w.TenantId == TenantId && w.IsActive)
            .OrderByDescending(w => w.IsDefault)
            .ThenBy(w => w.Name)
            .ToListAsync(ct);

        return Ok(items.Select(w => new GoldWarehouseDto
        {
            Id = w.Id,
            SyncId = w.SyncId,
            Name = w.Name,
            IsDefault = w.IsDefault,
            IsActive = w.IsActive,
            Notes = w.Notes
        }).ToList());
    }

    [HttpGet("suppliers")]
    public async Task<ActionResult<List<GoldSupplierDto>>> GetSuppliers(
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var query = Db.GoldSuppliers.AsNoTracking().Where(s => s.TenantId == TenantId && s.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Name.Contains(term) || s.Phone.Contains(term));
        }

        var items = await query.OrderBy(s => s.Name).Take(500).ToListAsync(ct);
        return Ok(items.Select(s => new GoldSupplierDto
        {
            Id = s.Id,
            SyncId = s.SyncId,
            Name = s.Name,
            Phone = s.Phone,
            Address = s.Address,
            CreditBalanceIqd = s.CreditBalanceIqd,
            CreditBalanceUsd = s.CreditBalanceUsd,
            IsActive = s.IsActive
        }).ToList());
    }
}

public sealed class GoldFxRateDto
{
    public Guid SyncId { get; set; }
    public DateTime RateDate { get; set; }
    public decimal UsdToIqd { get; set; }
    public string Notes { get; set; } = string.Empty;
}
