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

    [HttpGet("cash-movement")]
    public async Task<ActionResult<GoldPagedReportDto<GoldCashMovementDto>>> GetCashMovement(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? cashBoxId, CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        var boxes = await Db.GoldCashBoxes.AsNoTracking().Where(b => b.TenantId == TenantId)
            .ToDictionaryAsync(b => b.Id, ct);
        var rows = new List<GoldCashMovementDto>();

        var paymentsQ = Db.GoldPayments.AsNoTracking()
            .Include(p => p.Invoice!).ThenInclude(i => i!.Customer)
            .Include(p => p.Invoice!).ThenInclude(i => i!.Supplier)
            .Where(p => p.TenantId == TenantId);
        if (cashBoxId.HasValue) paymentsQ = paymentsQ.Where(p => p.CashBoxId == cashBoxId.Value);
        if (from.HasValue) paymentsQ = paymentsQ.Where(p => p.PaymentDate >= from.Value.Date);
        if (to.HasValue) paymentsQ = paymentsQ.Where(p => p.PaymentDate <= to.Value.Date);
        foreach (var p in await paymentsQ.OrderByDescending(p => p.PaymentDate).Take(3000).ToListAsync(ct))
        {
            var isPurchase = p.Invoice?.InvoiceType == GoldInvoiceType.Purchase;
            boxes.TryGetValue(p.CashBoxId ?? 0, out var box);
            rows.Add(new GoldCashMovementDto
            {
                Date = p.PaymentDate,
                MovementType = isPurchase ? "دفع شراء" : "تحصيل بيع",
                Reference = p.Invoice?.InvoiceNumber ?? $"#{p.Id}",
                PartyName = p.Invoice?.Supplier?.Name ?? p.Invoice?.Customer?.Name ?? "—",
                CashBoxName = box?.Name ?? "—",
                Currency = p.Currency.ToString(),
                AmountIn = isPurchase ? 0 : p.Amount,
                AmountOut = isPurchase ? p.Amount : 0,
                Notes = p.Notes
            });
        }

        var vouchersQ = Db.GoldVouchers.AsNoTracking().Where(v => v.TenantId == TenantId && !v.IsDeleted);
        if (cashBoxId.HasValue) vouchersQ = vouchersQ.Where(v => v.CashBoxId == cashBoxId.Value);
        if (from.HasValue) vouchersQ = vouchersQ.Where(v => v.VoucherDate >= from.Value.Date);
        if (to.HasValue) vouchersQ = vouchersQ.Where(v => v.VoucherDate <= to.Value.Date);
        var customers = await Db.GoldCustomers.AsNoTracking().Where(c => c.TenantId == TenantId)
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);
        var suppliers = await Db.GoldSuppliers.AsNoTracking().Where(s => s.TenantId == TenantId)
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        foreach (var v in await vouchersQ.OrderByDescending(v => v.VoucherDate).Take(2000).ToListAsync(ct))
        {
            boxes.TryGetValue(v.CashBoxId ?? 0, out var box);
            var isReceipt = v.VoucherType == GoldVoucherType.Receipt;
            var party = v.SupplierId.HasValue && suppliers.TryGetValue(v.SupplierId.Value, out var sn) ? sn
                : v.CustomerId.HasValue && customers.TryGetValue(v.CustomerId.Value, out var cn) ? cn : "—";
            rows.Add(new GoldCashMovementDto
            {
                Date = v.VoucherDate,
                MovementType = isReceipt ? "سند قبض" : "سند صرف",
                Reference = v.VoucherNumber,
                PartyName = party,
                CashBoxName = box?.Name ?? "—",
                Currency = v.Currency.ToString(),
                AmountIn = isReceipt ? v.Amount : 0,
                AmountOut = isReceipt ? 0 : v.Amount,
                Notes = v.Notes
            });
        }

        var expensesQ = Db.GoldExpenses.AsNoTracking().Where(e => e.TenantId == TenantId);
        if (cashBoxId.HasValue) expensesQ = expensesQ.Where(e => e.CashBoxId == cashBoxId.Value);
        if (from.HasValue) expensesQ = expensesQ.Where(e => e.ExpenseDate >= from.Value.Date);
        if (to.HasValue) expensesQ = expensesQ.Where(e => e.ExpenseDate <= to.Value.Date);
        foreach (var e in await expensesQ.OrderByDescending(e => e.ExpenseDate).Take(2000).ToListAsync(ct))
        {
            boxes.TryGetValue(e.CashBoxId, out var box);
            rows.Add(new GoldCashMovementDto
            {
                Date = e.ExpenseDate,
                MovementType = "مصروف",
                Reference = $"مصروف #{e.Id}",
                PartyName = "—",
                CashBoxName = box?.Name ?? "—",
                Currency = e.Currency.ToString(),
                AmountIn = 0,
                AmountOut = e.Amount,
                Notes = e.Notes
            });
        }

        var ordered = rows.OrderByDescending(r => r.Date).ThenByDescending(r => r.Reference).ToList();
        return Ok(new GoldPagedReportDto<GoldCashMovementDto> { TotalCount = ordered.Count, Items = ordered });
    }

    [HttpGet("karat-movement")]
    public async Task<ActionResult<GoldPagedReportDto<GoldKaratMovementDto>>> GetKaratMovement(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? warehouseId, CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        var invQ = Db.GoldInvoices.AsNoTracking().Include(i => i.Lines)
            .Where(i => i.TenantId == TenantId && i.Status != GoldInvoiceStatus.Cancelled);
        if (from.HasValue) invQ = invQ.Where(i => i.InvoiceDate >= from.Value.Date);
        if (to.HasValue) invQ = invQ.Where(i => i.InvoiceDate <= to.Value.Date);
        if (warehouseId.HasValue) invQ = invQ.Where(i => i.WarehouseId == warehouseId.Value);
        var invoices = await invQ.ToListAsync(ct);
        var stocks = await Db.GoldStockBalances.AsNoTracking().Where(s => s.TenantId == TenantId).ToListAsync(ct);
        if (warehouseId.HasValue) stocks = stocks.Where(s => s.WarehouseId == warehouseId.Value).ToList();

        var byKarat = invoices.SelectMany(i => i.Lines.Select(l => new { i.InvoiceType, l }))
            .GroupBy(x => x.l.KaratValue)
            .Select(g =>
            {
                decimal purchased = 0, sold = 0, exIn = 0, exOut = 0, returned = 0;
                foreach (var x in g)
                {
                    if (x.InvoiceType == GoldInvoiceType.Purchase) purchased += x.l.WeightGrams;
                    else if (x.InvoiceType == GoldInvoiceType.Sale) sold += x.l.WeightGrams;
                    else if (x.InvoiceType == GoldInvoiceType.SaleReturn) returned += x.l.WeightGrams;
                    else if (x.InvoiceType == GoldInvoiceType.Exchange)
                    {
                        if (x.l.LineDirection == GoldInvoiceLineDirection.In) exIn += x.l.WeightGrams;
                        else exOut += x.l.WeightGrams;
                    }
                }
                var closing = stocks.Where(s => s.KaratValue == g.Key).Sum(s => s.GramsOnHand);
                return new GoldKaratMovementDto
                {
                    KaratValue = g.Key,
                    KaratName = $"عيار {g.Key}",
                    PurchasedGrams = purchased,
                    SoldGrams = sold,
                    ReturnedGrams = returned,
                    ExchangeInGrams = exIn,
                    ExchangeOutGrams = exOut,
                    NetMovementGrams = purchased + exIn + returned - sold - exOut,
                    ClosingGrams = closing
                };
            }).OrderBy(r => r.KaratValue).ToList();

        return Ok(new GoldPagedReportDto<GoldKaratMovementDto> { TotalCount = byKarat.Count, Items = byKarat });
    }

    [HttpGet("profitability")]
    public async Task<ActionResult<GoldPagedReportDto<GoldProfitabilityDto>>> GetProfitability(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        var q = Db.GoldInvoices.AsNoTracking().Include(i => i.Lines)
            .Where(i => i.TenantId == TenantId && i.InvoiceType == GoldInvoiceType.Sale && i.Status != GoldInvoiceStatus.Cancelled);
        if (from.HasValue) q = q.Where(i => i.InvoiceDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(i => i.InvoiceDate <= to.Value.Date);
        var sales = await q.ToListAsync(ct);
        var avgCosts = await Db.GoldStockBalances.AsNoTracking().Where(s => s.TenantId == TenantId)
            .GroupBy(s => s.KaratValue)
            .Select(g => new { KaratValue = g.Key, AvgCost = g.Average(x => x.AverageCostPerGram) })
            .ToDictionaryAsync(x => x.KaratValue, x => x.AvgCost, ct);

        var rows = sales.SelectMany(s => s.Lines.Select(l => new { Sale = s, Line = l }))
            .GroupBy(x => x.Line.KaratValue)
            .Select(g =>
            {
                decimal salesValue = 0, making = 0, weight = 0, cost = 0;
                foreach (var x in g)
                {
                    weight += x.Line.WeightGrams;
                    var fx = x.Sale.FxRate > 0 ? x.Sale.FxRate : 1m;
                    salesValue += x.Sale.PricingCurrency == GoldCurrency.IQD ? x.Line.GoldValue : x.Line.GoldValue * fx;
                    making += x.Sale.PricingCurrency == GoldCurrency.IQD ? x.Line.MakingCharge : x.Line.MakingCharge * fx;
                    avgCosts.TryGetValue(x.Line.KaratValue, out var avg);
                    cost += x.Line.WeightGrams * avg;
                }
                return new GoldProfitabilityDto
                {
                    KaratValue = g.Key,
                    KaratName = $"عيار {g.Key}",
                    WeightSoldGrams = weight,
                    SalesGoldValue = salesValue,
                    MakingCharges = making,
                    EstimatedCost = cost,
                    GrossProfit = salesValue + making - cost
                };
            }).OrderBy(r => r.KaratValue).ToList();

        return Ok(new GoldPagedReportDto<GoldProfitabilityDto> { TotalCount = rows.Count, Items = rows });
    }

    [HttpGet("user-performance")]
    public async Task<ActionResult<GoldPagedReportDto<GoldUserPerformanceDto>>> GetUserPerformance(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct = default)
    {
        if (await EnsureGoldShopTenantAsync(ct) is { } err) return err;
        var q = Db.GoldInvoices.AsNoTracking()
            .Where(i => i.TenantId == TenantId && i.Status != GoldInvoiceStatus.Cancelled);
        if (from.HasValue) q = q.Where(i => i.InvoiceDate >= from.Value.Date);
        if (to.HasValue) q = q.Where(i => i.InvoiceDate <= to.Value.Date);
        var invoices = await q.ToListAsync(ct);
        var rows = invoices.Where(i => !string.IsNullOrWhiteSpace(i.CreatedBy))
            .GroupBy(i => i.CreatedBy, StringComparer.OrdinalIgnoreCase)
            .Select(g => new GoldUserPerformanceDto
            {
                UserName = g.Key,
                SalesCount = g.Count(i => i.InvoiceType == GoldInvoiceType.Sale),
                PurchasesCount = g.Count(i => i.InvoiceType == GoldInvoiceType.Purchase),
                ExchangeCount = g.Count(i => i.InvoiceType == GoldInvoiceType.Exchange),
                ReturnCount = g.Count(i => i.InvoiceType == GoldInvoiceType.SaleReturn),
                SalesAmountIqd = g.Where(i => i.InvoiceType == GoldInvoiceType.Sale).Sum(i => i.TotalAmountIqd),
                PurchasesAmountIqd = g.Where(i => i.InvoiceType == GoldInvoiceType.Purchase).Sum(i => i.TotalAmountIqd)
            })
            .OrderByDescending(r => r.SalesCount + r.PurchasesCount)
            .ToList();
        return Ok(new GoldPagedReportDto<GoldUserPerformanceDto> { TotalCount = rows.Count, Items = rows });
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

public sealed class GoldCashMovementDto
{
    public DateTime Date { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public string CashBoxName { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal AmountIn { get; set; }
    public decimal AmountOut { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class GoldKaratMovementDto
{
    public int KaratValue { get; set; }
    public string KaratName { get; set; } = string.Empty;
    public decimal PurchasedGrams { get; set; }
    public decimal SoldGrams { get; set; }
    public decimal ReturnedGrams { get; set; }
    public decimal ExchangeInGrams { get; set; }
    public decimal ExchangeOutGrams { get; set; }
    public decimal NetMovementGrams { get; set; }
    public decimal ClosingGrams { get; set; }
}

public sealed class GoldProfitabilityDto
{
    public int KaratValue { get; set; }
    public string KaratName { get; set; } = string.Empty;
    public decimal WeightSoldGrams { get; set; }
    public decimal SalesGoldValue { get; set; }
    public decimal MakingCharges { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal GrossProfit { get; set; }
}

public sealed class GoldUserPerformanceDto
{
    public string UserName { get; set; } = string.Empty;
    public int SalesCount { get; set; }
    public int PurchasesCount { get; set; }
    public int ExchangeCount { get; set; }
    public int ReturnCount { get; set; }
    public decimal SalesAmountIqd { get; set; }
    public decimal PurchasesAmountIqd { get; set; }
}
