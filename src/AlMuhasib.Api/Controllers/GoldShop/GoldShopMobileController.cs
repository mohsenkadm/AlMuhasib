using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums.Gold;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.GoldShop;

[ApiController]
[Route("api/gold-shop")]
[Authorize(Policy = "Tenant")]
public sealed class GoldShopMobileController : GoldShopApiControllerBase
{
    public GoldShopMobileController(ITenantContext tenantContext, CloudDbContext db) : base(db, tenantContext) { }

    // Also exposed under /mobile/* for Flutter clients.
    [HttpGet("dashboard")]
    [HttpGet("mobile/dashboard")]
    public async Task<ActionResult<GoldShopDashboardDto>> GetDashboard(CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var today = DateTime.Today;
        var invoices = await Db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.TenantId == TenantId && i.Status != GoldInvoiceStatus.Cancelled)
            .ToListAsync(ct);

        var todaySales = invoices.Where(i => i.InvoiceType == GoldInvoiceType.Sale && i.InvoiceDate.Date == today).ToList();
        var todayPurchases = invoices.Where(i => i.InvoiceType == GoldInvoiceType.Purchase && i.InvoiceDate.Date == today).ToList();
        var credit = invoices.Where(i => i.RemainingAmount > 0 && i.InvoiceType == GoldInvoiceType.Sale).ToList();

        var cashBoxes = await Db.GoldCashBoxes.AsNoTracking()
            .Where(c => c.TenantId == TenantId && c.IsActive).ToListAsync(ct);
        var stocks = await Db.GoldStockBalances.AsNoTracking()
            .Where(s => s.TenantId == TenantId).ToListAsync(ct);
        var settings = await Db.GoldSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == TenantId, ct);
        var lowThreshold = settings?.LowStockAlertGrams ?? 10m;

        var latestFx = await Db.GoldFxRates.AsNoTracking()
            .Where(r => r.TenantId == TenantId)
            .OrderByDescending(r => r.RateDate)
            .ThenByDescending(r => r.Id)
            .FirstOrDefaultAsync(ct);

        var latestPrices = await GetLatestPricesAsync(ct);
        var pricesUpdatedToday = latestPrices.Any(p => p.PriceDate.Date == today);

        var notifications = await Db.GoldNotifications.AsNoTracking()
            .Where(n => n.TenantId == TenantId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        return Ok(new GoldShopDashboardDto
        {
            TodaySalesIqd = todaySales.Sum(i => i.TotalAmountIqd),
            TodaySalesUsd = todaySales.Sum(i => i.TotalAmountUsd),
            TodayPurchasesIqd = todayPurchases.Sum(i => i.TotalAmountIqd),
            TodayPurchasesUsd = todayPurchases.Sum(i => i.TotalAmountUsd),
            CashBalanceIqd = cashBoxes.Where(c => c.Currency == GoldCurrency.IQD).Sum(c => c.Balance),
            CashBalanceUsd = cashBoxes.Where(c => c.Currency == GoldCurrency.USD).Sum(c => c.Balance),
            TotalStockGrams = stocks.Sum(s => s.GramsOnHand),
            TotalStockValueIqd = stocks.Sum(s => s.GramsOnHand * s.AverageCostPerGram),
            OpenCreditCount = credit.Count,
            OpenCreditIqd = credit.Sum(i => i.RemainingAmount > 0 && i.PaymentCurrency == GoldCurrency.IQD
                ? i.RemainingAmount
                : (i.FxRate > 0 ? i.RemainingAmount * i.FxRate : 0)),
            OpenCreditUsd = credit.Sum(i => i.PaymentCurrency == GoldCurrency.USD
                ? i.RemainingAmount
                : (i.FxRate > 0 ? i.RemainingAmount / i.FxRate : 0)),
            OverdueCreditCount = credit.Count(i =>
                (today - i.InvoiceDate.Date).TotalDays >= (settings?.OverdueDaysThreshold ?? 30)),
            LowStockKaratCount = stocks.Count(s => s.GramsOnHand <= lowThreshold),
            PricesUpdatedToday = pricesUpdatedToday,
            LatestUsdToIqd = latestFx?.UsdToIqd,
            StockByKarat = stocks
                .OrderByDescending(s => s.KaratValue)
                .Select(s => new GoldStockRowDto
                {
                    KaratValue = s.KaratValue,
                    KaratName = $"{s.KaratValue}K",
                    GramsOnHand = s.GramsOnHand,
                    AverageCostPerGram = s.AverageCostPerGram,
                    StockValue = s.GramsOnHand * s.AverageCostPerGram,
                    IsLowStock = s.GramsOnHand <= lowThreshold
                })
                .ToList(),
            LatestPrices = latestPrices,
            RecentInvoices = invoices
                .OrderByDescending(i => i.InvoiceDate)
                .ThenByDescending(i => i.Id)
                .Take(10)
                .Select(GoldShopInvoiceMapper.ToListItem)
                .ToList(),
            Alerts = notifications.Select(n => new GoldAlertDto
            {
                Id = n.Id,
                SyncId = n.SyncId,
                Type = n.Type.ToString(),
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                RelatedEntity = n.RelatedEntity
            }).ToList()
        });
    }

    [HttpGet("prices")]
    [HttpGet("prices/latest")]
    [HttpGet("mobile/prices")]
    public async Task<ActionResult<List<GoldPriceDto>>> GetLatestPrices(CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        return Ok(await GetLatestPricesAsync(ct));
    }

    [HttpGet("invoices")]
    [HttpGet("mobile/invoices")]
    public async Task<ActionResult<List<GoldInvoiceListDto>>> GetInvoices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var items = await Db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.TenantId == TenantId)
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(items.Select(GoldShopInvoiceMapper.ToListItem).ToList());
    }

    [HttpGet("invoices/{id:int}")]
    [HttpGet("mobile/invoices/{id:int}")]
    public async Task<ActionResult<GoldInvoiceDetailDto>> GetInvoiceById(int id, CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var invoice = await Db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.TenantId == TenantId && i.Id == id, ct);

        if (invoice is null)
            return NotFound();

        return Ok(GoldShopInvoiceMapper.ToDetail(invoice));
    }

    [HttpGet("customers")]
    [HttpGet("mobile/customers")]
    public async Task<ActionResult<List<GoldCustomerDto>>> GetCustomers(
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var query = Db.GoldCustomers.AsNoTracking().Where(c => c.TenantId == TenantId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Name.Contains(term) || c.Phone.Contains(term));
        }

        var items = await query.OrderBy(c => c.Name).Take(200).ToListAsync(ct);
        return Ok(items.Select(c => new GoldCustomerDto
        {
            Id = c.Id,
            SyncId = c.SyncId,
            Name = c.Name,
            Phone = c.Phone,
            Address = c.Address,
            CreditBalanceIqd = c.CreditBalanceIqd,
            CreditBalanceUsd = c.CreditBalanceUsd,
            IsActive = c.IsActive
        }).ToList());
    }

    [HttpGet("notifications")]
    [HttpGet("mobile/notifications")]
    public async Task<ActionResult<List<GoldAlertDto>>> GetNotifications(
        [FromQuery] bool unreadOnly = false,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var query = Db.GoldNotifications.AsNoTracking().Where(n => n.TenantId == TenantId);
        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        var items = await query.OrderByDescending(n => n.CreatedAt).Take(100).ToListAsync(ct);
        return Ok(items.Select(n => new GoldAlertDto
        {
            Id = n.Id,
            SyncId = n.SyncId,
            Type = n.Type.ToString(),
            Title = n.Title,
            Message = n.Message,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            RelatedEntity = n.RelatedEntity
        }).ToList());
    }

    private async Task<List<GoldPriceDto>> GetLatestPricesAsync(CancellationToken ct)
    {
        var prices = await Db.GoldMithqalPrices.AsNoTracking()
            .Where(p => p.TenantId == TenantId)
            .OrderByDescending(p => p.PriceDate)
            .ThenByDescending(p => p.Id)
            .ToListAsync(ct);

        return prices
            .GroupBy(p => p.KaratValue)
            .Select(g => g.First())
            .OrderByDescending(p => p.KaratValue)
            .Select(p => new GoldPriceDto
            {
                Id = p.Id,
                SyncId = p.SyncId,
                PriceDate = p.PriceDate,
                KaratValue = p.KaratValue,
                KaratName = $"{p.KaratValue}K",
                PricePerMithqal = p.PricePerMithqal,
                Currency = p.Currency.ToString(),
                FxRateUsed = p.FxRateUsed,
                PricePerGram = p.PricePerMithqal / 5m
            })
            .ToList();
    }
}

