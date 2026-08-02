using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core.Enums.Gold;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Api.Controllers.GoldShop;

[ApiController]
[Route("api/gold-shop/reports")]
[Authorize(Policy = "Tenant")]
public sealed class GoldShopReportsController : GoldShopApiControllerBase
{
    public GoldShopReportsController(CloudDbContext db, ITenantContext tenantContext) : base(db, tenantContext) { }

    [HttpGet("stock")]
    public async Task<ActionResult<GoldStockReportDto>> GetStockReport(CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var settings = await Db.GoldSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == TenantId, ct);
        var lowThreshold = settings?.LowStockAlertGrams ?? 10m;

        var stocks = await Db.GoldStockBalances.AsNoTracking()
            .Where(s => s.TenantId == TenantId)
            .OrderByDescending(s => s.KaratValue)
            .ToListAsync(ct);

        var rows = stocks.Select(s => new GoldStockRowDto
        {
            KaratValue = s.KaratValue,
            GramsOnHand = s.GramsOnHand,
            AverageCostPerGram = s.AverageCostPerGram,
            StockValue = s.GramsOnHand * s.AverageCostPerGram,
            IsLowStock = s.GramsOnHand <= lowThreshold
        }).ToList();

        return Ok(new GoldStockReportDto
        {
            Rows = rows,
            TotalGrams = rows.Sum(r => r.GramsOnHand),
            TotalValue = rows.Sum(r => r.StockValue),
            LowStockCount = rows.Count(r => r.IsLowStock)
        });
    }

    [HttpGet("sales")]
    public async Task<ActionResult<GoldSalesReportDto>> GetSalesReport(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var query = Db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.TenantId == TenantId && i.Status != GoldInvoiceStatus.Cancelled);

        if (from.HasValue)
            query = query.Where(i => i.InvoiceDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(i => i.InvoiceDate <= to.Value.Date);

        var items = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .ToListAsync(ct);

        var sales = items.Where(i => i.InvoiceType == GoldInvoiceType.Sale).ToList();
        var purchases = items.Where(i => i.InvoiceType == GoldInvoiceType.Purchase).ToList();

        return Ok(new GoldSalesReportDto
        {
            Rows = items.Select(GoldShopInvoiceMapper.ToListItem).ToList(),
            SaleCount = sales.Count,
            PurchaseCount = purchases.Count,
            TotalSalesIqd = sales.Sum(i => i.TotalAmountIqd),
            TotalSalesUsd = sales.Sum(i => i.TotalAmountUsd),
            TotalPurchasesIqd = purchases.Sum(i => i.TotalAmountIqd),
            TotalPurchasesUsd = purchases.Sum(i => i.TotalAmountUsd),
            TotalWeightSoldGrams = sales.Sum(i => i.TotalWeightGrams),
            TotalWeightPurchasedGrams = purchases.Sum(i => i.TotalWeightGrams)
        });
    }

    [HttpGet("credit")]
    public async Task<ActionResult<GoldCreditReportDto>> GetCreditReport(CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var settings = await Db.GoldSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == TenantId, ct);
        var overdueDays = settings?.OverdueDaysThreshold ?? 30;
        var today = DateTime.Today;

        var items = await Db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.TenantId == TenantId
                        && i.InvoiceType == GoldInvoiceType.Sale
                        && i.RemainingAmount > 0
                        && i.Status != GoldInvoiceStatus.Cancelled)
            .OrderBy(i => i.InvoiceDate)
            .ToListAsync(ct);

        var rows = items.Select(i =>
        {
            var list = GoldShopInvoiceMapper.ToListItem(i);
            return new GoldCreditRowDto
            {
                Invoice = list,
                DaysOpen = (int)(today - i.InvoiceDate.Date).TotalDays,
                IsOverdue = (today - i.InvoiceDate.Date).TotalDays >= overdueDays
            };
        }).ToList();

        return Ok(new GoldCreditReportDto
        {
            Rows = rows,
            OpenCount = rows.Count,
            OverdueCount = rows.Count(r => r.IsOverdue),
            TotalRemainingIqd = items.Where(i => i.PaymentCurrency == GoldCurrency.IQD).Sum(i => i.RemainingAmount),
            TotalRemainingUsd = items.Where(i => i.PaymentCurrency == GoldCurrency.USD).Sum(i => i.RemainingAmount)
        });
    }
}

public sealed class GoldStockReportDto
{
    public List<GoldStockRowDto> Rows { get; set; } = [];
    public decimal TotalGrams { get; set; }
    public decimal TotalValue { get; set; }
    public int LowStockCount { get; set; }
}

public sealed class GoldSalesReportDto
{
    public List<GoldInvoiceListDto> Rows { get; set; } = [];
    public int SaleCount { get; set; }
    public int PurchaseCount { get; set; }
    public decimal TotalSalesIqd { get; set; }
    public decimal TotalSalesUsd { get; set; }
    public decimal TotalPurchasesIqd { get; set; }
    public decimal TotalPurchasesUsd { get; set; }
    public decimal TotalWeightSoldGrams { get; set; }
    public decimal TotalWeightPurchasedGrams { get; set; }
}

public sealed class GoldCreditRowDto
{
    public GoldInvoiceListDto Invoice { get; set; } = new();
    public int DaysOpen { get; set; }
    public bool IsOverdue { get; set; }
}

public sealed class GoldCreditReportDto
{
    public List<GoldCreditRowDto> Rows { get; set; } = [];
    public int OpenCount { get; set; }
    public int OverdueCount { get; set; }
    public decimal TotalRemainingIqd { get; set; }
    public decimal TotalRemainingUsd { get; set; }
}
