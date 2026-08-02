using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.Infrastructure.Data.Gold;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services.Gold;

public sealed class GoldReportService : IGoldReportService
{
    private readonly IDbContextFactory<GoldDbContext> _contextFactory;
    private readonly IGoldCustomerService _customerService;

    public GoldReportService(
        IDbContextFactory<GoldDbContext> contextFactory,
        IGoldCustomerService customerService)
    {
        _contextFactory = contextFactory;
        _customerService = customerService;
    }

    public async Task<GoldReportSummary> GetSummaryAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .Where(i => i.Status != GoldInvoiceStatus.Cancelled);

        if (dateFrom.HasValue)
            query = query.Where(i => i.InvoiceDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            query = query.Where(i => i.InvoiceDate.Date <= dateTo.Value.Date);

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .ToListAsync(cancellationToken);

        var sales = invoices.Where(i => i.InvoiceType == GoldInvoiceType.Sale).ToList();
        var purchases = invoices.Where(i => i.InvoiceType == GoldInvoiceType.Purchase).ToList();

        decimal makingIqd = 0, makingUsd = 0;
        foreach (var sale in sales)
        {
            var making = sale.TotalMakingCharge;
            if (sale.PricingCurrency == GoldCurrency.USD)
            {
                makingUsd += making;
                makingIqd += making * (sale.FxRate > 0 ? sale.FxRate : 1m);
            }
            else
            {
                makingIqd += making;
                makingUsd += making / (sale.FxRate > 0 ? sale.FxRate : 1m);
            }
        }

        var vouchers = context.GoldVouchers.AsNoTracking().AsQueryable();
        if (dateFrom.HasValue)
            vouchers = vouchers.Where(v => v.VoucherDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            vouchers = vouchers.Where(v => v.VoucherDate.Date <= dateTo.Value.Date);
        var voucherList = await vouchers.ToListAsync(cancellationToken);

        var expenses = context.GoldExpenses.AsNoTracking().AsQueryable();
        if (dateFrom.HasValue)
            expenses = expenses.Where(e => e.ExpenseDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            expenses = expenses.Where(e => e.ExpenseDate.Date <= dateTo.Value.Date);
        var expenseList = await expenses.ToListAsync(cancellationToken);

        var cashInIqd = voucherList.Where(v => v.VoucherType == GoldVoucherType.Receipt && v.Currency == GoldCurrency.IQD).Sum(v => v.Amount)
            + sales.SelectMany(s => s.Payments).Where(p => p.Currency == GoldCurrency.IQD).Sum(p => p.Amount);
        var cashInUsd = voucherList.Where(v => v.VoucherType == GoldVoucherType.Receipt && v.Currency == GoldCurrency.USD).Sum(v => v.Amount)
            + sales.SelectMany(s => s.Payments).Where(p => p.Currency == GoldCurrency.USD).Sum(p => p.Amount);
        var cashOutIqd = voucherList.Where(v => v.VoucherType == GoldVoucherType.Payment && v.Currency == GoldCurrency.IQD).Sum(v => v.Amount)
            + purchases.SelectMany(s => s.Payments).Where(p => p.Currency == GoldCurrency.IQD).Sum(p => p.Amount)
            + expenseList.Where(e => e.Currency == GoldCurrency.IQD).Sum(e => e.Amount);
        var cashOutUsd = voucherList.Where(v => v.VoucherType == GoldVoucherType.Payment && v.Currency == GoldCurrency.USD).Sum(v => v.Amount)
            + purchases.SelectMany(s => s.Payments).Where(p => p.Currency == GoldCurrency.USD).Sum(p => p.Amount)
            + expenseList.Where(e => e.Currency == GoldCurrency.USD).Sum(e => e.Amount);

        var creditCustomers = await context.GoldCustomers.AsNoTracking()
            .Where(c => c.CreditBalanceIqd > 0 || c.CreditBalanceUsd > 0)
            .ToListAsync(cancellationToken);

        var stock = await GoldInventoryService.BuildStockRowsAsync(context, null, cancellationToken);

        return new GoldReportSummary
        {
            DateFrom = dateFrom?.Date,
            DateTo = dateTo?.Date,
            SaleCount = sales.Count,
            PurchaseCount = purchases.Count,
            TotalSalesIqd = sales.Sum(s => s.TotalAmountIqd),
            TotalSalesUsd = sales.Sum(s => s.TotalAmountUsd),
            TotalPurchasesIqd = purchases.Sum(p => p.TotalAmountIqd),
            TotalPurchasesUsd = purchases.Sum(p => p.TotalAmountUsd),
            TotalMakingChargesIqd = GoldCurrencyHelper.Round(makingIqd),
            TotalMakingChargesUsd = GoldCurrencyHelper.Round(makingUsd),
            TotalWeightSoldGrams = sales.Sum(s => s.TotalWeightGrams),
            TotalWeightPurchasedGrams = purchases.Sum(p => p.TotalWeightGrams),
            CashInIqd = GoldCurrencyHelper.Round(cashInIqd),
            CashInUsd = GoldCurrencyHelper.Round(cashInUsd),
            CashOutIqd = GoldCurrencyHelper.Round(cashOutIqd),
            CashOutUsd = GoldCurrencyHelper.Round(cashOutUsd),
            CreditOutstandingIqd = creditCustomers.Sum(c => c.CreditBalanceIqd),
            CreditOutstandingUsd = creditCustomers.Sum(c => c.CreditBalanceUsd),
            ClosingStock = stock,
            Invoices = invoices.Select(GoldCurrencyHelper.ToListItem).ToList()
        };
    }

    public async Task<IReadOnlyList<GoldInvoiceListItem>> GetSalesReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        GoldInvoiceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var (items, _) = await GoldInvoiceQueryHelper.GetPagedAsync(
            _contextFactory,
            GoldInvoiceType.Sale,
            1,
            10_000,
            null,
            dateFrom,
            dateTo,
            status,
            null,
            cancellationToken);
        return items;
    }

    public async Task<IReadOnlyList<GoldInvoiceListItem>> GetPurchasesReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        GoldInvoiceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var (items, _) = await GoldInvoiceQueryHelper.GetPagedAsync(
            _contextFactory,
            GoldInvoiceType.Purchase,
            1,
            10_000,
            null,
            dateFrom,
            dateTo,
            status,
            null,
            cancellationToken);
        return items;
    }

