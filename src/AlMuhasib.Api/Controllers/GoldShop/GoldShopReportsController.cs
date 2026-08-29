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

    [HttpGet("aging")]
    public async Task<ActionResult<GoldAgingReportDto>> GetAgingReport(CancellationToken ct)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;

        var today = DateTime.Today;
        var items = await Db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.TenantId == TenantId
                        && i.InvoiceType == GoldInvoiceType.Sale
                        && i.RemainingAmount > 0
                        && i.Status != GoldInvoiceStatus.Cancelled)
            .ToListAsync(ct);

        static int Bucket(int days) => days switch
        {
            <= 30 => 0,
            <= 60 => 1,
            <= 90 => 2,
            _ => 3
        };

        var rows = items.Select(i =>
        {
            var days = (int)(today - i.InvoiceDate.Date).TotalDays;
            return new GoldAgingRowDto
            {
                Invoice = GoldShopInvoiceMapper.ToListItem(i),
                DaysOpen = days,
                Bucket = Bucket(days),
                BucketLabel = Bucket(days) switch
                {
                    0 => "0-30",
                    1 => "31-60",
                    2 => "61-90",
                    _ => "90+"
                }
            };
        }).OrderByDescending(r => r.DaysOpen).ToList();

        return Ok(new GoldAgingReportDto
        {
            Rows = rows,
            Bucket0To30 = rows.Where(r => r.Bucket == 0).Sum(r => r.Invoice.RemainingAmount),
            Bucket31To60 = rows.Where(r => r.Bucket == 1).Sum(r => r.Invoice.RemainingAmount),
            Bucket61To90 = rows.Where(r => r.Bucket == 2).Sum(r => r.Invoice.RemainingAmount),
            Bucket90Plus = rows.Where(r => r.Bucket == 3).Sum(r => r.Invoice.RemainingAmount),
            TotalRemaining = rows.Sum(r => r.Invoice.RemainingAmount)
        });
    }

    [HttpGet("purchases")]
    public async Task<ActionResult<GoldPagedReportDto<GoldInvoiceListDto>>> GetPurchasesReport(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        var rows = await FilterInvoicesAsync(GoldInvoiceType.Purchase, from, to, ct);
        return Ok(new GoldPagedReportDto<GoldInvoiceListDto>
        {
            TotalCount = rows.Count,
            Items = rows.Select(GoldShopInvoiceMapper.ToListItem).ToList()
        });
    }

    [HttpGet("sale-returns")]
    public async Task<ActionResult<GoldPagedReportDto<GoldSaleReturnRowDto>>> GetSaleReturnsReport(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        var invoices = await FilterInvoicesAsync(GoldInvoiceType.SaleReturn, from, to, ct);
        var relatedIds = invoices.Where(i => i.RelatedInvoiceId.HasValue).Select(i => i.RelatedInvoiceId!.Value).Distinct().ToList();
        var relatedMap = relatedIds.Count == 0
            ? new Dictionary<int, string>()
            : await Db.GoldInvoices.AsNoTracking()
                .Where(i => i.TenantId == TenantId && relatedIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => i.InvoiceNumber, ct);

        var items = invoices.Select(i => new GoldSaleReturnRowDto
        {
            Invoice = GoldShopInvoiceMapper.ToListItem(i),
            RelatedInvoiceNumber = i.RelatedInvoiceId.HasValue && relatedMap.TryGetValue(i.RelatedInvoiceId.Value, out var num) ? num : null
        }).ToList();

        return Ok(new GoldPagedReportDto<GoldSaleReturnRowDto> { TotalCount = items.Count, Items = items });
    }

    [HttpGet("exchanges")]
    public async Task<ActionResult<GoldPagedReportDto<GoldExchangeRowDto>>> GetExchangesReport(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        var query = Db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Where(i => i.TenantId == TenantId && i.InvoiceType == GoldInvoiceType.Exchange && i.Status != GoldInvoiceStatus.Cancelled);
        if (from.HasValue) query = query.Where(i => i.InvoiceDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(i => i.InvoiceDate <= to.Value.Date);

        var invoices = await query.OrderByDescending(i => i.InvoiceDate).ToListAsync(ct);
        var items = invoices.Select(i =>
        {
            var inLines = i.Lines.Where(l => l.LineDirection == GoldInvoiceLineDirection.In).ToList();
            var outLines = i.Lines.Where(l => l.LineDirection == GoldInvoiceLineDirection.Out).ToList();
            return new GoldExchangeRowDto
            {
                Invoice = GoldShopInvoiceMapper.ToListItem(i),
                InWeightGrams = inLines.Sum(l => l.WeightGrams),
                OutWeightGrams = outLines.Sum(l => l.WeightGrams),
                InTotalValue = inLines.Sum(l => l.LineTotal),
                OutTotalValue = outLines.Sum(l => l.LineTotal),
                ExchangeCashDifference = i.ExchangeCashDifference
            };
        }).ToList();

        return Ok(new GoldPagedReportDto<GoldExchangeRowDto> { TotalCount = items.Count, Items = items });
    }

    [HttpGet("deleted-invoices")]
    public async Task<ActionResult<GoldPagedReportDto<GoldInvoiceListDto>>> GetDeletedInvoicesReport(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        var query = Db.GoldInvoices.IgnoreQueryFilters().AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.TenantId == TenantId && i.IsDeleted);
        if (from.HasValue)
            query = query.Where(i => (i.DeletedAt ?? i.UpdatedAt ?? i.CreatedAt) >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(i => (i.DeletedAt ?? i.UpdatedAt ?? i.CreatedAt) <= to.Value.Date);

        var items = await query.OrderByDescending(i => i.DeletedAt).ToListAsync(ct);
        return Ok(new GoldPagedReportDto<GoldInvoiceListDto>
        {
            TotalCount = items.Count,
            Items = items.Select(GoldShopInvoiceMapper.ToListItem).ToList()
        });
    }

    private async Task<List<Cloud.Core.Entities.CloudGoldInvoice>> FilterInvoicesAsync(
        GoldInvoiceType type, DateTime? from, DateTime? to, CancellationToken ct)
    {
        var query = Db.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.TenantId == TenantId && i.InvoiceType == type && i.Status != GoldInvoiceStatus.Cancelled);
        if (from.HasValue) query = query.Where(i => i.InvoiceDate >= from.Value.Date);
        if (to.HasValue) query = query.Where(i => i.InvoiceDate <= to.Value.Date);
        return await query.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.Id).ToListAsync(ct);
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

public sealed class GoldAgingRowDto
{
    public GoldInvoiceListDto Invoice { get; set; } = new();
    public int DaysOpen { get; set; }
    public int Bucket { get; set; }
    public string BucketLabel { get; set; } = string.Empty;
}

public sealed class GoldAgingReportDto
{
    public List<GoldAgingRowDto> Rows { get; set; } = [];
    public decimal Bucket0To30 { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket61To90 { get; set; }
    public decimal Bucket90Plus { get; set; }
    public decimal TotalRemaining { get; set; }
}

public sealed class GoldPagedReportDto<T>
{
    public int TotalCount { get; set; }
    public List<T> Items { get; set; } = [];
}

public sealed class GoldSaleReturnRowDto
{
    public GoldInvoiceListDto Invoice { get; set; } = new();
    public string? RelatedInvoiceNumber { get; set; }
}

public sealed class GoldExchangeRowDto
{
    public GoldInvoiceListDto Invoice { get; set; } = new();
    public decimal InWeightGrams { get; set; }
    public decimal OutWeightGrams { get; set; }
    public decimal InTotalValue { get; set; }
    public decimal OutTotalValue { get; set; }
    public decimal ExchangeCashDifference { get; set; }
}