public sealed class GoldShopDashboardDto
{
    public decimal TodaySalesIqd { get; set; }
    public decimal TodaySalesUsd { get; set; }
    public decimal TodayPurchasesIqd { get; set; }
    public decimal TodayPurchasesUsd { get; set; }
    public decimal CashBalanceIqd { get; set; }
    public decimal CashBalanceUsd { get; set; }
    public decimal TotalStockGrams { get; set; }
    public decimal TotalStockValueIqd { get; set; }
    public int OpenCreditCount { get; set; }
    public decimal OpenCreditIqd { get; set; }
    public decimal OpenCreditUsd { get; set; }
    public int OverdueCreditCount { get; set; }
    public int LowStockKaratCount { get; set; }
    public bool PricesUpdatedToday { get; set; }
    public decimal? LatestUsdToIqd { get; set; }
    public List<GoldStockRowDto> StockByKarat { get; set; } = [];
    public List<GoldPriceDto> LatestPrices { get; set; } = [];
    public List<GoldInvoiceListDto> RecentInvoices { get; set; } = [];
    public List<GoldAlertDto> Alerts { get; set; } = [];
}

public sealed class GoldStockRowDto
{
    public int KaratValue { get; set; }
    public string KaratName { get; set; } = string.Empty;
    public decimal GramsOnHand { get; set; }
    public decimal AverageCostPerGram { get; set; }
    public decimal StockValue { get; set; }
    public bool IsLowStock { get; set; }
}

public sealed class GoldPriceDto
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public DateTime PriceDate { get; set; }
    public int KaratValue { get; set; }
    public string KaratName { get; set; } = string.Empty;
    public decimal PricePerMithqal { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal? FxRateUsed { get; set; }
    public decimal? PricePerGram { get; set; }
}

public sealed class GoldCustomerDto
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal CreditBalanceIqd { get; set; }
    public decimal CreditBalanceUsd { get; set; }
    public bool IsActive { get; set; }
}

public sealed class GoldAlertDto
{
    public int Id { get; set; }
    public Guid SyncId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RelatedEntity { get; set; }
}