    public async Task<IReadOnlyList<GoldStockRow>> GetStockReportAsync(
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await GoldInventoryService.BuildStockRowsAsync(context, warehouseId, cancellationToken);
    }

    public async Task<IReadOnlyList<GoldCustomerListItem>> GetCreditReportAsync(
        bool overdueOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (overdueOnly)
            return await _customerService.GetOverdueCreditCustomersAsync(cancellationToken);

        var (items, _) = await _customerService.GetPagedAsync(
            1,
            10_000,
            creditOnly: true,
            cancellationToken: cancellationToken);
        return items;
    }

    public async Task<IReadOnlyList<GoldAgingRow>> GetAgingReportAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var today = DateTime.Today;

        var openInvoices = await context.GoldInvoices.AsNoTracking()
            .Include(i => i.Customer)
            .Where(i => i.CustomerId.HasValue &&
                        i.RemainingAmount > 0 &&
                        i.Status != GoldInvoiceStatus.Cancelled &&
                        i.InvoiceType == GoldInvoiceType.Sale)
            .ToListAsync(cancellationToken);

        return openInvoices
            .GroupBy(i => i.CustomerId!.Value)
            .Select(g =>
            {
                var customer = g.First().Customer;
                decimal current = 0, d30 = 0, d60 = 0, d90 = 0, over = 0, totalUsd = 0;

                foreach (var inv in g)
                {
                    var age = (today - inv.InvoiceDate.Date).Days;
                    var amountIqd = inv.PricingCurrency == GoldCurrency.IQD
                        ? inv.RemainingAmount
                        : GoldCurrencyHelper.ConvertAmount(inv.RemainingAmount, GoldCurrency.USD, GoldCurrency.IQD, inv.FxRate > 0 ? inv.FxRate : 1m);
                    var amountUsd = inv.PricingCurrency == GoldCurrency.USD
                        ? inv.RemainingAmount
                        : GoldCurrencyHelper.ConvertAmount(inv.RemainingAmount, GoldCurrency.IQD, GoldCurrency.USD, inv.FxRate > 0 ? inv.FxRate : 1m);
                    totalUsd += amountUsd;

                    if (age <= 0)
                        current += amountIqd;
                    else if (age <= 30)
                        d30 += amountIqd;
                    else if (age <= 60)
                        d60 += amountIqd;
                    else if (age <= 90)
                        d90 += amountIqd;
                    else
                        over += amountIqd;
                }

                return new GoldAgingRow
                {
                    CustomerId = g.Key,
                    CustomerName = customer?.Name ?? string.Empty,
                    Phone = customer?.Phone ?? string.Empty,
                    CurrentIqd = GoldCurrencyHelper.Round(current),
                    Days1To30Iqd = GoldCurrencyHelper.Round(d30),
                    Days31To60Iqd = GoldCurrencyHelper.Round(d60),
                    Days61To90Iqd = GoldCurrencyHelper.Round(d90),
                    Over90Iqd = GoldCurrencyHelper.Round(over),
                    TotalIqd = GoldCurrencyHelper.Round(current + d30 + d60 + d90 + over),
                    TotalUsd = GoldCurrencyHelper.Round(totalUsd),
                    OpenInvoiceCount = g.Count(),
                    OldestOpenDate = g.Min(x => x.InvoiceDate)
                };
            })
            .OrderByDescending(r => r.TotalIqd)
            .ToList();
    }

    public async Task<IReadOnlyList<GoldKaratMovementRow>> GetKaratMovementReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var invoiceQuery = context.GoldInvoices.AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.Status != GoldInvoiceStatus.Cancelled);

        if (dateFrom.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.InvoiceDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.InvoiceDate.Date <= dateTo.Value.Date);
        if (warehouseId.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.WarehouseId == warehouseId.Value);

        var invoices = await invoiceQuery.ToListAsync(cancellationToken);
        var karats = await context.GoldKarats.AsNoTracking()
            .ToDictionaryAsync(k => k.KaratValue, k => k.Name, cancellationToken);
        var warehouses = await context.GoldWarehouses.AsNoTracking()
            .ToDictionaryAsync(w => w.Id, w => w.Name, cancellationToken);

        var transferQuery = context.GoldWarehouseTransfers.AsNoTracking().AsQueryable();
        if (dateFrom.HasValue)
            transferQuery = transferQuery.Where(t => t.TransferDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            transferQuery = transferQuery.Where(t => t.TransferDate.Date <= dateTo.Value.Date);
        if (warehouseId.HasValue)
            transferQuery = transferQuery.Where(t =>
                t.FromWarehouseId == warehouseId.Value || t.ToWarehouseId == warehouseId.Value);
        var transfers = await transferQuery.ToListAsync(cancellationToken);

        var stock = await GoldInventoryService.BuildStockRowsAsync(context, warehouseId, cancellationToken);
        var closingByKey = stock.ToDictionary(s => (s.WarehouseId, s.KaratValue), s => s.GramsOnHand);

        var keys = new HashSet<(int WarehouseId, int KaratValue)>();
        foreach (var inv in invoices)
        {
            var wh = inv.WarehouseId ?? 0;
            foreach (var line in inv.Lines)
                keys.Add((wh, line.KaratValue));
        }

        foreach (var t in transfers)
        {
            keys.Add((t.FromWarehouseId, t.KaratValue));
            keys.Add((t.ToWarehouseId, t.KaratValue));
        }

        foreach (var s in stock)
            keys.Add((s.WarehouseId, s.KaratValue));

        var rows = new List<GoldKaratMovementRow>();
        foreach (var (whId, karat) in keys.OrderBy(k => k.WarehouseId).ThenBy(k => k.KaratValue))
        {
            if (warehouseId.HasValue && whId != warehouseId.Value && whId != 0)
                continue;

            decimal purchased = 0, sold = 0, exIn = 0, exOut = 0, trIn = 0, trOut = 0;

            foreach (var inv in invoices.Where(i => (i.WarehouseId ?? 0) == whId || (whId == 0 && !i.WarehouseId.HasValue)))
            {
                foreach (var line in inv.Lines.Where(l => l.KaratValue == karat))
                {
                    if (inv.InvoiceType == GoldInvoiceType.Purchase ||
                        (inv.InvoiceType == GoldInvoiceType.Exchange && line.LineDirection == GoldInvoiceLineDirection.In))
                    {
                        if (inv.InvoiceType == GoldInvoiceType.Purchase)
                            purchased += line.WeightGrams;
                        else
                            exIn += line.WeightGrams;
                    }
                    else if (inv.InvoiceType == GoldInvoiceType.Sale ||
                             (inv.InvoiceType == GoldInvoiceType.Exchange && line.LineDirection == GoldInvoiceLineDirection.Out))
                    {
                        if (inv.InvoiceType == GoldInvoiceType.Sale)
                            sold += line.WeightGrams;
                        else
                            exOut += line.WeightGrams;
                    }
                }
            }

            trOut = transfers.Where(t => t.FromWarehouseId == whId && t.KaratValue == karat).Sum(t => t.WeightGrams);
            trIn = transfers.Where(t => t.ToWarehouseId == whId && t.KaratValue == karat).Sum(t => t.WeightGrams);

            closingByKey.TryGetValue((whId, karat), out var closing);

            rows.Add(new GoldKaratMovementRow
            {
                KaratValue = karat,
                KaratName = karats.TryGetValue(karat, out var name) ? name : $"عيار {karat}",
                WarehouseId = whId == 0 ? null : whId,
                WarehouseName = whId == 0
                    ? string.Empty
                    : (warehouses.TryGetValue(whId, out var whName) ? whName : $"مخزن #{whId}"),
                PurchasedGrams = GoldCurrencyHelper.Round(purchased),
                SoldGrams = GoldCurrencyHelper.Round(sold),
                ExchangeInGrams = GoldCurrencyHelper.Round(exIn),
                ExchangeOutGrams = GoldCurrencyHelper.Round(exOut),
                TransferredInGrams = GoldCurrencyHelper.Round(trIn),
                TransferredOutGrams = GoldCurrencyHelper.Round(trOut),
                NetMovementGrams = GoldCurrencyHelper.Round(purchased + exIn + trIn - sold - exOut - trOut),
                ClosingGrams = GoldCurrencyHelper.Round(closing)
            });
        }

        return rows;
    }

    public async Task<IReadOnlyList<GoldProfitabilityRow>> GetProfitabilityReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.GoldInvoices.AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.InvoiceType == GoldInvoiceType.Sale && i.Status != GoldInvoiceStatus.Cancelled);

        if (dateFrom.HasValue)
            query = query.Where(i => i.InvoiceDate.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            query = query.Where(i => i.InvoiceDate.Date <= dateTo.Value.Date);

        var sales = await query.ToListAsync(cancellationToken);
        var karats = await context.GoldKarats.AsNoTracking()
            .ToDictionaryAsync(k => k.KaratValue, k => k.Name, cancellationToken);
        var avgCosts = await context.GoldStockBalances.AsNoTracking()
            .GroupBy(s => s.KaratValue)
            .Select(g => new { KaratValue = g.Key, AvgCost = g.Average(x => x.AverageCostPerGram) })
            .ToDictionaryAsync(x => x.KaratValue, x => x.AvgCost, cancellationToken);

        return sales
            .SelectMany(s => s.Lines.Select(l => new { Sale = s, Line = l }))
            .GroupBy(x => x.Line.KaratValue)
            .Select(g =>
            {
                decimal salesValueIqd = 0, makingIqd = 0, weight = 0, costIqd = 0;
                foreach (var x in g)
                {
                    weight += x.Line.WeightGrams;
                    var fx = x.Sale.FxRate > 0 ? x.Sale.FxRate : 1m;
                    var goldIqd = x.Sale.PricingCurrency == GoldCurrency.IQD
                        ? x.Line.GoldValue
                        : x.Line.GoldValue * fx;
                    var making = x.Sale.PricingCurrency == GoldCurrency.IQD
                        ? x.Line.MakingCharge
                        : x.Line.MakingCharge * fx;
                    salesValueIqd += goldIqd;
                    makingIqd += making;
                    avgCosts.TryGetValue(x.Line.KaratValue, out var avgCost);
                    costIqd += x.Line.WeightGrams * avgCost;
                }

                return new GoldProfitabilityRow
                {
                    KaratValue = g.Key,
                    KaratName = karats.TryGetValue(g.Key, out var name) ? name : $"عيار {g.Key}",
                    WeightSoldGrams = GoldCurrencyHelper.Round(weight),
                    SalesGoldValue = GoldCurrencyHelper.Round(salesValueIqd),
                    MakingCharges = GoldCurrencyHelper.Round(makingIqd),
                    EstimatedCost = GoldCurrencyHelper.Round(costIqd),
                    GrossProfit = GoldCurrencyHelper.Round(salesValueIqd + makingIqd - costIqd),
                    Currency = GoldCurrency.IQD
                };
            })
            .OrderBy(r => r.KaratValue)
            .ToList();
    }

    public async Task<IReadOnlyList<GoldAuditReportRow>> GetAuditReportAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? entityName = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (dateFrom.HasValue)
            query = query.Where(a => a.Timestamp.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue)
            query = query.Where(a => a.Timestamp.Date <= dateTo.Value.Date);
        if (!string.IsNullOrWhiteSpace(entityName))
        {
            var term = entityName.Trim();
            query = query.Where(a => a.EntityName.Contains(term));
        }

        // Prefer Gold-related entities when no filter provided.
        if (string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName.StartsWith("Gold"));

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Take(5_000)
            .ToListAsync(cancellationToken);

        return logs.Select(a => new GoldAuditReportRow
        {
            Timestamp = a.Timestamp,
            Action = a.Action.ToString(),
            EntityName = a.EntityName,
            EntityId = a.EntityId == 0 ? null : a.EntityId,
            UserName = a.CreatedBy,
            Details = string.Empty
        }).ToList();
    }
}
