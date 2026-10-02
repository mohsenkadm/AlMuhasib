using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Cloud.Infrastructure.Mobile;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Reports;

public sealed partial class CloudReportService : Application.Abstractions.ICloudReportService
{
    private readonly CloudDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CloudReportService(CloudDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    private int RequireTenantId()
    {
        var tid = _tenantContext.TenantId;
        if (tid is null || tid.Value <= 0)
            throw new InvalidOperationException("Tenant context is required");
        return tid.Value;
    }

    /// <summary>Normalize "to" date to include the entire day (start of next day).</summary>
    private static DateTime? EndOfDay(DateTime? to) => to?.Date.AddDays(1);

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // SALES
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<SalesReportResult> GetSalesReportAsync(DateTime? from, DateTime? to, int? customerId, PaymentMethod? method, int? warehouseId = null)
    {
        var tenantId = RequireTenantId();
        var context = _db;
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));

        IQueryable<CloudInvoice> BuildSalesQuery(AccountingCurrency currency)
        {
            var query = CloudInvoiceFilters.ForProfitAndSalesTotals(
                    context.Invoices
                        .ForTenant(tenantId)
                        .Include(i => i.Customer)
                        .Include(i => i.Warehouse)
                        .Include(i => i.InstallmentPlans),
                    context.InstallmentPlans.ForTenant(tenantId),
                    currency);

            if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
            if (to.HasValue) query = query.Where(i => i.Date < EndOfDay(to));
            if (customerId.HasValue) query = query.Where(i => i.CustomerId == customerId.Value);
            if (method.HasValue) query = query.Where(i => i.PaymentMethod == method.Value);
            if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);
            return query;
        }

        var iqdInvoices = await BuildSalesQuery(AccountingCurrency.IQD).OrderByDescending(i => i.Date).ToListAsync();
        var usdInvoices = await BuildSalesQuery(AccountingCurrency.USD).OrderByDescending(i => i.Date).ToListAsync();
        var invoices = iqdInvoices.Concat(usdInvoices).OrderByDescending(i => i.Date).ThenBy(i => i.Currency).ToList();

        decimal Signed(CloudInvoice i) => CloudInvoiceFilters.SignedNetAmount(i);
        decimal ResolveCompanyFee(CloudInvoice i)
        {
            if (i.InvoiceType != InvoiceType.Installment)
                return 0;

            var plan = i.InstallmentPlans.FirstOrDefault();
            if (plan is null || !CompanyFeeHelper.AppliesTo(plan.InstallmentType))
                return 0;

            return plan.CompanyFeeAmount > 0
                ? plan.CompanyFeeAmount
                : CompanyFeeHelper.CalculateAmount(i.NetAmount);
        }

        decimal KpiAmount(CloudInvoice i) =>
            FinancialReportIqdFoldIn.SignedSalesInBaseIqd(i.InvoiceType, i.NetAmount, i.Currency, i.FxRate, foldInUsd);

        var totalSales = foldInUsd ? invoices.Sum(KpiAmount) : iqdInvoices.Sum(Signed);
        var totalSalesUsd = foldInUsd ? 0m : usdInvoices.Sum(Signed);
        var kpiInvoices = foldInUsd ? invoices : iqdInvoices;
        return new SalesReportResult
        {
            TotalSales = totalSales,
            TotalSalesUsd = totalSalesUsd,
            TotalCompanyFees = iqdInvoices.Sum(ResolveCompanyFee),
            CashSales = kpiInvoices.Where(i => i.PaymentMethod == PaymentMethod.Cash).Sum(foldInUsd ? KpiAmount : Signed),
            CreditSales = kpiInvoices.Where(i => i.PaymentMethod == PaymentMethod.Credit).Sum(foldInUsd ? KpiAmount : Signed),
            InstallmentSales = kpiInvoices.Where(i => i.PaymentMethod == PaymentMethod.Installment).Sum(foldInUsd ? KpiAmount : Signed),
            InvoiceCount = invoices.Count,
            AverageInvoice = kpiInvoices.Count > 0 ? totalSales / kpiInvoices.Count : 0,
            TodaySales = kpiInvoices.Where(i => i.Date.Date == DateTime.Today).Sum(foldInUsd ? KpiAmount : Signed),
            TodaySalesUsd = foldInUsd ? 0m : usdInvoices.Where(i => i.Date.Date == DateTime.Today).Sum(Signed),
            DailyChart = kpiInvoices.GroupBy(i => i.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(foldInUsd ? KpiAmount : Signed) })
                .OrderBy(d => d.Date).ToList(),
            Rows = invoices.Select(i =>
            {
                var isReturn = i.InvoiceType == InvoiceType.SaleReturn;
                var signed = Signed(i);
                return new SalesReportRow
                {
                    InvoiceId = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    Date = i.Date,
                    CustomerName = i.Customer?.Name ?? "\u2014",
                    CustomerFileNumber = i.Customer?.FileNumber,
                    WarehouseName = i.Warehouse?.Name ?? "\u2014",
                    PaymentMethod = i.PaymentMethod switch
                    {
                        PaymentMethod.Cash => "\u0646\u0642\u062f\u064a",
                        PaymentMethod.Credit => "\u0622\u062c\u0644",
                        PaymentMethod.Installment => "\u0623\u0642\u0633\u0627\u0637",
                        _ => "\u2014"
                    },
                    InvoiceType = i.InvoiceType,
                    InvoiceTypeLabel = i.InvoiceType switch
                    {
                        InvoiceType.Installment => "أقساط",
                        InvoiceType.SaleReturn => "مرتجع مبيعات",
                        _ => "مبيعات"
                    },
                    IsReturn = isReturn,
                    TotalAmount = isReturn ? -Math.Abs(i.TotalAmount) : i.TotalAmount,
                    Discount = i.DiscountAmount,
                    NetAmount = signed,
                    CompanyFeeAmount = ResolveCompanyFee(i),
                    CreditDueDate = i.CreditDueDate,
                    PaidAmount = isReturn ? -Math.Abs(i.PaidAmount) : i.PaidAmount,
                    RemainingAmount = isReturn ? 0 : i.RemainingAmount,
                    IsCreditPaid = i.IsCreditPaid,
                    Currency = i.Currency
                };
            }).ToList()
        };
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // PURCHASES
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<PurchasesReportResult> GetPurchasesReportAsync(DateTime? from, DateTime? to, int? supplierId, int? warehouseId, PaymentMethod? method = null)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));

        IQueryable<CloudInvoice> BuildPurchasesQuery(AccountingCurrency currency)
        {
            var query = CloudInvoiceFilters.ForPurchasesTotals(
                    context.Invoices
                        .Include(i => i.Supplier)
                        .Include(i => i.Warehouse),
                    currency);

            if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
            if (to.HasValue) query = query.Where(i => i.Date < EndOfDay(to));
            if (supplierId.HasValue) query = query.Where(i => i.SupplierId == supplierId.Value);
            if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);
            if (method.HasValue) query = query.Where(i => i.PaymentMethod == method.Value);
            return query;
        }

        var iqdInvoices = await BuildPurchasesQuery(AccountingCurrency.IQD).OrderByDescending(i => i.Date).ToListAsync();
        var usdInvoices = await BuildPurchasesQuery(AccountingCurrency.USD).OrderByDescending(i => i.Date).ToListAsync();
        var invoices = iqdInvoices.Concat(usdInvoices).OrderByDescending(i => i.Date).ThenBy(i => i.Currency).ToList();
        decimal Signed(CloudInvoice i) => CloudInvoiceFilters.SignedNetAmount(i);
        decimal KpiAmount(CloudInvoice i) =>
            FinancialReportIqdFoldIn.AmountInBaseIqd(Signed(i), i.Currency, i.FxRate, foldInUsd);

        var totalPurchases = foldInUsd ? invoices.Sum(KpiAmount) : iqdInvoices.Sum(Signed);
        var totalPurchasesUsd = foldInUsd ? 0m : usdInvoices.Sum(Signed);
        var kpiInvoices = foldInUsd ? invoices : iqdInvoices;

        var dailySource = foldInUsd
            ? invoices
            : iqdInvoices.Count > 0 ? iqdInvoices : usdInvoices;
        var dailyCurrency = foldInUsd || iqdInvoices.Count > 0
            ? AccountingCurrency.IQD
            : AccountingCurrency.USD;
        var supplierSource = foldInUsd
            ? invoices
            : iqdInvoices.Count > 0 ? iqdInvoices : usdInvoices;

        return new PurchasesReportResult
        {
            TotalPurchases = totalPurchases,
            TotalPurchasesUsd = totalPurchasesUsd,
            InvoiceCount = invoices.Count,
            AverageInvoice = kpiInvoices.Count > 0 ? totalPurchases / kpiInvoices.Count : 0,
            TodayPurchases = kpiInvoices.Where(i => i.Date.Date == DateTime.Today).Sum(foldInUsd ? KpiAmount : Signed),
            TodayPurchasesUsd = foldInUsd ? 0m : usdInvoices.Where(i => i.Date.Date == DateTime.Today).Sum(Signed),
            DailyChartCurrency = dailyCurrency,
            DailyChart = BuildFilledDailyPurchasesChart(dailySource, from, to, foldInUsd ? KpiAmount : Signed),
            BySupplierChart = supplierSource.GroupBy(i => i.Supplier?.Name ?? "\u0623\u062e\u0631\u0649")
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(foldInUsd ? KpiAmount : Signed) })
                .OrderByDescending(x => x.Amount).Take(6).ToList(),
            Rows = invoices.Select(i =>
            {
                var isReturn = i.InvoiceType == InvoiceType.PurchaseReturn;
                var signed = Signed(i);
                return new PurchasesReportRow
                {
                    InvoiceId = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    Date = i.Date,
                    SupplierName = i.Supplier?.Name ?? "\u2014",
                    WarehouseName = i.Warehouse?.Name ?? "\u2014",
                    PaymentMethod = i.PaymentMethod switch
                    {
                        PaymentMethod.Cash => "\u0646\u0642\u062f\u064a",
                        PaymentMethod.Credit => "\u0622\u062c\u0644",
                        _ => "\u2014"
                    },
                    InvoiceType = i.InvoiceType,
                    InvoiceTypeLabel = isReturn ? "مرتجع مشتريات" : "مشتريات",
                    IsReturn = isReturn,
                    TotalAmount = isReturn ? -Math.Abs(i.TotalAmount) : i.TotalAmount,
                    Discount = i.DiscountAmount,
                    NetAmount = signed,
                    PaidAmount = isReturn ? -Math.Abs(i.PaidAmount) : i.PaidAmount,
                    RemainingAmount = isReturn ? 0 : i.RemainingAmount,
                    IsCreditPaid = i.IsCreditPaid,
                    Currency = i.Currency
                };
            }).ToList()
        };
    }

    private static List<DailyAmountPoint> BuildFilledDailyPurchasesChart(
        IReadOnlyList<CloudInvoice> invoices,
        DateTime? from,
        DateTime? to,
        Func<CloudInvoice, decimal> signed)
    {
        if (invoices.Count == 0)
            return [];

        var byDay = invoices
            .GroupBy(i => i.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(signed));

        var start = from?.Date ?? byDay.Keys.Min();
        var end = to?.Date ?? byDay.Keys.Max();
        if (end < start)
            (start, end) = (end, start);

        var spanDays = (end - start).TotalDays;
        if (spanDays > 90)
            return byDay.OrderBy(kv => kv.Key)
                .Select(kv => new DailyAmountPoint { Date = kv.Key, Amount = kv.Value })
                .ToList();

        var list = new List<DailyAmountPoint>((int)spanDays + 1);
        for (var d = start; d <= end; d = d.AddDays(1))
            list.Add(new DailyAmountPoint { Date = d, Amount = byDay.GetValueOrDefault(d) });
        return list;
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // PROFIT
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<ProfitReportResult> GetProfitReportAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));

        var salesBaseQ = foldInUsd
            ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(context.Invoices, context.InstallmentPlans)
            : CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans);
        var salesUsdQ = CloudInvoiceFilters.ForProfitAndSalesTotals(
            context.Invoices, context.InstallmentPlans, AccountingCurrency.USD);
        var expBaseQ = foldInUsd
            ? context.Expenses.AsQueryable()
            : context.Expenses.Where(e => e.Currency == AccountingCurrency.IQD);
        var expUsdQ = context.Expenses.Where(e => e.Currency == AccountingCurrency.USD);
        var bankBaseQ = foldInUsd
            ? context.Vouchers.Where(v => v.VoucherType == VoucherType.BankReceipt)
            : context.Vouchers.Where(v => v.VoucherType == VoucherType.BankReceipt && v.Currency == AccountingCurrency.IQD);
        var distQ = context.ProfitDistributions.AsQueryable();

        if (from.HasValue)
        {
            salesBaseQ = salesBaseQ.Where(i => i.Date >= from.Value);
            salesUsdQ = salesUsdQ.Where(i => i.Date >= from.Value);
            expBaseQ = expBaseQ.Where(e => e.Date >= from.Value);
            expUsdQ = expUsdQ.Where(e => e.Date >= from.Value);
            bankBaseQ = bankBaseQ.Where(v => v.Date >= from.Value);
            distQ = distQ.Where(p => p.Date >= from.Value);
        }
        if (to.HasValue)
        {
            salesBaseQ = salesBaseQ.Where(i => i.Date < EndOfDay(to));
            salesUsdQ = salesUsdQ.Where(i => i.Date < EndOfDay(to));
            expBaseQ = expBaseQ.Where(e => e.Date < EndOfDay(to));
            expUsdQ = expUsdQ.Where(e => e.Date < EndOfDay(to));
            bankBaseQ = bankBaseQ.Where(v => v.Date < EndOfDay(to));
            distQ = distQ.Where(p => p.Date < EndOfDay(to));
        }

        var salesRows = await salesBaseQ
            .Select(i => new { i.InvoiceType, i.NetAmount, i.Currency, i.FxRate })
            .ToListAsync();
        var totalSales = FinancialReportIqdFoldIn.SumSignedSalesInBaseIqd(
            salesRows.Select(r => (r.InvoiceType, r.NetAmount, r.Currency, r.FxRate)), foldInUsd);
        var totalSalesUsd = foldInUsd ? 0m : await CloudInvoiceFilters.SumSignedNetAsync(salesUsdQ);
        var cogs = await CalculateCogsAsync(context, from, EndOfDay(to));
        var expenseRows = await expBaseQ
            .Select(e => new { e.Amount, e.Currency, e.FxRate })
            .ToListAsync();
        var totalExpenses = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            expenseRows.Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);
        var totalExpensesUsd = foldInUsd ? 0m : await expUsdQ.SumAsync(e => (decimal?)e.Amount) ?? 0;
        var bankRows = await bankBaseQ
            .Select(v => new { v.BankFees, v.Currency, v.FxRate })
            .ToListAsync();
        var totalBankFees = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            bankRows.Select(r => (r.BankFees, r.Currency, r.FxRate)), foldInUsd);
        var distributed = await distQ.SumAsync(p => (decimal?)p.DistributedAmount) ?? 0;
        var grossProfit = totalSales - cogs;
        var profitOpening = await CloudProductCostHelper.GetProfitOpeningBalanceAsync(context, to);
        var netProfit = grossProfit - totalExpenses - totalBankFees - distributed + profitOpening;

        return new ProfitReportResult
        {
            TotalSales = totalSales,
            TotalSalesUsd = totalSalesUsd,
            TotalPurchases = cogs,
            GrossProfit = grossProfit,
            TotalExpenses = totalExpenses,
            TotalExpensesUsd = totalExpensesUsd,
            TotalBankFees = totalBankFees,
            DistributedProfits = distributed,
            ProfitOpeningBalance = profitOpening,
            NetProfit = netProfit,
            ProfitMargin = totalSales > 0 ? Math.Round(grossProfit / totalSales * 100, 1) : 0
        };
    }

    public async Task<List<MonthlyProfitRow>> GetMonthlyProfitAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var rangeStart = (from ?? DateTime.Today.AddMonths(-12)).Date;
        var rangeEndExclusive = EndOfDay(to ?? DateTime.Today);
        if (!rangeEndExclusive.HasValue)
            return [];

        if (rangeEndExclusive.Value <= rangeStart)
            return [];

        var lastMonthStart = new DateTime((to ?? DateTime.Today).Year, (to ?? DateTime.Today).Month, 1);
        var result = new List<MonthlyProfitRow>();

        for (var cursor = new DateTime(rangeStart.Year, rangeStart.Month, 1);
             cursor <= lastMonthStart;
             cursor = cursor.AddMonths(1))
        {
            var monthStart = cursor;
            var monthEndExclusive = monthStart.AddMonths(1);
            var effectiveFrom = rangeStart > monthStart ? rangeStart : monthStart;
            var effectiveToExclusive = rangeEndExclusive.Value < monthEndExclusive
                ? rangeEndExclusive.Value
                : monthEndExclusive;

            if (effectiveFrom >= effectiveToExclusive)
                continue;

            var salesQ = foldInUsd
                ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(context.Invoices, context.InstallmentPlans)
                : CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans);
            salesQ = salesQ.Where(i => i.Date >= effectiveFrom && i.Date < effectiveToExclusive);
            var salesRows = await salesQ
                .Select(i => new { i.InvoiceType, i.NetAmount, i.Currency, i.FxRate })
                .ToListAsync();
            var sales = FinancialReportIqdFoldIn.SumSignedSalesInBaseIqd(
                salesRows.Select(r => (r.InvoiceType, r.NetAmount, r.Currency, r.FxRate)), foldInUsd);

            var purchases = await CalculateCogsAsync(context, effectiveFrom, effectiveToExclusive);
            var expenseQ = foldInUsd
                ? context.Expenses.AsQueryable()
                : context.Expenses.Where(e => e.Currency == AccountingCurrency.IQD);
            var expenseRows = await expenseQ
                .Where(e => e.Date >= effectiveFrom && e.Date < effectiveToExclusive)
                .Select(e => new { e.Amount, e.Currency, e.FxRate })
                .ToListAsync();
            var expenses = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                expenseRows.Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);

            var gross = sales - purchases;
            var net = gross - expenses;
            result.Add(new MonthlyProfitRow
            {
                Month = $"{cursor.Year}/{cursor.Month:D2}",
                Sales = sales,
                Purchases = purchases,
                GrossProfit = gross,
                Expenses = expenses,
                NetProfit = net,
                ProfitMargin = sales > 0 ? Math.Round(gross / sales * 100, 1) : 0
            });
        }

        return result;
    }

    private static async Task<decimal> CalculateCogsAsync(CloudDbContext context, DateTime? fromInclusive, DateTime? toExclusive)
    {
        // تكلفة البضاعة بالدينار لجميع المبيعات (بما فيها فواتير USD)
        var soldItemsQuery = context.InvoiceItems
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && ii.Invoice != null
                         && (ii.Invoice.InvoiceType == InvoiceType.Sale
                             || ii.Invoice.InvoiceType == InvoiceType.Installment
                             || ii.Invoice.InvoiceType == InvoiceType.SaleReturn));

        if (fromInclusive.HasValue)
            soldItemsQuery = soldItemsQuery.Where(ii => ii.Invoice!.Date >= fromInclusive.Value);
        if (toExclusive.HasValue)
            soldItemsQuery = soldItemsQuery.Where(ii => ii.Invoice!.Date < toExclusive.Value);

        var soldItems = await soldItemsQuery.ToListAsync();
        if (soldItems.Count == 0)
            return 0;

        var productIds = soldItems.Select(ii => ii.ProductId!.Value).Distinct().ToList();
        var stocks = await context.WarehouseStocks
            .Where(ws => productIds.Contains(ws.ProductId))
            .ToListAsync();

        var purchasesByProduct = await CloudProductCostHelper.GetPurchaseItemsByProductAsync(context, productIds);
        if (toExclusive.HasValue)
        {
            purchasesByProduct = purchasesByProduct.ToDictionary(
                kv => kv.Key,
                kv => kv.Value
                    .Where(ii => ii.Invoice == null || ii.Invoice.Date < toExclusive.Value)
                    .ToList());
        }

        decimal cogs = 0;
        foreach (var sold in soldItems)
        {
            var productId = sold.ProductId!.Value;
            var productPurchases = purchasesByProduct.GetValueOrDefault(productId) ?? [];
            var avgCost = CloudProductCostHelper.ComputeAverageUnitCostForProduct(productPurchases, stocks, productId);
            var qty = sold.Invoice!.InvoiceType == InvoiceType.SaleReturn
                ? -Math.Abs(sold.Quantity)
                : sold.Quantity;
            cogs += Math.Round(qty * avgCost, 0);
        }

        return cogs;
    }

    public async Task<List<ProfitInvoiceDetailRow>> GetProfitInvoiceDetailsAsync(DateTime? from, DateTime? to)
    {
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(_db, tenantId));
        var invoicesQ = (foldInUsd
                ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(_db.Invoices, _db.InstallmentPlans)
                : CloudInvoiceFilters.ForProfitAndSalesTotals(_db.Invoices, _db.InstallmentPlans))
            .AsQueryable();

        if (from.HasValue)
            invoicesQ = invoicesQ.Where(i => i.Date >= from.Value);
        if (to.HasValue)
            invoicesQ = invoicesQ.Where(i => i.Date < EndOfDay(to));

        var invoices = await invoicesQ
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .OrderByDescending(i => i.Date)
            .ToListAsync();
        if (invoices.Count == 0)
            return [];

        var soldItems = invoices
            .SelectMany(i => i.Items.Where(ii => ii.ProductId != null))
            .ToList();

        var productIds = soldItems.Select(ii => ii.ProductId!.Value).Distinct().ToList();
        var stocks = await _db.WarehouseStocks
            .Where(ws => productIds.Contains(ws.ProductId))
            .ToListAsync();

        var purchasesByProduct = await CloudProductCostHelper.GetPurchaseItemsByProductAsync(_db, productIds);
        if (to.HasValue)
        {
            var end = EndOfDay(to);
            purchasesByProduct = purchasesByProduct.ToDictionary(
                kv => kv.Key,
                kv => kv.Value
                    .Where(ii => ii.Invoice == null || !end.HasValue || ii.Invoice.Date < end.Value)
                    .ToList());
        }

        var rows = new List<ProfitInvoiceDetailRow>();
        foreach (var invoice in invoices)
        {
            decimal cost = 0;
            var lineItems = invoice.Items.Where(ii => ii.ProductId != null).ToList();
            var isReturn = invoice.InvoiceType == InvoiceType.SaleReturn;
            foreach (var item in lineItems)
            {
                var productId = item.ProductId!.Value;
                var productPurchases = purchasesByProduct.GetValueOrDefault(productId) ?? [];
                var avgCost = CloudProductCostHelper.ComputeAverageUnitCostForProduct(productPurchases, stocks, productId);
                var qty = isReturn ? -Math.Abs(item.Quantity) : item.Quantity;
                cost += Math.Round(qty * avgCost, 0);
            }

            var revenue = FinancialReportIqdFoldIn.AmountInBaseIqd(
                CloudInvoiceFilters.SignedNetAmount(invoice), invoice.Currency, invoice.FxRate, foldInUsd);
            var profit = revenue - cost;
            rows.Add(new ProfitInvoiceDetailRow
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                Date = invoice.Date,
                CustomerName = invoice.Customer?.Name ?? "—",
                CustomerFileNumber = invoice.Customer?.FileNumber,
                InvoiceTypeLabel = invoice.InvoiceType switch
                {
                    InvoiceType.Installment => "أقساط",
                    InvoiceType.SaleReturn => "مرتجع مبيعات",
                    _ => "مبيعات"
                },
                ItemCount = lineItems.Count,
                Revenue = revenue,
                Cost = cost,
                GrossProfit = profit,
                MarginPercent = revenue != 0 ? Math.Round(profit / revenue * 100, 1) : 0
            });
        }

        return rows;
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // INSTALLMENTS
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<InstallmentsSummaryResult> GetInstallmentsSummaryAsync(DateTime? from, DateTime? to, int? customerId, string? status)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var plansQ = context.InstallmentPlans.Include(p => p.Customer).Include(p => p.Installments).Include(p => p.Invoice)
            .Where(p => foldInUsd || p.Invoice!.Currency == AccountingCurrency.IQD);
        if (customerId.HasValue) plansQ = plansQ.Where(p => p.CustomerId == customerId.Value);
        var plans = await plansQ.ToListAsync();

        var rows = new List<InstallmentSummaryRow>();
        foreach (var plan in plans)
        {
            var inv = plan.Invoice!;
            decimal InBase(decimal amount) =>
                FinancialReportIqdFoldIn.AmountInBaseIqd(amount, inv.Currency, inv.FxRate, foldInUsd);

            var insts = plan.Installments.Where(i => !i.IsDeleted).ToList();
            if (from.HasValue) insts = insts.Where(i => i.DueDate >= from.Value).ToList();
            if (to.HasValue) insts = insts.Where(i => i.DueDate < EndOfDay(to)).ToList();
            if (insts.Count == 0) continue;

            var total = insts.Sum(i => InBase(i.Amount));
            var paid = insts.Sum(i => InBase(i.PaidAmount));
            var remaining = insts.Sum(i => InBase(i.RemainingAmount));
            var hasOverdue = insts.Any(i => i.Status == InstallmentStatus.Overdue);
            var allPaid = insts.All(i => i.Status == InstallmentStatus.Paid);
            var statusText = allPaid ? "\u0645\u0633\u062f\u062f" : hasOverdue ? "\u0645\u062a\u0623\u062e\u0631" : "\u0642\u064a\u062f \u0627\u0644\u062a\u0633\u062f\u064a\u062f";

            if (!string.IsNullOrEmpty(status) && status != "\u0627\u0644\u0643\u0644" && statusText != status) continue;

            rows.Add(new InstallmentSummaryRow
            {
                CustomerName = plan.Customer?.Name ?? "\u2014",
                CustomerFileNumber = plan.Customer?.FileNumber,
                PlanNumber = plan.Id.ToString(),
                TotalAmount = total, PaidAmount = paid, RemainingAmount = remaining,
                InstallmentCount = insts.Count, Status = statusText
            });
        }

        var allInsts = plans.SelectMany(p => p.Installments.Where(i => !i.IsDeleted)
            .Select(i => new { Inst = i, Inv = p.Invoice! })).ToList();
        var paidInsts = allInsts.Where(x => x.Inst.Status == InstallmentStatus.Paid);
        var overdueInsts = allInsts.Where(x => x.Inst.Status == InstallmentStatus.Overdue);
        var unpaidInsts = allInsts.Where(x => x.Inst.Status != InstallmentStatus.Paid);
        decimal InBaseAll(decimal amount, CloudInvoice inv) =>
            FinancialReportIqdFoldIn.AmountInBaseIqd(amount, inv.Currency, inv.FxRate, foldInUsd);

        return new InstallmentsSummaryResult
        {
            TotalAmount = allInsts.Sum(x => InBaseAll(x.Inst.Amount, x.Inv)),
            PaidAmount = allInsts.Sum(x => InBaseAll(x.Inst.PaidAmount, x.Inv)),
            UnpaidAmount = unpaidInsts.Sum(x => InBaseAll(x.Inst.RemainingAmount, x.Inv)),
            OverdueAmount = overdueInsts.Sum(x => InBaseAll(x.Inst.RemainingAmount, x.Inv)),
            TotalCount = allInsts.Count, PaidCount = paidInsts.Count(),
            UnpaidCount = unpaidInsts.Count(), OverdueCount = overdueInsts.Count(),
            Rows = rows,
            StatusChart =
            [
                new() { Name = "Ù…Ø³Ø¯Ø¯", Amount = paidInsts.Count() },
                new() { Name = "Ø¬Ø²Ø¦ÙŠ", Amount = allInsts.Count(x => x.Inst.Status == InstallmentStatus.PartiallyPaid) },
                new() { Name = "Ù…Ø¹Ù„Ù‚", Amount = allInsts.Count(x => x.Inst.Status == InstallmentStatus.Pending) },
                new() { Name = "Ù…ØªØ£Ø®Ø±", Amount = overdueInsts.Count() }
            ],
            MonthlyCollectionChart = allInsts.Where(x => x.Inst.PaymentDate.HasValue)
                .GroupBy(x => new DateTime(x.Inst.PaymentDate!.Value.Year, x.Inst.PaymentDate.Value.Month, 1))
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => InBaseAll(x.Inst.PaidAmount, x.Inv)) })
                .OrderBy(d => d.Date).ToList()
        };
    }

    public async Task<InstallmentDetailResult> GetInstallmentDetailAsync(int customerId)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var plans = await context.InstallmentPlans
            .Include(p => p.Customer).Include(p => p.Installments).Include(p => p.Invoice)
            .Where(p => p.CustomerId == customerId
                        && (foldInUsd || p.Invoice!.Currency == AccountingCurrency.IQD)).ToListAsync();

        var allInsts = plans.SelectMany(p => p.Installments.Where(i => !i.IsDeleted)
            .Select(i => new { Inst = i, Inv = p.Invoice! })).ToList();
        decimal InBase(decimal amount, CloudInvoice inv) =>
            FinancialReportIqdFoldIn.AmountInBaseIqd(amount, inv.Currency, inv.FxRate, foldInUsd);
        var totalAmt = allInsts.Sum(x => InBase(x.Inst.Amount, x.Inv));
        var paidAmt = allInsts.Sum(x => InBase(x.Inst.PaidAmount, x.Inv));

        return new InstallmentDetailResult
        {
            CustomerName = plans.FirstOrDefault()?.Customer?.Name ?? "\u2014",
            PlanCount = plans.Count, TotalAmount = totalAmt,
            CollectionRate = totalAmt > 0 ? Math.Round(paidAmt / totalAmt * 100, 1) : 0,
            AverageInstallment = allInsts.Count > 0 ? totalAmt / allInsts.Count : 0,
            Rows = allInsts.OrderBy(x => x.Inst.DueDate).Select(x => new InstallmentDetailRow
            {
                DueDate = x.Inst.DueDate,
                Amount = InBase(x.Inst.Amount, x.Inv),
                PaidAmount = InBase(x.Inst.PaidAmount, x.Inv),
                RemainingAmount = InBase(x.Inst.RemainingAmount, x.Inv),
                PaymentDate = x.Inst.PaymentDate,
                PlanNumber = x.Inst.InstallmentPlanId.ToString(),
                Status = x.Inst.Status switch
                {
                    InstallmentStatus.Paid => "\u0645\u0633\u062f\u062f",
                    InstallmentStatus.PartiallyPaid => "\u062c\u0632\u0626\u064a",
                    InstallmentStatus.Overdue => "\u0645\u062a\u0623\u062e\u0631",
                    _ => "\u0645\u0639\u0644\u0642"
                }
            }).ToList(),
            MonthlyDueChart = allInsts.GroupBy(x => new DateTime(x.Inst.DueDate.Year, x.Inst.DueDate.Month, 1))
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(x => InBase(x.Inst.Amount, x.Inv)) })
                .OrderBy(d => d.Date).ToList()
        };
    }

    public async Task<PaidInstallmentsResult> GetPaidInstallmentsAsync(DateTime? from, DateTime? to, int? customerId, int? cashBoxId)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var query = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Include(i => i.InstallmentPlan).ThenInclude(p => p!.Invoice)
            .Include(i => i.CashBox)
            .Where(i => i.Status == InstallmentStatus.Paid
                        && (foldInUsd || i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD));

        if (from.HasValue) query = query.Where(i => i.PaymentDate >= from.Value);
        if (to.HasValue) query = query.Where(i => i.PaymentDate < EndOfDay(to));
        if (customerId.HasValue) query = query.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);
        if (cashBoxId.HasValue) query = query.Where(i => i.CashBoxId == cashBoxId.Value);

        var insts = await query.OrderByDescending(i => i.PaymentDate).ToListAsync();
        decimal PaidInBase(CloudInstallment i)
        {
            var inv = i.InstallmentPlan!.Invoice!;
            return FinancialReportIqdFoldIn.AmountInBaseIqd(i.PaidAmount, inv.Currency, inv.FxRate, foldInUsd);
        }

        return new PaidInstallmentsResult
        {
            TotalPaid = insts.Sum(PaidInBase),
            PaidCount = insts.Count,
            MaxPaid = insts.Count > 0 ? insts.Max(PaidInBase) : 0,
            AveragePaymentDays = insts.Count > 0 ? (decimal)Math.Round(insts.Where(i => i.PaymentDate.HasValue).Average(i => (i.PaymentDate!.Value - i.DueDate).TotalDays), 0) : 0,
            Rows = insts.Select(i => new PaidInstallmentRow
            {
                CustomerName = i.InstallmentPlan?.Customer?.Name ?? "\u2014",
                CustomerFileNumber = i.InstallmentPlan?.Customer?.FileNumber,
                PlanNumber = i.InstallmentPlanId.ToString(),
                Amount = PaidInBase(i), PaymentDate = i.PaymentDate ?? i.DueDate,
                CashBoxName = i.CashBox?.Name ?? "\u2014"
            }).ToList(),
            MonthlyChart = insts.Where(i => i.PaymentDate.HasValue)
                .GroupBy(i => new DateTime(i.PaymentDate!.Value.Year, i.PaymentDate.Value.Month, 1))
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(PaidInBase) })
                .OrderBy(d => d.Date).ToList(),
            ByCashBoxChart = insts.GroupBy(i => i.CashBox?.Name ?? "\u063a\u064a\u0631 \u0645\u062d\u062f\u062f")
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(i => PaidInBase(i)) }).ToList()
        };
    }

    public async Task<UnpaidInstallmentsResult> GetUnpaidInstallmentsAsync(DateTime? from, DateTime? to, int? customerId)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var query = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Include(i => i.InstallmentPlan).ThenInclude(p => p!.Invoice)
            .Where(i => i.Status != InstallmentStatus.Paid
                        && (foldInUsd || i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD));

        if (from.HasValue) query = query.Where(i => i.DueDate >= from.Value);
        if (to.HasValue) query = query.Where(i => i.DueDate < EndOfDay(to));
        if (customerId.HasValue) query = query.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);

        var insts = await query.OrderBy(i => i.DueDate).ToListAsync();
        var today = DateTime.Today;
        decimal RemainingInBase(CloudInstallment i)
        {
            var inv = i.InstallmentPlan!.Invoice!;
            return FinancialReportIqdFoldIn.AmountInBaseIqd(i.RemainingAmount, inv.Currency, inv.FxRate, foldInUsd);
        }

        return new UnpaidInstallmentsResult
        {
            TotalUnpaid = insts.Sum(RemainingInBase),
            UnpaidCount = insts.Count,
            CustomerCount = insts.Select(i => i.InstallmentPlan?.CustomerId).Distinct().Count(),
            OldestOverdueDays = insts.Where(i => i.DueDate < today).Select(i => (today - i.DueDate).Days).DefaultIfEmpty(0).Max(),
            Rows = insts.Select(i => new UnpaidInstallmentRow
            {
                InstallmentId = i.Id,
                InvoiceId = i.InstallmentPlan?.InvoiceId ?? 0,
                CustomerName = i.InstallmentPlan?.Customer?.Name ?? "\u2014",
                CustomerFileNumber = i.InstallmentPlan?.Customer?.FileNumber,
                PlanNumber = i.InstallmentPlanId.ToString(),
                DueDate = i.DueDate, Amount = i.Amount, RemainingAmount = RemainingInBase(i),
                OverdueDays = i.DueDate < today ? (today - i.DueDate).Days : 0
            }).ToList(),
            ByCustomerChart = insts.GroupBy(i => i.InstallmentPlan?.Customer?.Name ?? "\u2014")
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(i => RemainingInBase(i)) })
                .OrderByDescending(x => x.Amount).Take(10).ToList()
        };
    }

    public async Task<OverdueResult> GetOverdueReportAsync(DateTime asOfDate, int? minDaysOverdue, int? customerId)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));

        // â”€â”€ 1. Overdue installments â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var instQuery = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Include(i => i.InstallmentPlan).ThenInclude(p => p!.Invoice)
            .Where(i => i.Status != InstallmentStatus.Paid
                        && i.DueDate < asOfDate
                        && (foldInUsd || i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD));

        if (customerId.HasValue) instQuery = instQuery.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);

        var insts = await instQuery.OrderBy(i => i.DueDate).ToListAsync();
        if (minDaysOverdue.HasValue)
            insts = insts.Where(i => (asOfDate - i.DueDate).Days >= minDaysOverdue.Value).ToList();

        var rows = insts.Select(i =>
        {
            var inv = i.InstallmentPlan!.Invoice!;
            var overdue = FinancialReportIqdFoldIn.AmountInBaseIqd(i.RemainingAmount, inv.Currency, inv.FxRate, foldInUsd);
            return new OverdueRow
            {
                InstallmentId = i.Id,
                InvoiceId = i.InstallmentPlan?.InvoiceId ?? 0,
                CustomerName = i.InstallmentPlan?.Customer?.Name ?? "\u2014",
                CustomerFileNumber = i.InstallmentPlan?.Customer?.FileNumber,
                Phone = i.InstallmentPlan?.Customer?.Phone ?? "\u2014",
                PlanNumber = i.InstallmentPlanId.ToString(),
                OverdueAmount = overdue,
                OverdueDays = (asOfDate - i.DueDate).Days,
                LastPaymentDate = i.PaymentDate,
                DueDate = i.DueDate
            };
        }).ToList();

        // â”€â”€ 2. Overdue credit invoices (Ø¢Ø¬Ù„) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var creditQuery = context.Invoices
            .Include(i => i.Customer)
            .Where(i => i.PaymentMethod == PaymentMethod.Credit
                        && (foldInUsd || i.Currency == AccountingCurrency.IQD)
                        && i.CreditDueDate.HasValue
                        && i.CreditDueDate.Value.Date < asOfDate.Date
                        && (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment));

        if (customerId.HasValue) creditQuery = creditQuery.Where(i => i.CustomerId == customerId.Value);

        var creditInvoices = await creditQuery.OrderBy(i => i.CreditDueDate).ToListAsync();
        if (minDaysOverdue.HasValue)
            creditInvoices = creditInvoices.Where(i => (asOfDate - i.CreditDueDate!.Value.Date).Days >= minDaysOverdue.Value).ToList();

        var creditRows = creditInvoices.Select(i =>
        {
            var raw = i.RemainingAmount > 0 ? i.RemainingAmount : i.NetAmount;
            var overdue = FinancialReportIqdFoldIn.AmountInBaseIqd(raw, i.Currency, i.FxRate, foldInUsd);
            return new OverdueRow
            {
                InstallmentId = 0,
                InvoiceId = i.Id,
                CustomerName = i.Customer?.Name ?? "\u2014",
                CustomerFileNumber = i.Customer?.FileNumber,
                Phone = i.Customer?.Phone ?? "\u2014",
                PlanNumber = i.InvoiceNumber,
                OverdueAmount = overdue,
                OverdueDays = (asOfDate.Date - i.CreditDueDate!.Value.Date).Days,
                LastPaymentDate = null,
                DueDate = i.CreditDueDate!.Value
            };
        }).ToList();

        rows.AddRange(creditRows);
        rows = rows.OrderByDescending(r => r.OverdueDays).ToList();

        var topCustomers = rows.GroupBy(r => r.CustomerName)
            .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(r => r.OverdueAmount) })
            .OrderByDescending(x => x.Amount).Take(10).ToList();

        var buckets = new List<NameAmountPoint>
        {
            new() { Name = "1-30 \u064a\u0648\u0645", Amount = rows.Where(r => r.OverdueDays is >= 1 and <= 30).Sum(r => r.OverdueAmount) },
            new() { Name = "31-60 \u064a\u0648\u0645", Amount = rows.Where(r => r.OverdueDays is >= 31 and <= 60).Sum(r => r.OverdueAmount) },
            new() { Name = "61-90 \u064a\u0648\u0645", Amount = rows.Where(r => r.OverdueDays is >= 61 and <= 90).Sum(r => r.OverdueAmount) },
            new() { Name = "+90 \u064a\u0648\u0645", Amount = rows.Where(r => r.OverdueDays > 90).Sum(r => r.OverdueAmount) }
        };

        return new OverdueResult
        {
            OverdueCustomerCount = rows.Select(r => r.CustomerName).Distinct().Count(),
            TotalOverdueAmount = rows.Sum(r => r.OverdueAmount),
            TopOverdueCustomer = topCustomers.FirstOrDefault()?.Name ?? "\u2014",
            AverageOverdueDays = rows.Count > 0 ? (int)rows.Average(r => r.OverdueDays) : 0,
            Rows = rows, TopCustomersChart = topCustomers, OverdueBucketChart = buckets
        };
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // CUSTOMER STATEMENT
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<CustomerStatementResult> GetCustomerStatementAsync(int customerId, DateTime? from = null, DateTime? to = null)
    {
        var context = _db;
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
        if (customer is null) return new CustomerStatementResult { CustomerName = "\u2014" };

        await EnsureDebtReceiptsAppliedAsync(context, customerId);

        var invQ = context.Invoices.AsNoTracking()
            .Where(i => i.CustomerId == customerId &&
                        (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
                        (i.PaymentMethod == PaymentMethod.Credit || i.PaymentMethod == PaymentMethod.Installment));
        if (from.HasValue) invQ = invQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) invQ = invQ.Where(i => i.Date < EndOfDay(to));
        var invoices = await invQ.OrderBy(i => i.Date).ToListAsync();

        var vQ = context.Vouchers.AsNoTracking()
            .Where(v => v.CustomerId == customerId &&
                        (v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt));
        if (from.HasValue) vQ = vQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) vQ = vQ.Where(v => v.Date < EndOfDay(to));
        var vouchers = await vQ.OrderBy(v => v.Date).ToListAsync();

        var planIds = await context.InstallmentPlans.AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .Select(p => p.Id)
            .ToListAsync();

        var installmentPayments = new List<CloudInstallment>();
        if (planIds.Count > 0)
        {
            var instQ = context.Installments.AsNoTracking()
                .Include(i => i.InstallmentPlan!).ThenInclude(p => p.Invoice)
                .Where(i => planIds.Contains(i.InstallmentPlanId) && i.PaidAmount > 0);
            if (from.HasValue) instQ = instQ.Where(i => (i.PaymentDate ?? i.DueDate) >= from.Value);
            if (to.HasValue) instQ = instQ.Where(i => (i.PaymentDate ?? i.DueDate) < EndOfDay(to));
            installmentPayments = await instQ.OrderBy(i => i.PaymentDate).ToListAsync();
        }

        var allCreditRows = await context.Invoices.AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.PaymentMethod == PaymentMethod.Credit)
            .Select(i => new { i.Currency, i.RemainingAmount })
            .ToListAsync();
        var allReceiptRows = await context.Vouchers.AsNoTracking()
            .Where(v => v.CustomerId == customerId &&
                        v.VoucherType == VoucherType.Receipt &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .Select(v => new { v.Currency, v.Amount })
            .ToListAsync();
        var allUnappliedDebtRows = await context.Vouchers.AsNoTracking()
            .Where(v => v.CustomerId == customerId &&
                        v.VoucherType == VoucherType.DebtReceipt &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .Select(v => new { v.Currency, v.Amount })
            .ToListAsync();

        var installmentRemainingByCurrency = planIds.Count == 0
            ? new List<(AccountingCurrency Currency, decimal Amount)>()
            : (await context.Installments.AsNoTracking()
                .Where(i => planIds.Contains(i.InstallmentPlanId) && i.Status != InstallmentStatus.Paid)
                .Select(i => new { InvoiceCurrency = i.InstallmentPlan!.Invoice!.Currency, i.RemainingAmount })
                .ToListAsync())
                .Select(x => (Currency: x.InvoiceCurrency, Amount: x.RemainingAmount))
                .ToList();

        var dualBalance = CustomerBalanceHelper.ComputeOutstandingBalances(
            allCreditRows.Select(r => (r.Currency, r.RemainingAmount)),
            installmentRemainingByCurrency,
            allUnappliedDebtRows.Select(r => (r.Currency, r.Amount)),
            allReceiptRows.Select(r => (r.Currency, r.Amount)));

        var unpaidInstallmentIqd = installmentRemainingByCurrency
            .Where(x => x.Currency == AccountingCurrency.IQD)
            .Sum(x => x.Amount);
        var unpaidInstallmentUsd = installmentRemainingByCurrency
            .Where(x => x.Currency == AccountingCurrency.USD)
            .Sum(x => x.Amount);

        var (ledgerRows, _) = CustomerBalanceHelper.BuildDualCustomerStatementLedger(
            invoices.Select(i => new CustomerBalanceInvoiceRow
            {
                Id = i.Id,
                Date = i.Date,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceType = i.InvoiceType,
                PaymentMethod = i.PaymentMethod,
                NetAmount = i.NetAmount,
                PaidAmount = i.PaidAmount,
                RemainingAmount = i.RemainingAmount,
                Currency = i.Currency
            }),
            vouchers.Select(v => new CustomerBalanceVoucherRow
            {
                Id = v.Id,
                Date = v.Date,
                VoucherNumber = v.VoucherNumber,
                VoucherType = v.VoucherType,
                Amount = v.Amount,
                Notes = v.Notes,
                Currency = v.Currency
            }),
            installmentPayments.Select(i => new CustomerBalanceInstallmentPaymentRow
            {
                Id = i.Id,
                Date = i.PaymentDate ?? i.DueDate,
                PaidAmount = i.PaidAmount,
                Currency = i.InstallmentPlan?.Invoice?.Currency ?? AccountingCurrency.IQD
            }),
            new DualCurrencyBalance(unpaidInstallmentIqd, unpaidInstallmentUsd));

        var rows = ledgerRows.Select(r => new CustomerStatementRow
        {
            Date = r.Date,
            Description = r.Description,
            Debit = r.Debit,
            Credit = r.Credit,
            RunningBalance = r.RunningBalance,
            Currency = r.Currency,
            SourceKind = r.SourceKind,
            DocumentId = r.DocumentId
        }).ToList();

        return new CustomerStatementResult
        {
            CustomerName = customer.Name,
            CustomerFileNumber = customer.FileNumber,
            TotalDebit = rows.Where(r => r.Currency == AccountingCurrency.IQD).Sum(r => r.Debit),
            TotalCredit = rows.Where(r => r.Currency == AccountingCurrency.IQD).Sum(r => r.Credit),
            TotalDebitUsd = rows.Where(r => r.Currency == AccountingCurrency.USD).Sum(r => r.Debit),
            TotalCreditUsd = rows.Where(r => r.Currency == AccountingCurrency.USD).Sum(r => r.Credit),
            Balance = dualBalance.Iqd,
            BalanceUsd = dualBalance.Usd,
            TransactionCount = rows.Count,
            Rows = rows
        };
    }

    private static async Task EnsureDebtReceiptsAppliedAsync(CloudDbContext context, int customerId)
    {
        var pending = await context.Vouchers
            .Where(v => v.CustomerId == customerId &&
                        v.VoucherType == VoucherType.DebtReceipt &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .OrderBy(v => v.Date)
            .ThenBy(v => v.Id)
            .ToListAsync();

        if (pending.Count == 0)
            return;

        var creditInvoices = await context.Invoices
            .Where(i => i.CustomerId == customerId &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.RemainingAmount > 0)
            .OrderBy(i => i.Date)
            .ThenBy(i => i.Id)
            .ToListAsync();

        foreach (var voucher in pending)
        {
            var snapshot = creditInvoices
                .Select(i => (i.Id, i.Date, i.NetAmount, i.PaidAmount, i.RemainingAmount, i.Currency))
                .ToList();
            var updates = CustomerBalanceHelper.AllocateToCreditInvoices(
                snapshot, voucher.Amount, voucher.Currency);
            foreach (var u in updates)
            {
                var inv = creditInvoices.First(i => i.Id == u.Id);
                inv.PaidAmount = u.PaidAmount;
                inv.RemainingAmount = u.RemainingAmount;
                inv.IsCreditPaid = u.IsCreditPaid;
                inv.UpdatedAt = DateTime.UtcNow;
                inv.UpdatedBy = "balance-repair";
            }

            voucher.Notes = CustomerBalanceHelper.MarkDebtReceiptApplied(voucher.Notes);
            voucher.UpdatedAt = DateTime.UtcNow;
            voucher.UpdatedBy = "balance-repair";
        }

        await context.SaveChangesAsync();
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // SUPPLIER STATEMENT
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<SupplierStatementResult> GetSupplierStatementAsync(int supplierId, DateTime? from = null, DateTime? to = null)
    {
        var context = _db;
        var supplier = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId);
        if (supplier is null) return new SupplierStatementResult { SupplierName = "\u2014" };

        await EnsureSupplierPaymentsAppliedAsync(context, supplierId);

        var rows = new List<SupplierStatementRow>();
        foreach (var currency in new[] { AccountingCurrency.IQD, AccountingCurrency.USD })
        {
            var currencyRows = new List<SupplierStatementRow>();
            var invQ = context.Invoices
                .Where(i => i.SupplierId == supplierId &&
                            i.InvoiceType == InvoiceType.Purchase &&
                            i.PaymentMethod == PaymentMethod.Credit &&
                            i.Currency == currency);
            if (from.HasValue) invQ = invQ.Where(i => i.Date >= from.Value);
            if (to.HasValue) invQ = invQ.Where(i => i.Date < EndOfDay(to));
            foreach (var inv in await invQ.OrderBy(i => i.Date).ToListAsync())
            {
                currencyRows.Add(new SupplierStatementRow
                {
                    Date = inv.Date,
                    Description = $"فاتورة مشتريات {inv.InvoiceNumber}",
                    Credit = inv.NetAmount,
                    Currency = currency
                });
                if (inv.PaidAmount > 0)
                {
                    currencyRows.Add(new SupplierStatementRow
                    {
                        Date = inv.Date,
                        Description = $"تسديد فاتورة مشتريات آجلة {inv.InvoiceNumber}",
                        Debit = inv.PaidAmount,
                        Currency = currency
                    });
                }
            }

            var returnQ = context.Invoices
                .Where(i => i.SupplierId == supplierId &&
                            i.InvoiceType == InvoiceType.PurchaseReturn &&
                            i.PaymentMethod == PaymentMethod.Credit &&
                            i.Currency == currency &&
                            i.RemainingAmount > 0);
            if (from.HasValue) returnQ = returnQ.Where(i => i.Date >= from.Value);
            if (to.HasValue) returnQ = returnQ.Where(i => i.Date < EndOfDay(to));
            foreach (var inv in await returnQ.OrderBy(i => i.Date).ToListAsync())
            {
                currencyRows.Add(new SupplierStatementRow
                {
                    Date = inv.Date,
                    Description = $"رصيد دائن مرتجع مشتريات {inv.InvoiceNumber}",
                    Debit = inv.RemainingAmount,
                    Currency = currency
                });
            }

            var vQ = context.Vouchers.Where(v => v.SupplierId == supplierId &&
                                                v.VoucherType == VoucherType.Payment &&
                                                v.Currency == currency &&
                                                (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker)));
            if (from.HasValue) vQ = vQ.Where(v => v.Date >= from.Value);
            if (to.HasValue) vQ = vQ.Where(v => v.Date < EndOfDay(to));
            foreach (var v in await vQ.OrderBy(v => v.Date).ToListAsync())
            {
                currencyRows.Add(new SupplierStatementRow
                {
                    Date = v.Date,
                    Description = $"سند صرف {v.VoucherNumber}",
                    Debit = v.Amount,
                    Currency = currency
                });
            }

            currencyRows = currencyRows.OrderBy(r => r.Date).ToList();
            decimal bal = 0;
            foreach (var r in currencyRows)
            {
                bal += r.Credit - r.Debit;
                r.RunningBalance = bal;
            }
            rows.AddRange(currencyRows);
        }

        rows = rows.OrderBy(r => r.Date).ThenBy(r => r.Currency).ToList();
        var iqdRows = rows.Where(r => r.Currency == AccountingCurrency.IQD).ToList();
        var usdRows = rows.Where(r => r.Currency == AccountingCurrency.USD).ToList();

        var creditRemainingIqd = await context.Invoices.AsNoTracking()
            .Where(i => i.SupplierId == supplierId &&
                        i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Currency == AccountingCurrency.IQD &&
                        i.RemainingAmount > 0)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;
        var returnCreditsIqd = await context.Invoices.AsNoTracking()
            .Where(i => i.SupplierId == supplierId &&
                        i.InvoiceType == InvoiceType.PurchaseReturn &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Currency == AccountingCurrency.IQD &&
                        i.RemainingAmount > 0)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;
        var unappliedIqd = await context.Vouchers.AsNoTracking()
            .Where(v => v.SupplierId == supplierId &&
                        v.VoucherType == VoucherType.Payment &&
                        v.Currency == AccountingCurrency.IQD &&
                        !v.InvoiceId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0m;
        var creditRemainingUsd = await context.Invoices.AsNoTracking()
            .Where(i => i.SupplierId == supplierId &&
                        i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Currency == AccountingCurrency.USD &&
                        i.RemainingAmount > 0)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;
        var returnCreditsUsd = await context.Invoices.AsNoTracking()
            .Where(i => i.SupplierId == supplierId &&
                        i.InvoiceType == InvoiceType.PurchaseReturn &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Currency == AccountingCurrency.USD &&
                        i.RemainingAmount > 0)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;
        var unappliedUsd = await context.Vouchers.AsNoTracking()
            .Where(v => v.SupplierId == supplierId &&
                        v.VoucherType == VoucherType.Payment &&
                        v.Currency == AccountingCurrency.USD &&
                        !v.InvoiceId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0m;

        var balanceIqd = SupplierBalanceHelper.ComputeOutstandingPayables(
            creditRemainingIqd, unappliedIqd + returnCreditsIqd);
        var balanceUsd = SupplierBalanceHelper.ComputeOutstandingPayables(
            creditRemainingUsd, unappliedUsd + returnCreditsUsd);

        return new SupplierStatementResult
        {
            SupplierName = supplier.Name,
            TotalDebit = iqdRows.Sum(r => r.Debit),
            TotalCredit = iqdRows.Sum(r => r.Credit),
            Balance = balanceIqd,
            BalanceUsd = balanceUsd,
            InvoiceCount = rows.Count(r => r.Credit > 0),
            Rows = rows
        };
    }

    private static async Task EnsureSupplierPaymentsAppliedAsync(CloudDbContext context, int supplierId)
    {
        var pending = await context.Vouchers
            .Where(v => v.SupplierId == supplierId &&
                        v.VoucherType == VoucherType.Payment &&
                        !v.InvoiceId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker)))
            .OrderBy(v => v.Date)
            .ThenBy(v => v.Id)
            .ToListAsync();

        if (pending.Count == 0)
            return;

        var creditInvoices = await context.Invoices
            .Where(i => i.SupplierId == supplierId &&
                        i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.RemainingAmount > 0)
            .OrderBy(i => i.Date)
            .ThenBy(i => i.Id)
            .ToListAsync();

        foreach (var voucher in pending)
        {
            var snapshot = creditInvoices
                .Select(i => (i.Id, i.Date, i.NetAmount, i.PaidAmount, i.RemainingAmount, i.Currency))
                .ToList();
            var updates = SupplierBalanceHelper.AllocateToPurchaseInvoices(
                snapshot, voucher.Amount, voucher.Currency);
            if (updates.Count == 0)
                continue;

            foreach (var u in updates)
            {
                var inv = creditInvoices.First(i => i.Id == u.Id);
                inv.PaidAmount = u.PaidAmount;
                inv.RemainingAmount = u.RemainingAmount;
                inv.IsCreditPaid = u.IsCreditPaid;
                inv.UpdatedAt = DateTime.UtcNow;
                inv.UpdatedBy = "balance-repair";
            }

            var appliedTotal = updates.Sum(u =>
            {
                var before = snapshot.First(s => s.Id == u.Id);
                return before.RemainingAmount - u.RemainingAmount;
            });

            if (appliedTotal >= voucher.Amount - 0.001m)
            {
                voucher.Notes = SupplierBalanceHelper.MarkPaymentApplied(voucher.Notes);
                voucher.UpdatedAt = DateTime.UtcNow;
                voucher.UpdatedBy = "balance-repair";
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<InvestorStatementResult> GetInvestorStatementAsync(int investorId, DateTime? from = null, DateTime? to = null)
    {
        var context = _db;
        var investor = await context.Investors.FirstOrDefaultAsync(i => i.Id == investorId);
        if (investor is null) return new InvestorStatementResult { InvestorName = "\u2014" };

        var rows = new List<InvestorStatementRow>();

        if (investor.OpeningBalance > 0 && (!from.HasValue || from.Value <= investor.CreatedAt))
        {
            rows.Add(new InvestorStatementRow
            {
                Date = investor.CreatedAt,
                Description = "رصيد افتتاحي",
                Credit = investor.OpeningBalance
            });
        }

        var txQ = context.InvestorTransactions.Where(t => t.InvestorId == investorId);
        if (from.HasValue) txQ = txQ.Where(t => t.Date >= from.Value);
        if (to.HasValue) txQ = txQ.Where(t => t.Date < EndOfDay(to));
        foreach (var tx in await txQ.OrderBy(t => t.Date).ToListAsync())
        {
            rows.Add(new InvestorStatementRow
            {
                Date = tx.Date,
                Description = tx.Type == InvestorTransactionType.Deposit ? "إيداع" : "سحب",
                Credit = tx.Type == InvestorTransactionType.Deposit ? tx.Amount : 0,
                Debit = tx.Type == InvestorTransactionType.Withdrawal ? tx.Amount : 0
            });
        }

        var distQ = from d in context.ProfitDistributionDetails
                    join p in context.ProfitDistributions on d.ProfitDistributionId equals p.Id
                    where d.InvestorId == investorId
                    select new { Detail = d, DistributionDate = p.Date };
        if (from.HasValue) distQ = distQ.Where(x => x.DistributionDate >= from.Value);
        if (to.HasValue) distQ = distQ.Where(x => x.DistributionDate < EndOfDay(to));
        foreach (var dist in await distQ.OrderBy(x => x.DistributionDate).ToListAsync())
        {
            rows.Add(new InvestorStatementRow
            {
                Date = dist.DistributionDate,
                Description = "توزيع أرباح",
                Credit = dist.Detail.Amount
            });
        }

        rows = rows.OrderBy(r => r.Date).ToList();
        decimal balance = 0;
        foreach (var r in rows)
        {
            balance += r.Credit - r.Debit;
            r.RunningBalance = balance;
        }

        return new InvestorStatementResult
        {
            InvestorName = investor.Name,
            TotalDebit = rows.Sum(r => r.Debit),
            TotalCredit = rows.Sum(r => r.Credit),
            Balance = balance,
            TransactionCount = rows.Count,
            Rows = rows
        };
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // EXPENSES
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<ExpensesReportResult> GetExpensesReportAsync(DateTime? from, DateTime? to, int? expenseTypeId, int? cashBoxId)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var reportCurrency = AccountingCurrency.IQD;
        if (cashBoxId.HasValue)
        {
            reportCurrency = await context.CashBoxes.AsNoTracking()
                .Where(c => c.Id == cashBoxId.Value)
                .Select(c => c.Currency)
                .FirstOrDefaultAsync();
            foldInUsd = false;
        }

        var query = context.Expenses.Include(e => e.ExpenseType).Include(e => e.CashBox).AsQueryable();
        if (cashBoxId.HasValue || !foldInUsd)
            query = query.Where(e => e.Currency == reportCurrency);
        if (from.HasValue) query = query.Where(e => e.Date >= from.Value);
        if (to.HasValue) query = query.Where(e => e.Date < EndOfDay(to));
        if (expenseTypeId.HasValue) query = query.Where(e => e.ExpenseTypeId == expenseTypeId.Value);
        if (cashBoxId.HasValue) query = query.Where(e => e.CashBoxId == cashBoxId.Value);

        var expenses = await query.OrderByDescending(e => e.Date).ToListAsync();
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        decimal AmountOf(CloudExpense e) =>
            FinancialReportIqdFoldIn.AmountInBaseIqd(e.Amount, e.Currency, e.FxRate, foldInUsd);

        return new ExpensesReportResult
        {
            TotalExpenses = expenses.Sum(AmountOf),
            TodayExpenses = expenses.Where(e => e.Date.Date == today).Sum(AmountOf),
            MonthExpenses = expenses.Where(e => e.Date >= monthStart).Sum(AmountOf),
            TopExpenseType = expenses.GroupBy(e => e.ExpenseType?.Name ?? "\u0623\u062e\u0631\u0649")
                .OrderByDescending(g => g.Sum(AmountOf)).FirstOrDefault()?.Key ?? "\u2014",
            Currency = reportCurrency,
            Rows = expenses.Select(e => new ExpenseReportRow
            {
                Date = e.Date, ExpenseTypeName = e.ExpenseType?.Name ?? "\u2014",
                Amount = AmountOf(e), CashBoxName = e.CashBox?.Name ?? "\u2014",
                Notes = e.Notes ?? "", CreatedBy = e.CreatedBy ?? "\u2014"
            }).ToList(),
            ByTypeChart = expenses.GroupBy(e => e.ExpenseType?.Name ?? "\u0623\u062e\u0631\u0649")
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(AmountOf) }).ToList(),
            DailyChart = expenses.GroupBy(e => e.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(AmountOf) })
                .OrderBy(d => d.Date).ToList()
        };
    }

    // INCOME & EXPENSE
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<IncomeExpenseResult> GetIncomeExpenseReportAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var rows = new List<IncomeExpenseRow>();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));

        var salesQ = foldInUsd
            ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(context.Invoices, context.InstallmentPlans)
            : CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans, AccountingCurrency.IQD);
        var expQ = foldInUsd
            ? context.Expenses.Include(e => e.ExpenseType).AsQueryable()
            : context.Expenses.Include(e => e.ExpenseType).Where(e => e.Currency == AccountingCurrency.IQD);
        if (from.HasValue) { salesQ = salesQ.Where(i => i.Date >= from.Value); expQ = expQ.Where(e => e.Date >= from.Value); }
        if (to.HasValue) { salesQ = salesQ.Where(i => i.Date < EndOfDay(to)); expQ = expQ.Where(e => e.Date < EndOfDay(to)); }

        var salesRows = await salesQ
            .Select(i => new { i.InvoiceType, i.NetAmount, i.Currency, i.FxRate })
            .ToListAsync();
        var totalSales = FinancialReportIqdFoldIn.SumSignedSalesInBaseIqd(
            salesRows.Select(r => (r.InvoiceType, r.NetAmount, r.Currency, r.FxRate)), foldInUsd);

        var instQ = context.Installments.Where(i => i.PaidAmount > 0
                        && (foldInUsd || i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD));
        if (from.HasValue) instQ = instQ.Where(i => i.PaymentDate >= from.Value);
        if (to.HasValue) instQ = instQ.Where(i => i.PaymentDate < EndOfDay(to));
        var instRows = await instQ
            .Select(i => new
            {
                i.PaidAmount,
                Currency = i.InstallmentPlan!.Invoice!.Currency,
                FxRate = i.InstallmentPlan!.Invoice!.FxRate
            })
            .ToListAsync();
        var instCollections = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            instRows.Select(r => (r.PaidAmount, r.Currency, r.FxRate)), foldInUsd);

        var recQ = context.Vouchers.Where(v =>
            (v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt)
            && (foldInUsd || v.Currency == AccountingCurrency.IQD));
        if (from.HasValue) recQ = recQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) recQ = recQ.Where(v => v.Date < EndOfDay(to));
        var receiptRows = await recQ
            .Select(v => new { v.Amount, v.Currency, v.FxRate })
            .ToListAsync();
        var receipts = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            receiptRows.Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);

        var totalIncome = totalSales + instCollections + receipts;
        rows.Add(new IncomeExpenseRow { Section = "\u0627\u0644\u0648\u0627\u0631\u062f\u0627\u062a", Type = "\u0625\u064a\u0631\u0627\u062f", Description = "\u0645\u0628\u064a\u0639\u0627\u062a \u0646\u0642\u062f\u064a\u0629", Amount = totalSales });
        rows.Add(new IncomeExpenseRow { Section = "\u0627\u0644\u0648\u0627\u0631\u062f\u0627\u062a", Type = "\u0625\u064a\u0631\u0627\u062f", Description = "\u062a\u062d\u0635\u064a\u0644 \u0623\u0642\u0633\u0627\u0637", Amount = instCollections });
        rows.Add(new IncomeExpenseRow { Section = "\u0627\u0644\u0648\u0627\u0631\u062f\u0627\u062a", Type = "\u0625\u064a\u0631\u0627\u062f", Description = "\u0633\u0646\u062f\u0627\u062a \u0642\u0628\u0636", Amount = receipts });

        var expenses = await expQ.ToListAsync();
        var grouped = expenses.GroupBy(e => e.ExpenseType?.Name ?? "\u0623\u062e\u0631\u0649");
        foreach (var g in grouped)
        {
            var amount = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                g.Select(e => (e.Amount, e.Currency, e.FxRate)), foldInUsd);
            rows.Add(new IncomeExpenseRow { Section = "\u0627\u0644\u0645\u0635\u0631\u0648\u0641\u0627\u062a", Type = "\u0645\u0635\u0631\u0648\u0641", Description = g.Key, Amount = amount });
        }

        var totalExpenses = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            expenses.Select(e => (e.Amount, e.Currency, e.FxRate)), foldInUsd);
        var net = totalIncome - totalExpenses;

        var f = from?.Date ?? DateTime.Today.AddMonths(-12);
        var tExclusive = EndOfDay(to ?? DateTime.Today) ?? DateTime.Today.AddDays(1);
        var tInclusive = tExclusive.AddTicks(-1);

        var monthlySalesQ = foldInUsd
            ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(context.Invoices, context.InstallmentPlans)
            : CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans, AccountingCurrency.IQD);
        var monthlyIncomeRaw = await monthlySalesQ
            .Where(i => i.Date >= f && i.Date < tExclusive)
            .Select(i => new { i.Date, i.InvoiceType, i.NetAmount, i.Currency, i.FxRate })
            .ToListAsync();
        var monthlyByMonth = monthlyIncomeRaw
            .GroupBy(i => new { i.Date.Year, i.Date.Month })
            .ToDictionary(
                g => (g.Key.Year, g.Key.Month),
                g => FinancialReportIqdFoldIn.SumSignedSalesInBaseIqd(
                    g.Select(i => (i.InvoiceType, i.NetAmount, i.Currency, i.FxRate)), foldInUsd));

        var chartInstQ = context.Installments.Where(i => i.PaidAmount > 0
                        && i.PaymentDate != null
                        && i.PaymentDate >= f && i.PaymentDate < tExclusive
                        && (foldInUsd || i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD));
        var chartInstRows = await chartInstQ
            .Select(i => new
            {
                Date = i.PaymentDate!.Value,
                i.PaidAmount,
                Currency = i.InstallmentPlan!.Invoice!.Currency,
                FxRate = i.InstallmentPlan!.Invoice!.FxRate
            })
            .ToListAsync();
        foreach (var g in chartInstRows.GroupBy(i => new { i.Date.Year, i.Date.Month }))
        {
            var key = (g.Key.Year, g.Key.Month);
            var add = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                g.Select(x => (x.PaidAmount, x.Currency, x.FxRate)), foldInUsd);
            monthlyByMonth[key] = monthlyByMonth.GetValueOrDefault(key) + add;
        }

        var chartRecQ = context.Vouchers.Where(v =>
            (v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt)
            && v.Date >= f && v.Date < tExclusive
            && (foldInUsd || v.Currency == AccountingCurrency.IQD));
        var chartRecRows = await chartRecQ
            .Select(v => new { v.Date, v.Amount, v.Currency, v.FxRate })
            .ToListAsync();
        foreach (var g in chartRecRows.GroupBy(v => new { v.Date.Year, v.Date.Month }))
        {
            var key = (g.Key.Year, g.Key.Month);
            var add = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                g.Select(x => (x.Amount, x.Currency, x.FxRate)), foldInUsd);
            monthlyByMonth[key] = monthlyByMonth.GetValueOrDefault(key) + add;
        }

        var monthlyExpQ = foldInUsd
            ? context.Expenses.Where(e => e.Date >= f && e.Date < tExclusive)
            : context.Expenses.Where(e => e.Currency == AccountingCurrency.IQD && e.Date >= f && e.Date < tExclusive);
        var monthlyExpRaw = await monthlyExpQ
            .Select(e => new { e.Date, e.Amount, e.Currency, e.FxRate })
            .ToListAsync();
        var monthlyExp = monthlyExpRaw
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .ToDictionary(
                g => (g.Key.Year, g.Key.Month),
                g => FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                    g.Select(e => (e.Amount, e.Currency, e.FxRate)), foldInUsd));

        var monthlyChart = new List<MonthlyIncomeExpensePoint>();
        for (var d = new DateTime(f.Year, f.Month, 1); d <= tInclusive; d = d.AddMonths(1))
        {
            var key = (d.Year, d.Month);
            monthlyChart.Add(new MonthlyIncomeExpensePoint
            {
                Month = $"{d.Year}/{d.Month:D2}",
                Income = monthlyByMonth.GetValueOrDefault(key),
                Expense = monthlyExp.GetValueOrDefault(key)
            });
        }

        return new IncomeExpenseResult
        {
            TotalIncome = totalIncome, TotalExpenses = totalExpenses, NetResult = net,
            ExpenseRate = totalIncome > 0 ? Math.Round(totalExpenses / totalIncome * 100, 1) : 0,
            Rows = rows, MonthlyChart = monthlyChart
        };
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // WAREHOUSE
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<List<WarehouseStockRow>> GetWarehouseReportAsync(int? warehouseId, bool includeZero = false)
    {
        var context = _db;
        var query = context.WarehouseStocks.Include(ws => ws.Product).Include(ws => ws.Warehouse).AsQueryable();
        if (warehouseId.HasValue) query = query.Where(ws => ws.WarehouseId == warehouseId.Value);
        if (!includeZero) query = query.Where(ws => ws.Quantity > 0);

        var stocks = await query.OrderBy(ws => ws.Warehouse!.Name).ThenBy(ws => ws.Product!.Name).ToListAsync();
        var productIds = stocks.Select(s => s.ProductId).Distinct().ToList();
        var purchasesByProduct = await CloudProductCostHelper.GetPurchaseItemsByProductAsync(context, productIds);
        var result = new List<WarehouseStockRow>();
        foreach (var s in stocks)
        {
            var pi = purchasesByProduct.GetValueOrDefault(s.ProductId) ?? [];
            var avgCost = CloudProductCostHelper.ComputeAverageUnitCost(pi, s.OpeningQuantity, s.UnitCost);

            result.Add(new WarehouseStockRow
            {
                ProductName = s.Product?.Name ?? "\u2014", WarehouseName = s.Warehouse?.Name ?? "\u2014",
                Quantity = s.Quantity, AverageCost = Math.Round(avgCost, 0), TotalValue = Math.Round(s.Quantity * avgCost, 0)
            });
        }
        return result;
    }

    public Task<DamageInvoicesReportResult> GetDamageInvoicesReportAsync(
        DateTime? from, DateTime? to, int? warehouseId) =>
        Task.FromResult(new DamageInvoicesReportResult());

    public Task<PackagingStockReportResult> GetPackagingStockReportAsync(int? warehouseId, int? productId) =>
        Task.FromResult(new PackagingStockReportResult());

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // INVESTORS
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<InvestorsReportResult> GetInvestorsReportAsync(int? investorId, DateTime? from, DateTime? to)
    {
        var context = _db;
        var invQ = context.Investors.Include(i => i.Transactions).AsQueryable();
        if (investorId.HasValue) invQ = invQ.Where(i => i.Id == investorId.Value);
        var investors = await invQ.ToListAsync();

        var distQ = context.ProfitDistributionDetails.Include(d => d.Investor).AsQueryable();
        var dists = await distQ.ToListAsync();
        var lastDist = await context.ProfitDistributions.OrderByDescending(p => p.Date).FirstOrDefaultAsync();

        var rows = investors.Select(inv =>
        {
            var deposits = inv.Transactions.Where(t => t.Type == InvestorTransactionType.Deposit).Sum(t => t.Amount);
            var withdrawals = inv.Transactions.Where(t => t.Type == InvestorTransactionType.Withdrawal).Sum(t => t.Amount);
            var distributed = dists.Where(d => d.InvestorId == inv.Id).Sum(d => d.Amount);
            var lastW = inv.Transactions.Where(t => t.Type == InvestorTransactionType.Withdrawal).OrderByDescending(t => t.Date).FirstOrDefault();

            return new InvestorReportRow
            {
                InvestorName = inv.Name, TotalDeposit = deposits,
                EligibleDeposit = inv.TotalDeposit, ProfitPercentage = inv.ProfitPercentage,
                TotalDistributed = distributed, LastWithdrawal = lastW?.Date
            };
        }).ToList();

        return new InvestorsReportResult
        {
            TotalInvestments = rows.Sum(r => r.TotalDeposit),
            TotalDistributed = rows.Sum(r => r.TotalDistributed),
            InvestorCount = rows.Count,
            LastDistributionDate = lastDist?.Date,
            Rows = rows,
            SharesChart = rows.Select(r => new NameAmountPoint { Name = r.InvestorName, Amount = r.TotalDeposit }).ToList(),
            DistributedChart = rows.Select(r => new NameAmountPoint { Name = r.InvestorName, Amount = r.TotalDistributed }).ToList()
        };
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // CASH FLOW
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<CashFlowResult> GetCashFlowReportAsync(int? cashBoxId, DateTime? from, DateTime? to)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var rows = new List<CashFlowRow>();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));

        AccountingCurrency? currencyFilter = null;
        var reportCurrency = AccountingCurrency.IQD;
        if (cashBoxId.HasValue)
        {
            reportCurrency = await context.CashBoxes.AsNoTracking()
                .Where(c => c.Id == cashBoxId.Value)
                .Select(c => c.Currency)
                .FirstOrDefaultAsync();
            if (!foldInUsd)
                currencyFilter = reportCurrency;
        }
        else if (!foldInUsd)
        {
            currencyFilter = AccountingCurrency.IQD;
        }

        if (foldInUsd)
            reportCurrency = AccountingCurrency.IQD;

        decimal ToIqd(decimal amount, AccountingCurrency currency, decimal fxRate) =>
            foldInUsd
                ? FinancialReportIqdFoldIn.AmountInBaseIqd(amount, currency, fxRate, true)
                : amount;

        var salesQ = context.Invoices.Include(i => i.CashBox)
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment)
                        && i.PaymentMethod == PaymentMethod.Cash);
        if (currencyFilter.HasValue) salesQ = salesQ.Where(i => i.Currency == currencyFilter.Value);
        if (cashBoxId.HasValue) salesQ = salesQ.Where(i => i.CashBoxId == cashBoxId.Value);
        if (from.HasValue) salesQ = salesQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) salesQ = salesQ.Where(i => i.Date < EndOfDay(to));
        foreach (var inv in await salesQ.ToListAsync())
            rows.Add(new CashFlowRow
            {
                Date = inv.Date,
                Type = "\u0645\u0628\u064a\u0639\u0627\u062a",
                Description = $"\u0641\u0627\u062a\u0648\u0631\u0629 {inv.InvoiceNumber}",
                Incoming = ToIqd(inv.NetAmount, inv.Currency, inv.FxRate),
                AccountName = inv.CashBox?.Name ?? "\u2014"
            });

        var saleReturnQ = context.Invoices.Include(i => i.CashBox)
            .Where(i => i.InvoiceType == InvoiceType.SaleReturn
                        && i.PaymentMethod == PaymentMethod.Cash);
        if (currencyFilter.HasValue) saleReturnQ = saleReturnQ.Where(i => i.Currency == currencyFilter.Value);
        if (cashBoxId.HasValue) saleReturnQ = saleReturnQ.Where(i => i.CashBoxId == cashBoxId.Value);
        if (from.HasValue) saleReturnQ = saleReturnQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) saleReturnQ = saleReturnQ.Where(i => i.Date < EndOfDay(to));
        foreach (var inv in await saleReturnQ.ToListAsync())
            rows.Add(new CashFlowRow
            {
                Date = inv.Date,
                Type = "مرتجع مبيعات",
                Description = $"فاتورة {inv.InvoiceNumber}",
                Outgoing = ToIqd(inv.NetAmount, inv.Currency, inv.FxRate),
                AccountName = inv.CashBox?.Name ?? "\u2014"
            });

        var purchQ = context.Invoices.Include(i => i.CashBox)
            .Where(i => i.InvoiceType == InvoiceType.Purchase
                        && i.PaymentMethod == PaymentMethod.Cash);
        if (currencyFilter.HasValue) purchQ = purchQ.Where(i => i.Currency == currencyFilter.Value);
        if (cashBoxId.HasValue) purchQ = purchQ.Where(i => i.CashBoxId == cashBoxId.Value);
        if (from.HasValue) purchQ = purchQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) purchQ = purchQ.Where(i => i.Date < EndOfDay(to));
        foreach (var inv in await purchQ.ToListAsync())
            rows.Add(new CashFlowRow
            {
                Date = inv.Date,
                Type = "\u0645\u0634\u062a\u0631\u064a\u0627\u062a",
                Description = $"\u0641\u0627\u062a\u0648\u0631\u0629 {inv.InvoiceNumber}",
                Outgoing = ToIqd(inv.NetAmount, inv.Currency, inv.FxRate),
                AccountName = inv.CashBox?.Name ?? "\u2014"
            });

        var purchaseReturnQ = context.Invoices.Include(i => i.CashBox)
            .Where(i => i.InvoiceType == InvoiceType.PurchaseReturn
                        && i.PaymentMethod == PaymentMethod.Cash);
        if (currencyFilter.HasValue) purchaseReturnQ = purchaseReturnQ.Where(i => i.Currency == currencyFilter.Value);
        if (cashBoxId.HasValue) purchaseReturnQ = purchaseReturnQ.Where(i => i.CashBoxId == cashBoxId.Value);
        if (from.HasValue) purchaseReturnQ = purchaseReturnQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) purchaseReturnQ = purchaseReturnQ.Where(i => i.Date < EndOfDay(to));
        foreach (var inv in await purchaseReturnQ.ToListAsync())
            rows.Add(new CashFlowRow
            {
                Date = inv.Date,
                Type = "مرتجع مشتريات",
                Description = $"فاتورة {inv.InvoiceNumber}",
                Incoming = ToIqd(inv.NetAmount, inv.Currency, inv.FxRate),
                AccountName = inv.CashBox?.Name ?? "\u2014"
            });

        var vouchQ = context.Vouchers.Include(v => v.CashBox).AsQueryable();
        if (currencyFilter.HasValue) vouchQ = vouchQ.Where(v => v.Currency == currencyFilter.Value);
        if (cashBoxId.HasValue) vouchQ = vouchQ.Where(v => v.CashBoxId == cashBoxId.Value);
        if (from.HasValue) vouchQ = vouchQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) vouchQ = vouchQ.Where(v => v.Date < EndOfDay(to));
        foreach (var v in await vouchQ.ToListAsync())
        {
            bool isIncoming = v.VoucherType is VoucherType.Receipt or VoucherType.DebtReceipt or VoucherType.InvestorDeposit or VoucherType.BankReceipt;
            var amount = ToIqd(v.Amount, v.Currency, v.FxRate);
            rows.Add(new CashFlowRow
            {
                Date = v.Date,
                Type = v.VoucherType switch { VoucherType.Receipt => "\u0633\u0646\u062f \u0642\u0628\u0636", VoucherType.Payment => "\u0633\u0646\u062f \u0635\u0631\u0641", VoucherType.DebtReceipt => "\u062a\u0633\u062f\u064a\u062f \u062f\u064a\u0646", VoucherType.BankReceipt => "\u0642\u0628\u0636 \u0628\u0646\u0643\u064a", VoucherType.InvestorDeposit => "\u0625\u064a\u062f\u0627\u0639 \u0645\u0633\u062a\u062b\u0645\u0631", VoucherType.InvestorWithdrawal => "\u0633\u062d\u0628 \u0645\u0633\u062a\u062b\u0645\u0631", _ => "\u0633\u0646\u062f" },
                Description = v.VoucherNumber,
                Incoming = isIncoming ? amount : 0,
                Outgoing = !isIncoming ? amount : 0,
                AccountName = v.CashBox?.Name ?? "\u2014"
            });
        }

        var expQ = context.Expenses.Include(e => e.CashBox).Include(e => e.ExpenseType).AsQueryable();
        if (currencyFilter.HasValue) expQ = expQ.Where(e => e.Currency == currencyFilter.Value);
        if (cashBoxId.HasValue) expQ = expQ.Where(e => e.CashBoxId == cashBoxId.Value);
        if (from.HasValue) expQ = expQ.Where(e => e.Date >= from.Value);
        if (to.HasValue) expQ = expQ.Where(e => e.Date < EndOfDay(to));
        foreach (var e in await expQ.ToListAsync())
            rows.Add(new CashFlowRow
            {
                Date = e.Date,
                Type = "\u0645\u0635\u0631\u0648\u0641",
                Description = e.ExpenseType?.Name ?? "\u0645\u0635\u0631\u0648\u0641",
                Outgoing = ToIqd(e.Amount, e.Currency, e.FxRate),
                AccountName = e.CashBox?.Name ?? "\u2014"
            });

        rows = rows.OrderBy(r => r.Date).ToList();
        decimal bal = 0;
        foreach (var r in rows) { bal += r.Incoming - r.Outgoing; r.Balance = bal; }

        var totalIn = rows.Sum(r => r.Incoming);
        var totalOut = rows.Sum(r => r.Outgoing);

        decimal currentBal;
        if (cashBoxId.HasValue)
        {
            var box = await context.CashBoxes.AsNoTracking()
                .Where(c => c.Id == cashBoxId.Value)
                .Select(c => new { c.Balance, c.Currency })
                .FirstOrDefaultAsync();
            if (box is null)
                currentBal = 0;
            else if (foldInUsd)
            {
                var asOf = await GetAsOfUsdToIqdRateAsync(to ?? DateTime.Today);
                currentBal = FinancialReportIqdFoldIn.CashOrBankInBaseIqd(
                    box.Balance, box.Currency, asOf, true);
            }
            else
                currentBal = box.Balance;
        }
        else if (foldInUsd)
        {
            var asOf = await GetAsOfUsdToIqdRateAsync(to ?? DateTime.Today);
            var boxes = await context.CashBoxes.AsNoTracking()
                .Select(c => new { c.Balance, c.Currency })
                .ToListAsync();
            currentBal = FinancialReportIqdFoldIn.SumCashOrBankInBaseIqd(
                boxes.Select(c => (c.Balance, c.Currency)), asOf, true);
        }
        else
        {
            currentBal = await context.CashBoxes
                .Where(c => c.Currency == AccountingCurrency.IQD)
                .SumAsync(c => (decimal?)c.Balance) ?? 0;
        }

        return new CashFlowResult
        {
            TotalIncoming = totalIn, TotalOutgoing = totalOut,
            NetFlow = totalIn - totalOut, CurrentBalance = currentBal,
            Currency = reportCurrency,
            Rows = rows,
            DailyIncomingChart = rows.Where(r => r.Incoming > 0).GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(r => r.Incoming) }).OrderBy(d => d.Date).ToList(),
            DailyOutgoingChart = rows.Where(r => r.Outgoing > 0).GroupBy(r => r.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(r => r.Outgoing) }).OrderBy(d => d.Date).ToList()
        };
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // BALANCE SHEET
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<BalanceSheetResult> GetBalanceSheetAsync(DateTime date)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var endOfDay = date.Date.AddDays(1).AddTicks(-1);
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var asOfUsdToIqd = foldInUsd
            ? await GetAsOfUsdToIqdRateAsync(date)
            : 0m;

        decimal capital = await context.CapitalEntries
            .Where(c => c.Type == CapitalEntryType.Initial && c.Date <= endOfDay)
            .SumAsync(c => c.Amount);

        decimal adjustments = await context.CapitalEntries
            .Where(c => c.Type == CapitalEntryType.Adjustment && c.Date <= endOfDay)
            .SumAsync(c => c.Amount);

        decimal profitOpening = await CloudProductCostHelper.GetProfitOpeningBalanceAsync(context, endOfDay);

        var salesBaseQ = foldInUsd
            ? CloudInvoiceFilters.ForProfitAndSalesTotalsAll(context.Invoices, context.InstallmentPlans)
            : CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans, AccountingCurrency.IQD);
        var salesRows = await salesBaseQ.Where(i => i.Date <= endOfDay)
            .Select(i => new { i.InvoiceType, i.NetAmount, i.Currency, i.FxRate })
            .ToListAsync();
        decimal totalSales = FinancialReportIqdFoldIn.SumSignedSalesInBaseIqd(
            salesRows.Select(r => (r.InvoiceType, r.NetAmount, r.Currency, r.FxRate)), foldInUsd);
        decimal costOfSales = await CalculateCogsAsync(context, null, endOfDay.AddTicks(1));
        var expenseQ = foldInUsd
            ? context.Expenses.Where(e => e.Date <= endOfDay)
            : context.Expenses.Where(e => e.Currency == AccountingCurrency.IQD && e.Date <= endOfDay);
        var expenseRows = await expenseQ.Select(e => new { e.Amount, e.Currency, e.FxRate }).ToListAsync();
        decimal totalExpenses = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            expenseRows.Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);

        decimal salesProfit = totalSales - costOfSales;
        decimal accumulatedProfits = profitOpening + salesProfit - totalExpenses;
        decimal equityTotal = capital + adjustments + accumulatedProfits;

        var supplierCreditQ = context.Invoices.Where(i =>
            i.SupplierId != null &&
            i.InvoiceType == InvoiceType.Purchase &&
            i.PaymentMethod == PaymentMethod.Credit &&
            i.Date <= endOfDay &&
            (foldInUsd || i.Currency == AccountingCurrency.IQD));
        var supplierCreditRemaining = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await supplierCreditQ.Select(i => new { i.RemainingAmount, i.Currency, i.FxRate }).ToListAsync())
            .Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);
        var supplierReturnQ = context.Invoices.Where(i =>
            i.SupplierId != null &&
            i.InvoiceType == InvoiceType.PurchaseReturn &&
            i.PaymentMethod == PaymentMethod.Credit &&
            i.Date <= endOfDay &&
            (foldInUsd || i.Currency == AccountingCurrency.IQD));
        var supplierReturnCredits = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await supplierReturnQ.Select(i => new { i.RemainingAmount, i.Currency, i.FxRate }).ToListAsync())
            .Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);
        var unappliedSupplierQ = context.Vouchers.Where(v =>
            v.SupplierId != null &&
            v.VoucherType == VoucherType.Payment &&
            v.Date <= endOfDay &&
            !v.InvoiceId.HasValue &&
            (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker)) &&
            (foldInUsd || v.Currency == AccountingCurrency.IQD));
        var unappliedSupplierPayments = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await unappliedSupplierQ.Select(v => new { v.Amount, v.Currency, v.FxRate }).ToListAsync())
            .Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);
        decimal supplierPayables = SupplierBalanceHelper.ComputeOutstandingPayables(
            supplierCreditRemaining, unappliedSupplierPayments + supplierReturnCredits);

        decimal investorDeposits = await context.InvestorTransactions
            .Where(t => t.Type == InvestorTransactionType.Deposit && t.Date <= endOfDay)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        decimal investorWithdrawals = await context.InvestorTransactions
            .Where(t => t.Type == InvestorTransactionType.Withdrawal && t.Date <= endOfDay)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        investorDeposits = Math.Max(0, investorDeposits - investorWithdrawals);

        decimal liabilitiesTotal = supplierPayables + investorDeposits;
        decimal equityAndLiabilitiesTotal = equityTotal + liabilitiesTotal;

        var cashBoxes = await context.CashBoxes.ToListAsync();
        var cashBoxRows = cashBoxes
            .Where(c => foldInUsd || c.Currency == AccountingCurrency.IQD)
            .Select(c => new BalanceSheetCashBoxRow
            {
                Name = c.Name,
                Balance = FinancialReportIqdFoldIn.CashOrBankInBaseIqd(
                    c.Balance, c.Currency, asOfUsdToIqd, foldInUsd)
            }).ToList();
        decimal cashBoxesTotal = cashBoxRows.Sum(c => c.Balance);

        var banks = await context.BankAccounts.ToListAsync();
        var bankRows = banks
            .Where(b => foldInUsd || b.Currency == AccountingCurrency.IQD)
            .Select(b => new BalanceSheetBankRow
            {
                Name = b.Name,
                Balance = FinancialReportIqdFoldIn.CashOrBankInBaseIqd(
                    b.Balance, b.Currency, asOfUsdToIqd, foldInUsd)
            }).ToList();
        decimal banksTotal = bankRows.Sum(b => b.Balance);

        var creditInvQ = context.Invoices.Where(i =>
            i.CustomerId != null &&
            (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
            i.PaymentMethod == PaymentMethod.Credit &&
            i.Date <= endOfDay &&
            (foldInUsd || i.Currency == AccountingCurrency.IQD));
        var creditRemaining = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await creditInvQ.Select(i => new { i.RemainingAmount, i.Currency, i.FxRate }).ToListAsync())
            .Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);
        var saleReturnQ = context.Invoices.Where(i =>
            i.CustomerId != null &&
            i.InvoiceType == InvoiceType.SaleReturn &&
            i.PaymentMethod == PaymentMethod.Credit &&
            i.Date <= endOfDay &&
            (foldInUsd || i.Currency == AccountingCurrency.IQD));
        var saleReturnCredits = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await saleReturnQ.Select(i => new { i.RemainingAmount, i.Currency, i.FxRate }).ToListAsync())
            .Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);
        var unappliedDebtQ = context.Vouchers.Where(v =>
            v.CustomerId != null &&
            v.VoucherType == VoucherType.DebtReceipt &&
            v.Date <= endOfDay &&
            !v.InvoiceId.HasValue &&
            !v.InstallmentId.HasValue &&
            (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)) &&
            (foldInUsd || v.Currency == AccountingCurrency.IQD));
        var unappliedDebt = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await unappliedDebtQ.Select(v => new { v.Amount, v.Currency, v.FxRate }).ToListAsync())
            .Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);
        var unappliedReceiptsQ = context.Vouchers.Where(v =>
            v.CustomerId != null &&
            v.VoucherType == VoucherType.Receipt &&
            v.Date <= endOfDay &&
            !v.InvoiceId.HasValue &&
            !v.InstallmentId.HasValue &&
            (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)) &&
            (foldInUsd || v.Currency == AccountingCurrency.IQD));
        var unappliedReceipts = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            (await unappliedReceiptsQ.Select(v => new { v.Amount, v.Currency, v.FxRate }).ToListAsync())
            .Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);
        decimal customerDebts = CustomerBalanceHelper.ComputeOutstandingBalance(
            creditRemaining, 0, unappliedDebt, unappliedReceipts + saleReturnCredits);

        var stocks = await context.WarehouseStocks
            .Include(ws => ws.Product)
            .ToListAsync();
        var inventoryProductIds = stocks.Where(s => s.Quantity > 0).Select(s => s.ProductId).Distinct().ToList();
        var inventoryPurchases = await CloudProductCostHelper.GetPurchaseItemsByProductAsync(context, inventoryProductIds);
        decimal inventoryValue = 0;
        foreach (var s in stocks)
        {
            if (s.Quantity <= 0) continue;

            var purchaseItems = inventoryPurchases.GetValueOrDefault(s.ProductId) ?? [];
            var avgCost = CloudProductCostHelper.ComputeAverageUnitCost(purchaseItems, s.OpeningQuantity, s.UnitCost);
            if (avgCost > 0)
                inventoryValue += Math.Round(s.Quantity * avgCost, 0);
        }

        var installmentRows = await context.Installments
            .Where(i => i.RemainingAmount > 0 &&
                        (foldInUsd || i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD))
            .Select(i => new
            {
                i.RemainingAmount,
                Currency = i.InstallmentPlan!.Invoice!.Currency,
                FxRate = i.InstallmentPlan!.Invoice!.FxRate
            })
            .ToListAsync();
        decimal installmentReceivables = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
            installmentRows.Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);

        decimal assetsTotal = cashBoxesTotal + banksTotal + customerDebts + inventoryValue + installmentReceivables;
        decimal difference = equityAndLiabilitiesTotal - assetsTotal;

        return new BalanceSheetResult
        {
            Capital = capital,
            Adjustments = adjustments,
            AccumulatedProfits = accumulatedProfits,
            EquityTotal = equityTotal,
            ProfitOpeningBalance = profitOpening,
            SalesTotal = totalSales,
            CostOfSales = costOfSales,
            SalesProfit = salesProfit,
            ExpensesTotal = totalExpenses,
            SupplierPayables = supplierPayables,
            InvestorDeposits = investorDeposits,
            LiabilitiesTotal = liabilitiesTotal,
            EquityAndLiabilitiesTotal = equityAndLiabilitiesTotal,
            CashBoxesTotal = cashBoxesTotal,
            CashBoxes = cashBoxRows,
            BanksTotal = banksTotal,
            Banks = bankRows,
            CashBoxesTotalUsd = 0,
            BanksTotalUsd = 0,
            CustomerDebtsUsd = 0,
            SupplierPayablesUsd = 0,
            CustomerDebts = customerDebts,
            InventoryValue = inventoryValue,
            InstallmentReceivables = installmentReceivables,
            AssetsTotal = assetsTotal,
            Difference = difference,
            IsBalanced = Math.Abs(difference) < 1m
        };
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // TOP PRODUCTS & PROFIT MARGIN
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<TopProductsReportResult> GetTopProductsReportAsync(
        DateTime? from, DateTime? to, int? warehouseId, int topCount = 30, bool sortByRevenueDescending = true)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var query = context.InvoiceItems
            .Include(ii => ii.Product)
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && ii.Invoice != null
                         && (ii.Invoice.InvoiceType == InvoiceType.Sale || ii.Invoice.InvoiceType == InvoiceType.Installment || ii.Invoice.InvoiceType == InvoiceType.SaleReturn)
                         && (foldInUsd || ii.Invoice.Currency == AccountingCurrency.IQD));

        if (from.HasValue) query = query.Where(ii => ii.Invoice!.Date >= from.Value);
        if (to.HasValue) query = query.Where(ii => ii.Invoice!.Date < EndOfDay(to));
        if (warehouseId.HasValue) query = query.Where(ii => ii.Invoice!.WarehouseId == warehouseId.Value);

        var items = await query.ToListAsync();
        decimal LineRevenue(CloudInvoiceItem x)
        {
            var signed = InvoiceFilters.SignedSaleLineAmount(x.Invoice!.InvoiceType, x.TotalPrice);
            return FinancialReportIqdFoldIn.AmountInBaseIqd(signed, x.Invoice.Currency, x.Invoice.FxRate, foldInUsd);
        }

        var grouped = items
            .GroupBy(ii => ii.ProductId!.Value)
            .Select(g => new TopProductRow
            {
                ProductId = g.Key,
                ProductName = g.First().Product?.Name ?? g.First().ItemName,
                QuantitySold = g.Sum(x => InvoiceFilters.SignedSaleLineQuantity(x.Invoice!.InvoiceType, x.Quantity)),
                Revenue = g.Sum(LineRevenue)
            })
            .ToList();

        var ordered = sortByRevenueDescending
            ? grouped.OrderByDescending(r => r.Revenue).ThenByDescending(r => r.QuantitySold)
            : grouped.OrderBy(r => r.Revenue).ThenBy(r => r.QuantitySold);

        var top = ordered.Take(Math.Max(1, topCount)).ToList();
        var totalRevenue = grouped.Sum(r => r.Revenue);

        for (var i = 0; i < top.Count; i++)
        {
            top[i].Rank = i + 1;
            top[i].SharePercent = totalRevenue > 0 ? Math.Round(top[i].Revenue / totalRevenue * 100, 1) : 0;
        }

        return new TopProductsReportResult
        {
            TotalRevenue = totalRevenue,
            TotalQuantity = grouped.Sum(r => r.QuantitySold),
            ProductCount = grouped.Count,
            Rows = top,
            Chart = top.Take(10).Select(r => new NameAmountPoint { Name = r.ProductName, Amount = r.Revenue }).ToList()
        };
    }

    public async Task<ProductProfitMarginReportResult> GetProductProfitMarginReportAsync(
        DateTime? from, DateTime? to, int? warehouseId)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var query = context.InvoiceItems
            .Include(ii => ii.Product)
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && ii.Invoice != null
                         && (ii.Invoice.InvoiceType == InvoiceType.Sale || ii.Invoice.InvoiceType == InvoiceType.Installment || ii.Invoice.InvoiceType == InvoiceType.SaleReturn)
                         && (foldInUsd || ii.Invoice.Currency == AccountingCurrency.IQD));

        if (from.HasValue) query = query.Where(ii => ii.Invoice!.Date >= from.Value);
        if (to.HasValue) query = query.Where(ii => ii.Invoice!.Date < EndOfDay(to));
        if (warehouseId.HasValue) query = query.Where(ii => ii.Invoice!.WarehouseId == warehouseId.Value);

        var soldItems = await query.ToListAsync();
        if (soldItems.Count == 0)
        {
            return new ProductProfitMarginReportResult();
        }

        var productIds = soldItems.Select(ii => ii.ProductId!.Value).Distinct().ToList();
        var stocks = await context.WarehouseStocks
            .Where(ws => productIds.Contains(ws.ProductId))
            .ToListAsync();
        var purchasesByProduct = await CloudProductCostHelper.GetPurchaseItemsByProductAsync(context, productIds);

        decimal LineRevenue(CloudInvoiceItem x)
        {
            var signed = InvoiceFilters.SignedSaleLineAmount(x.Invoice!.InvoiceType, x.TotalPrice);
            return FinancialReportIqdFoldIn.AmountInBaseIqd(signed, x.Invoice.Currency, x.Invoice.FxRate, foldInUsd);
        }

        var rows = new List<ProductProfitMarginRow>();
        foreach (var g in soldItems.GroupBy(ii => ii.ProductId!.Value))
        {
            var revenue = g.Sum(LineRevenue);
            var qty = g.Sum(x => InvoiceFilters.SignedSaleLineQuantity(x.Invoice!.InvoiceType, x.Quantity));
            var avgCost = CloudProductCostHelper.ComputeAverageUnitCostForProduct(
                purchasesByProduct.GetValueOrDefault(g.Key) ?? [], stocks, g.Key);
            var cost = Math.Round(qty * avgCost, 0);
            var profit = revenue - cost;
            rows.Add(new ProductProfitMarginRow
            {
                ProductId = g.Key,
                ProductName = g.First().Product?.Name ?? g.First().ItemName,
                QuantitySold = qty,
                Revenue = revenue,
                Cost = cost,
                GrossProfit = profit,
                MarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100, 1) : 0
            });
        }

        rows = rows.OrderByDescending(r => r.GrossProfit).ToList();
        var totalRevenue = rows.Sum(r => r.Revenue);
        var totalCost = rows.Sum(r => r.Cost);
        var totalProfit = rows.Sum(r => r.GrossProfit);

        return new ProductProfitMarginReportResult
        {
            TotalRevenue = totalRevenue,
            TotalCost = totalCost,
            TotalGrossProfit = totalProfit,
            AverageMarginPercent = totalRevenue > 0 ? Math.Round(totalProfit / totalRevenue * 100, 1) : 0,
            Rows = rows
        };
    }

    public async Task<MaterialNetProfitReportResult> GetMaterialNetProfitReportAsync(
        DateTime? from, DateTime? to, int? warehouseId, bool ascending = false, int? topN = null)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var query = context.InvoiceItems
            .Include(ii => ii.Product)
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && ii.Invoice != null
                         && (ii.Invoice.InvoiceType == InvoiceType.Sale || ii.Invoice.InvoiceType == InvoiceType.Installment || ii.Invoice.InvoiceType == InvoiceType.SaleReturn)
                         && (foldInUsd || ii.Invoice.Currency == AccountingCurrency.IQD));

        if (from.HasValue) query = query.Where(ii => ii.Invoice!.Date >= from.Value);
        if (to.HasValue) query = query.Where(ii => ii.Invoice!.Date < EndOfDay(to));
        if (warehouseId.HasValue) query = query.Where(ii => ii.Invoice!.WarehouseId == warehouseId.Value);

        var soldItems = await query.ToListAsync();
        if (soldItems.Count == 0)
            return new MaterialNetProfitReportResult();

        var productIds = soldItems.Select(ii => ii.ProductId!.Value).Distinct().ToList();
        var stockQuery = context.WarehouseStocks.Where(ws => productIds.Contains(ws.ProductId));
        if (warehouseId.HasValue)
            stockQuery = stockQuery.Where(ws => ws.WarehouseId == warehouseId.Value);
        var stocks = await stockQuery.ToListAsync();
        var allStocksForCost = await context.WarehouseStocks
            .Where(ws => productIds.Contains(ws.ProductId))
            .ToListAsync();
        var purchasesByProduct = await CloudProductCostHelper.GetPurchaseItemsByProductAsync(context, productIds);

        decimal LineRevenue(CloudInvoiceItem x)
        {
            var signed = InvoiceFilters.SignedSaleLineAmount(x.Invoice!.InvoiceType, x.TotalPrice);
            return FinancialReportIqdFoldIn.AmountInBaseIqd(signed, x.Invoice.Currency, x.Invoice.FxRate, foldInUsd);
        }

        var rows = new List<MaterialNetProfitRow>();
        foreach (var g in soldItems.GroupBy(ii => ii.ProductId!.Value))
        {
            var revenue = g.Sum(LineRevenue);
            var qty = g.Sum(x => InvoiceFilters.SignedSaleLineQuantity(x.Invoice!.InvoiceType, x.Quantity));
            var avgCost = CloudProductCostHelper.ComputeAverageUnitCostForProduct(
                purchasesByProduct.GetValueOrDefault(g.Key) ?? [], allStocksForCost, g.Key);
            var cost = Math.Round(qty * avgCost, 0);
            var profit = revenue - cost;
            var stockQty = stocks.Where(s => s.ProductId == g.Key).Sum(s => s.Quantity);
            var stockValue = Math.Round(stockQty * avgCost, 0);

            rows.Add(new MaterialNetProfitRow
            {
                ProductId = g.Key,
                ProductName = g.First().Product?.Name ?? g.First().ItemName,
                StockQuantity = stockQty,
                StockValue = stockValue,
                QuantitySold = qty,
                Revenue = revenue,
                Cost = cost,
                NetProfit = profit,
                MarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100, 1) : 0
            });
        }

        rows = ascending
            ? rows.OrderBy(r => r.NetProfit).ToList()
            : rows.OrderByDescending(r => r.NetProfit).ToList();

        if (topN is > 0)
            rows = rows.Take(topN.Value).ToList();

        for (var i = 0; i < rows.Count; i++)
            rows[i].Rank = i + 1;

        var totalRevenue = rows.Sum(r => r.Revenue);
        var totalProfit = rows.Sum(r => r.NetProfit);

        return new MaterialNetProfitReportResult
        {
            TotalNetProfit = totalProfit,
            TotalStockValue = rows.Sum(r => r.StockValue),
            ProductCount = rows.Count,
            AverageMarginPercent = totalRevenue > 0 ? Math.Round(totalProfit / totalRevenue * 100, 1) : 0,
            Rows = rows,
            Chart = rows.Take(10)
                .Select(r => new NameAmountPoint { Name = TruncateChartLabel(r.ProductName), Amount = r.NetProfit })
                .ToList()
        };
    }

    public async Task<CustomerNetProfitReportResult> GetCustomerNetProfitReportAsync(
        DateTime? from, DateTime? to, bool ascending = false, int? topN = null)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));

        var query = context.InvoiceItems
            .Include(ii => ii.Product)
            .Include(ii => ii.Invoice!).ThenInclude(i => i.Customer)
            .Where(ii => ii.Invoice != null
                         && ii.Invoice.CustomerId != null
                         && (foldInUsd || ii.Invoice.Currency == AccountingCurrency.IQD)
                         && (ii.Invoice.InvoiceType == InvoiceType.Sale || ii.Invoice.InvoiceType == InvoiceType.Installment || ii.Invoice.InvoiceType == InvoiceType.SaleReturn));

        if (from.HasValue) query = query.Where(ii => ii.Invoice!.Date >= from.Value);
        if (to.HasValue) query = query.Where(ii => ii.Invoice!.Date < EndOfDay(to));

        var soldItems = await query.ToListAsync();
        if (soldItems.Count == 0)
            return new CustomerNetProfitReportResult();

        var productIds = soldItems
            .Where(ii => ii.ProductId != null)
            .Select(ii => ii.ProductId!.Value)
            .Distinct()
            .ToList();
        var stocks = productIds.Count > 0
            ? await context.WarehouseStocks.Where(ws => productIds.Contains(ws.ProductId)).ToListAsync()
            : [];
        var purchasesByProduct = productIds.Count > 0
            ? await CloudProductCostHelper.GetPurchaseItemsByProductAsync(context, productIds)
            : new Dictionary<int, List<CloudInvoiceItem>>();

        var avgCostByProduct = productIds.ToDictionary(
            id => id,
            id => CloudProductCostHelper.ComputeAverageUnitCostForProduct(
                purchasesByProduct.GetValueOrDefault(id) ?? [], stocks, id));

        var customerIds = soldItems.Select(ii => ii.Invoice!.CustomerId!.Value).Distinct().ToList();
        var outstandingRows = await context.Invoices.AsNoTracking()
            .Where(i => i.CustomerId != null
                        && customerIds.Contains(i.CustomerId.Value)
                        && i.RemainingAmount > 0
                        && (foldInUsd || i.Currency == AccountingCurrency.IQD))
            .Select(i => new { CustomerId = i.CustomerId!.Value, i.RemainingAmount, i.Currency, i.FxRate })
            .ToListAsync();
        var outstandingByCustomer = outstandingRows
            .GroupBy(x => x.CustomerId)
            .ToDictionary(
                g => g.Key,
                g => FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                    g.Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd));

        decimal LineSalesInBaseIqd(CloudInvoiceItem x)
        {
            var signed = InvoiceFilters.SignedSaleLineAmount(x.Invoice!.InvoiceType, x.TotalPrice);
            return FinancialReportIqdFoldIn.AmountInBaseIqd(signed, x.Invoice.Currency, x.Invoice.FxRate, foldInUsd);
        }

        var rows = new List<CustomerNetProfitRow>();
        foreach (var g in soldItems.GroupBy(ii => ii.Invoice!.CustomerId!.Value))
        {
            var invoiceIds = g.Select(x => x.InvoiceId).Distinct().Count();
            var sales = g.Sum(LineSalesInBaseIqd);
            var cost = Math.Round(g.Sum(x =>
            {
                if (x.ProductId is null) return 0m;
                var avg = avgCostByProduct.GetValueOrDefault(x.ProductId.Value);
                return InvoiceFilters.SignedSaleLineQuantity(x.Invoice!.InvoiceType, x.Quantity) * avg;
            }), 0);
            var profit = sales - cost;
            var customer = g.First().Invoice!.Customer;

            rows.Add(new CustomerNetProfitRow
            {
                CustomerId = g.Key,
                CustomerName = customer?.Name ?? "—",
                CustomerFileNumber = customer?.FileNumber,
                Phone = customer?.Phone ?? "—",
                InvoiceCount = invoiceIds,
                SalesAmount = sales,
                Cost = cost,
                NetProfit = profit,
                MarginPercent = sales > 0 ? Math.Round(profit / sales * 100, 1) : 0,
                OutstandingBalance = outstandingByCustomer.GetValueOrDefault(g.Key)
            });
        }

        rows = ascending
            ? rows.OrderBy(r => r.NetProfit).ToList()
            : rows.OrderByDescending(r => r.NetProfit).ToList();

        if (topN is > 0)
            rows = rows.Take(topN.Value).ToList();

        for (var i = 0; i < rows.Count; i++)
            rows[i].Rank = i + 1;

        var totalSales = rows.Sum(r => r.SalesAmount);
        var totalProfit = rows.Sum(r => r.NetProfit);

        return new CustomerNetProfitReportResult
        {
            TotalNetProfit = totalProfit,
            TotalOutstanding = rows.Sum(r => r.OutstandingBalance),
            CustomerCount = rows.Count,
            AverageMarginPercent = totalSales > 0 ? Math.Round(totalProfit / totalSales * 100, 1) : 0,
            Rows = rows,
            Chart = rows.Take(10)
                .Select(r => new NameAmountPoint { Name = TruncateChartLabel(r.CustomerName), Amount = r.NetProfit })
                .ToList()
        };
    }

    public async Task<InstallmentAgingReportResult> GetInstallmentAgingReportAsync(DateTime asOfDate, int? customerId)
    {
        var context = _db;
        var query = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Include(i => i.InstallmentPlan).ThenInclude(p => p!.Invoice)
            .Where(i => i.Status != InstallmentStatus.Paid
                        && i.RemainingAmount > 0);

        if (customerId.HasValue)
            query = query.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);

        var insts = await query.OrderBy(i => i.DueDate).ToListAsync();
        var asOf = asOfDate.Date;

        static string ResolveBucket(DateTime dueDate, DateTime asOfDate)
        {
            if (dueDate.Date >= asOfDate)
                return "غير مستحق";

            var days = (asOfDate - dueDate.Date).Days;
            return days switch
            {
                <= 30 => "1-30 يوم",
                <= 60 => "31-60 يوم",
                <= 90 => "61-90 يوم",
                _ => "+90 يوم"
            };
        }

        var rows = insts.Select(i =>
        {
            var days = i.DueDate.Date < asOf ? (asOf - i.DueDate.Date).Days : 0;
            var currency = i.InstallmentPlan?.Invoice?.Currency ?? AccountingCurrency.IQD;
            return new InstallmentAgingRow
            {
                InstallmentId = i.Id,
                InvoiceId = i.InstallmentPlan?.InvoiceId ?? 0,
                CustomerName = i.InstallmentPlan?.Customer?.Name ?? "\u2014",
                CustomerFileNumber = i.InstallmentPlan?.Customer?.FileNumber,
                Phone = i.InstallmentPlan?.Customer?.Phone ?? "\u2014",
                PlanNumber = i.InstallmentPlanId.ToString(),
                DueDate = i.DueDate,
                Amount = i.Amount,
                RemainingAmount = i.RemainingAmount,
                DaysOverdue = days,
                AgingBucket = ResolveBucket(i.DueDate.Date, asOf),
                Currency = currency
            };
        }).OrderByDescending(r => r.DaysOverdue).ThenBy(r => r.DueDate).ToList();

        var iqdRows = rows.Where(r => r.Currency == AccountingCurrency.IQD).ToList();
        var usdRows = rows.Where(r => r.Currency == AccountingCurrency.USD).ToList();
        var bucketOrder = new[] { "غير مستحق", "1-30 يوم", "31-60 يوم", "61-90 يوم", "+90 يوم" };
        var buckets = bucketOrder.Select(name => new InstallmentAgingBucketSummary
        {
            BucketName = name,
            Count = rows.Count(r => r.AgingBucket == name),
            Amount = iqdRows.Where(r => r.AgingBucket == name).Sum(r => r.RemainingAmount),
            AmountUsd = usdRows.Where(r => r.AgingBucket == name).Sum(r => r.RemainingAmount)
        }).ToList();

        return new InstallmentAgingReportResult
        {
            TotalOutstanding = iqdRows.Sum(r => r.RemainingAmount),
            TotalOutstandingUsd = usdRows.Sum(r => r.RemainingAmount),
            InstallmentCount = rows.Count,
            CustomerCount = rows.Select(r => r.CustomerName).Distinct().Count(),
            Buckets = buckets,
            Rows = rows
        };
    }

    public async Task<CustomersOverviewReportResult> GetCustomersOverviewReportAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var customers = await context.Customers.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var rows = new List<CustomerOverviewRow>();

        foreach (var customer in customers)
        {
            var invQ = context.Invoices.AsNoTracking()
                .Where(i => i.CustomerId == customer.Id &&
                            (foldInUsd || i.Currency == AccountingCurrency.IQD) &&
                            (i.InvoiceType == InvoiceType.Sale
                             || i.InvoiceType == InvoiceType.Installment
                             || i.InvoiceType == InvoiceType.SaleReturn));
            if (from.HasValue) invQ = invQ.Where(i => i.Date >= from.Value);
            if (to.HasValue) invQ = invQ.Where(i => i.Date < EndOfDay(to));

            var invoices = await invQ
                .Select(i => new { i.InvoiceType, i.NetAmount, i.Currency, i.FxRate })
                .ToListAsync();
            var invoiceCount = invoices.Count;
            var salesAmount = FinancialReportIqdFoldIn.SumSignedSalesInBaseIqd(
                invoices.Select(i => (i.InvoiceType, i.NetAmount, i.Currency, i.FxRate)), foldInUsd);

            var voucherQ = context.Vouchers.AsNoTracking()
                .Where(v => v.CustomerId == customer.Id &&
                            (foldInUsd || v.Currency == AccountingCurrency.IQD) &&
                            (v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt));
            if (from.HasValue) voucherQ = voucherQ.Where(v => v.Date >= from.Value);
            if (to.HasValue) voucherQ = voucherQ.Where(v => v.Date < EndOfDay(to));
            var voucherRows = await voucherQ
                .Select(v => new { v.Amount, v.Currency, v.FxRate })
                .ToListAsync();
            var collected = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                voucherRows.Select(v => (v.Amount, v.Currency, v.FxRate)), foldInUsd);

            var planIds = await context.InstallmentPlans.AsNoTracking()
                .Where(p => p.CustomerId == customer.Id)
                .Select(p => p.Id)
                .ToListAsync();
            if (planIds.Count > 0)
            {
                var instQ = context.Installments.AsNoTracking()
                    .Where(i => planIds.Contains(i.InstallmentPlanId)
                                && i.PaidAmount > 0
                                && (foldInUsd || i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD));
                if (from.HasValue) instQ = instQ.Where(i => (i.PaymentDate ?? i.DueDate) >= from.Value);
                if (to.HasValue) instQ = instQ.Where(i => (i.PaymentDate ?? i.DueDate) < EndOfDay(to));
                var instRows = await instQ
                    .Select(i => new
                    {
                        i.PaidAmount,
                        Currency = i.InstallmentPlan!.Invoice!.Currency,
                        FxRate = i.InstallmentPlan!.Invoice!.FxRate
                    })
                    .ToListAsync();
                collected += FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                    instRows.Select(i => (i.PaidAmount, i.Currency, i.FxRate)), foldInUsd);
            }

            var creditInvQ = context.Invoices.AsNoTracking()
                .Where(i => i.CustomerId == customer.Id &&
                            i.PaymentMethod == PaymentMethod.Credit &&
                            (foldInUsd || i.Currency == AccountingCurrency.IQD));
            var outstandingCredit = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                (await creditInvQ.Select(i => new { i.RemainingAmount, i.Currency, i.FxRate }).ToListAsync())
                .Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);

            var outstandingInstallments = 0m;
            if (planIds.Count > 0)
            {
                var outInstQ = context.Installments.AsNoTracking()
                    .Where(i => planIds.Contains(i.InstallmentPlanId) &&
                                i.Status != InstallmentStatus.Paid &&
                                (foldInUsd || i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD));
                outstandingInstallments = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                    (await outInstQ.Select(i => new
                    {
                        i.RemainingAmount,
                        Currency = i.InstallmentPlan!.Invoice!.Currency,
                        FxRate = i.InstallmentPlan!.Invoice!.FxRate
                    }).ToListAsync())
                    .Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);
            }

            var unappliedDebtQ = context.Vouchers.AsNoTracking()
                .Where(v => v.CustomerId == customer.Id &&
                            v.VoucherType == VoucherType.DebtReceipt &&
                            (foldInUsd || v.Currency == AccountingCurrency.IQD) &&
                            !v.InvoiceId.HasValue &&
                            !v.InstallmentId.HasValue &&
                            (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)));
            var unappliedDebt = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                (await unappliedDebtQ.Select(v => new { v.Amount, v.Currency, v.FxRate }).ToListAsync())
                .Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);

            var unappliedReceiptsQ = context.Vouchers.AsNoTracking()
                .Where(v => v.CustomerId == customer.Id &&
                            v.VoucherType == VoucherType.Receipt &&
                            (foldInUsd || v.Currency == AccountingCurrency.IQD) &&
                            !v.InvoiceId.HasValue &&
                            !v.InstallmentId.HasValue &&
                            (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)));
            var unappliedReceipts = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                (await unappliedReceiptsQ.Select(v => new { v.Amount, v.Currency, v.FxRate }).ToListAsync())
                .Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);

            var outstanding = CustomerBalanceHelper.ComputeOutstandingBalance(
                outstandingCredit, outstandingInstallments, unappliedDebt, unappliedReceipts);

            var outstandingUsd = 0m;
            if (!foldInUsd)
            {
                var outstandingCreditUsd = await context.Invoices.AsNoTracking()
                    .Where(i => i.CustomerId == customer.Id &&
                                i.PaymentMethod == PaymentMethod.Credit &&
                                i.Currency == AccountingCurrency.USD)
                    .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;
                var outstandingInstallmentsUsd = 0m;
                if (planIds.Count > 0)
                {
                    outstandingInstallmentsUsd = await context.Installments.AsNoTracking()
                        .Where(i => planIds.Contains(i.InstallmentPlanId) &&
                                    i.Status != InstallmentStatus.Paid &&
                                    i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.USD)
                        .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;
                }
                var unappliedDebtUsd = await context.Vouchers.AsNoTracking()
                    .Where(v => v.CustomerId == customer.Id &&
                                v.VoucherType == VoucherType.DebtReceipt &&
                                v.Currency == AccountingCurrency.USD &&
                                !v.InvoiceId.HasValue &&
                                !v.InstallmentId.HasValue &&
                                (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
                    .SumAsync(v => (decimal?)v.Amount) ?? 0m;
                var unappliedReceiptsUsd = await context.Vouchers.AsNoTracking()
                    .Where(v => v.CustomerId == customer.Id &&
                                v.VoucherType == VoucherType.Receipt &&
                                v.Currency == AccountingCurrency.USD &&
                                !v.InvoiceId.HasValue &&
                                !v.InstallmentId.HasValue &&
                                (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
                    .SumAsync(v => (decimal?)v.Amount) ?? 0m;
                outstandingUsd = CustomerBalanceHelper.ComputeOutstandingBalance(
                    outstandingCreditUsd, outstandingInstallmentsUsd, unappliedDebtUsd, unappliedReceiptsUsd);
            }

            if (invoiceCount == 0 && collected == 0 && outstanding == 0 && outstandingUsd == 0)
                continue;

            rows.Add(new CustomerOverviewRow
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                CustomerFileNumber = customer.FileNumber,
                Phone = customer.Phone ?? "-",
                InvoiceCount = invoiceCount,
                SalesAmount = salesAmount,
                CollectedAmount = collected,
                OutstandingBalance = outstanding,
                OutstandingBalanceUsd = outstandingUsd
            });
        }

        return new CustomersOverviewReportResult
        {
            TotalSales = rows.Sum(r => r.SalesAmount),
            TotalCollected = rows.Sum(r => r.CollectedAmount),
            TotalOutstanding = rows.Sum(r => r.OutstandingBalance),
            TotalOutstandingUsd = rows.Sum(r => r.OutstandingBalanceUsd),
            CustomerCount = rows.Count,
            Rows = rows.OrderByDescending(r => r.OutstandingBalance).ThenByDescending(r => r.SalesAmount).ToList()
        };
    }

    public async Task<SuppliersOverviewReportResult> GetSuppliersOverviewReportAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var tenantId = RequireTenantId();
        var foldInUsd = FinancialReportIqdFoldIn.ShouldFoldUsdIntoIqd(
            await CloudMultiCurrencyFeatureGate.IsEnabledAsync(context, tenantId));
        var suppliers = await context.Suppliers.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        var rows = new List<SupplierOverviewRow>();

        foreach (var supplier in suppliers)
        {
            var invBase = context.Invoices.AsNoTracking().Where(i => i.SupplierId == supplier.Id);
            var invQ = foldInUsd
                ? CloudInvoiceFilters.ForPurchasesTotalsAll(invBase)
                : CloudInvoiceFilters.ForPurchasesTotals(invBase);
            if (from.HasValue) invQ = invQ.Where(i => i.Date >= from.Value);
            if (to.HasValue) invQ = invQ.Where(i => i.Date < EndOfDay(to));

            var invoices = await invQ
                .Select(i => new { i.InvoiceType, i.NetAmount, i.Currency, i.FxRate, i.PaymentMethod })
                .ToListAsync();
            var invoiceCount = invoices.Count;
            var purchaseAmount = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                invoices.Select(i => (InvoiceFilters.SignedNetAmount(i.InvoiceType, i.NetAmount), i.Currency, i.FxRate)),
                foldInUsd);

            var voucherQ = context.Vouchers.AsNoTracking()
                .Where(v => v.SupplierId == supplier.Id
                            && v.VoucherType == VoucherType.Payment
                            && (foldInUsd || v.Currency == AccountingCurrency.IQD));
            if (from.HasValue) voucherQ = voucherQ.Where(v => v.Date >= from.Value);
            if (to.HasValue) voucherQ = voucherQ.Where(v => v.Date < EndOfDay(to));
            var voucherRows = await voucherQ
                .Select(v => new { v.Amount, v.Currency, v.FxRate })
                .ToListAsync();
            var paid = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                voucherRows.Select(v => (v.Amount, v.Currency, v.FxRate)), foldInUsd);

            paid += FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                invoices.Where(i => i.PaymentMethod == PaymentMethod.Cash)
                    .Select(i => (InvoiceFilters.SignedNetAmount(i.InvoiceType, i.NetAmount), i.Currency, i.FxRate)),
                foldInUsd);

            var creditRemaining = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                (await context.Invoices.AsNoTracking()
                    .Where(i => i.SupplierId == supplier.Id &&
                                i.InvoiceType == InvoiceType.Purchase &&
                                i.PaymentMethod == PaymentMethod.Credit &&
                                (foldInUsd || i.Currency == AccountingCurrency.IQD) &&
                                i.RemainingAmount > 0)
                    .Select(i => new { i.RemainingAmount, i.Currency, i.FxRate })
                    .ToListAsync())
                .Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);

            var unappliedPayments = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                (await context.Vouchers.AsNoTracking()
                    .Where(v => v.SupplierId == supplier.Id &&
                                v.VoucherType == VoucherType.Payment &&
                                (foldInUsd || v.Currency == AccountingCurrency.IQD) &&
                                !v.InvoiceId.HasValue &&
                                (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker)))
                    .Select(v => new { v.Amount, v.Currency, v.FxRate })
                    .ToListAsync())
                .Select(r => (r.Amount, r.Currency, r.FxRate)), foldInUsd);

            var returnCredits = FinancialReportIqdFoldIn.SumAmountsInBaseIqd(
                (await context.Invoices.AsNoTracking()
                    .Where(i => i.SupplierId == supplier.Id &&
                                i.InvoiceType == InvoiceType.PurchaseReturn &&
                                i.PaymentMethod == PaymentMethod.Credit &&
                                (foldInUsd || i.Currency == AccountingCurrency.IQD) &&
                                i.RemainingAmount > 0)
                    .Select(i => new { i.RemainingAmount, i.Currency, i.FxRate })
                    .ToListAsync())
                .Select(r => (r.RemainingAmount, r.Currency, r.FxRate)), foldInUsd);

            var outstanding = SupplierBalanceHelper.ComputeOutstandingPayables(
                creditRemaining, unappliedPayments + returnCredits);

            var outstandingUsd = 0m;
            if (!foldInUsd)
            {
                var creditRemainingUsd = await context.Invoices.AsNoTracking()
                    .Where(i => i.SupplierId == supplier.Id &&
                                i.InvoiceType == InvoiceType.Purchase &&
                                i.PaymentMethod == PaymentMethod.Credit &&
                                i.Currency == AccountingCurrency.USD &&
                                i.RemainingAmount > 0)
                    .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;
                var unappliedPaymentsUsd = await context.Vouchers.AsNoTracking()
                    .Where(v => v.SupplierId == supplier.Id &&
                                v.VoucherType == VoucherType.Payment &&
                                v.Currency == AccountingCurrency.USD &&
                                !v.InvoiceId.HasValue &&
                                (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker)))
                    .SumAsync(v => (decimal?)v.Amount) ?? 0m;
                var returnCreditsUsd = await context.Invoices.AsNoTracking()
                    .Where(i => i.SupplierId == supplier.Id &&
                                i.InvoiceType == InvoiceType.PurchaseReturn &&
                                i.PaymentMethod == PaymentMethod.Credit &&
                                i.Currency == AccountingCurrency.USD &&
                                i.RemainingAmount > 0)
                    .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;
                outstandingUsd = SupplierBalanceHelper.ComputeOutstandingPayables(
                    creditRemainingUsd, unappliedPaymentsUsd + returnCreditsUsd);
            }

            if (invoiceCount == 0 && paid == 0 && outstanding == 0 && outstandingUsd == 0)
                continue;

            rows.Add(new SupplierOverviewRow
            {
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                Phone = supplier.Phone ?? "-",
                InvoiceCount = invoiceCount,
                PurchaseAmount = purchaseAmount,
                PaidAmount = paid,
                OutstandingBalance = outstanding,
                OutstandingBalanceUsd = outstandingUsd
            });
        }

        return new SuppliersOverviewReportResult
        {
            TotalPurchases = rows.Sum(r => r.PurchaseAmount),
            TotalPaid = rows.Sum(r => r.PaidAmount),
            TotalOutstanding = rows.Sum(r => r.OutstandingBalance),
            TotalOutstandingUsd = rows.Sum(r => r.OutstandingBalanceUsd),
            SupplierCount = rows.Count,
            Rows = rows.OrderByDescending(r => r.OutstandingBalance).ThenByDescending(r => r.PurchaseAmount).ToList()
        };
    }

    public async Task<ProfitComparisonResult> GetProfitComparisonAsync(DateTime? from, DateTime? to)
    {
        var currentTo = to?.Date ?? DateTime.Today;
        var currentFrom = from?.Date ?? currentTo.AddMonths(-1);
        if (currentFrom > currentTo)
            (currentFrom, currentTo) = (currentTo, currentFrom);

        var spanDays = Math.Max(1, (currentTo - currentFrom).Days + 1);
        var previousTo = currentFrom.AddDays(-1);
        var previousFrom = previousTo.AddDays(-(spanDays - 1));

        var current = await GetProfitReportAsync(currentFrom, currentTo);
        var previous = await GetProfitReportAsync(previousFrom, previousTo);

        return new ProfitComparisonResult
        {
            CurrentFrom = currentFrom,
            CurrentTo = currentTo,
            PreviousFrom = previousFrom,
            PreviousTo = previousTo,
            Current = current,
            Previous = previous,
            SalesChangePercent = PercentChange(previous.TotalSales, current.TotalSales),
            GrossProfitChangePercent = PercentChange(previous.GrossProfit, current.GrossProfit),
            NetProfitChangePercent = PercentChange(previous.NetProfit, current.NetProfit)
        };
    }

    public async Task<ProductMovementReportResult> GetProductMovementReportAsync(
        DateTime? from, DateTime? to, int? warehouseId, int? productId)
    {
        var context = _db;

        var itemsQ = context.InvoiceItems.AsNoTracking()
            .Where(ii => ii.ProductId != null);

        if (productId.HasValue)
            itemsQ = itemsQ.Where(ii => ii.ProductId == productId);

        var query =
            from ii in itemsQ
            join inv in context.Invoices.AsNoTracking() on ii.InvoiceId equals inv.Id
            where inv.InvoiceType == InvoiceType.Purchase
                  || inv.InvoiceType == InvoiceType.PurchaseReturn
                  || inv.InvoiceType == InvoiceType.Sale
                  || inv.InvoiceType == InvoiceType.Installment
                  || inv.InvoiceType == InvoiceType.SaleReturn
            select new { ii, inv };

        if (from.HasValue)
            query = query.Where(x => x.inv.Date >= from.Value);
        if (to.HasValue)
            query = query.Where(x => x.inv.Date < EndOfDay(to));
        if (warehouseId.HasValue)
            query = query.Where(x => x.inv.WarehouseId == warehouseId);

        var raw = await query.ToListAsync();

        var grouped = raw
            .GroupBy(x => x.ii.ProductId!.Value)
            .Select(g =>
            {
                var name = g.Select(x => x.ii.ItemName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? $"#{g.Key}";
                decimal qtyIn = g.Where(x => x.inv.InvoiceType == InvoiceType.Purchase).Sum(x => x.ii.Quantity)
                    - g.Where(x => x.inv.InvoiceType == InvoiceType.PurchaseReturn).Sum(x => Math.Abs(x.ii.Quantity));
                decimal qtyOut = g.Where(x => x.inv.InvoiceType is InvoiceType.Sale or InvoiceType.Installment).Sum(x => x.ii.Quantity)
                    - g.Where(x => x.inv.InvoiceType == InvoiceType.SaleReturn).Sum(x => Math.Abs(x.ii.Quantity));
                return new ProductMovementRow
                {
                    ProductId = g.Key,
                    ProductName = name,
                    QuantityIn = qtyIn,
                    QuantityOut = qtyOut
                };
            })
            .Where(r => r.QuantityIn != 0 || r.QuantityOut != 0)
            .OrderByDescending(r => r.QuantityOut + r.QuantityIn)
            .ToList();

        return new ProductMovementReportResult
        {
            TotalQuantityIn = grouped.Sum(r => r.QuantityIn),
            TotalQuantityOut = grouped.Sum(r => r.QuantityOut),
            ProductCount = grouped.Count,
            Rows = grouped
        };
    }

    public async Task<StockHealthReportResult> GetStockHealthReportAsync(
        int? warehouseId, decimal lowStockThreshold, int deadStockDays, StockHealthFilter filter = StockHealthFilter.All)
    {
        var context = _db;
        var threshold = Math.Max(0, lowStockThreshold);
        var deadDays = Math.Max(1, deadStockDays);
        var asOf = DateTime.Today;
        var deadCutoff = asOf.AddDays(-deadDays);

        var stockQ = context.WarehouseStocks.AsNoTracking()
            .Include(ws => ws.Product)
            .Include(ws => ws.Warehouse)
            .Where(ws => ws.Quantity > 0);
        if (warehouseId.HasValue)
            stockQ = stockQ.Where(ws => ws.WarehouseId == warehouseId.Value);

        var stocks = await stockQ.ToListAsync();
        var stockProductIds = stocks.Select(s => s.ProductId).Distinct().ToList();
        var purchasesByProduct = await CloudProductCostHelper.GetPurchaseItemsByProductAsync(context, stockProductIds);

        var lastSaleByProduct = await (
            from ii in context.InvoiceItems.AsNoTracking()
            join inv in context.Invoices.AsNoTracking() on ii.InvoiceId equals inv.Id
            where ii.ProductId != null &&
                  (inv.InvoiceType == InvoiceType.Sale || inv.InvoiceType == InvoiceType.Installment)
            group inv.Date by ii.ProductId into g
            select new { ProductId = g.Key!.Value, LastSale = g.Max(d => d) }
        ).ToDictionaryAsync(x => x.ProductId, x => x.LastSale);

        var rows = new List<StockHealthRow>();
        foreach (var s in stocks)
        {
            var productId = s.ProductId;
            lastSaleByProduct.TryGetValue(productId, out var lastSale);
            var isDead = !lastSaleByProduct.ContainsKey(productId) || lastSale < deadCutoff;
            var isLow = s.Quantity <= threshold;

            if (!isDead && !isLow)
                continue;

            var status = isDead ? StockHealthStatus.DeadStock : StockHealthStatus.LowStock;
            if (filter == StockHealthFilter.LowStockOnly && status != StockHealthStatus.LowStock)
                continue;
            if (filter == StockHealthFilter.DeadStockOnly && status != StockHealthStatus.DeadStock)
                continue;

            var pi = purchasesByProduct.GetValueOrDefault(productId) ?? [];
            var avgCost = CloudProductCostHelper.ComputeAverageUnitCost(pi, s.OpeningQuantity, s.UnitCost);
            var stockValue = Math.Round(s.Quantity * avgCost, 0);

            int? daysSince = lastSaleByProduct.ContainsKey(productId)
                ? Math.Max(0, (asOf - lastSale).Days)
                : null;

            rows.Add(new StockHealthRow
            {
                ProductId = productId,
                ProductName = s.Product?.Name ?? "â€”",
                WarehouseName = s.Warehouse?.Name ?? "â€”",
                Quantity = s.Quantity,
                AverageCost = Math.Round(avgCost, 0),
                StockValue = stockValue,
                Status = status,
                LastSaleDate = lastSaleByProduct.ContainsKey(productId) ? lastSale : null,
                DaysSinceLastSale = daysSince
            });
        }

        var deadRows = rows.Where(r => r.Status == StockHealthStatus.DeadStock).ToList();
        return new StockHealthReportResult
        {
            LowStockCount = rows.Count(r => r.Status == StockHealthStatus.LowStock),
            DeadStockCount = deadRows.Count,
            TotalDeadStockValue = deadRows.Sum(r => r.StockValue),
            Rows = rows.OrderByDescending(r => r.Status == StockHealthStatus.DeadStock)
                .ThenByDescending(r => r.StockValue)
                .ThenBy(r => r.ProductName)
                .ToList()
        };
    }

    public async Task<InventoryReplenishmentReportResult> GetInventoryReplenishmentReportAsync(
        DateTime? from,
        DateTime? to,
        int? warehouseId,
        decimal minimumStock,
        InventoryReplenishmentFilter filter = InventoryReplenishmentFilter.All)
    {
        var minStock = Math.Max(0, minimumStock);

        var stockQ = _db.WarehouseStocks.AsNoTracking()
            .Include(ws => ws.Product).ThenInclude(p => p!.Category)
            .Include(ws => ws.Warehouse)
            .AsQueryable();
        if (warehouseId.HasValue)
            stockQ = stockQ.Where(ws => ws.WarehouseId == warehouseId.Value);

        var stocks = await stockQ.ToListAsync();

        var salesQ = _db.InvoiceItems.AsNoTracking()
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && ii.Invoice != null
                         && (ii.Invoice.InvoiceType == InvoiceType.Sale
                             || ii.Invoice.InvoiceType == InvoiceType.Installment
                             || ii.Invoice.InvoiceType == InvoiceType.SaleReturn));
        if (from.HasValue) salesQ = salesQ.Where(ii => ii.Invoice!.Date >= from.Value);
        if (to.HasValue) salesQ = salesQ.Where(ii => ii.Invoice!.Date < EndOfDay(to));
        if (warehouseId.HasValue) salesQ = salesQ.Where(ii => ii.Invoice!.WarehouseId == warehouseId.Value);

        var salesItems = await salesQ.ToListAsync();
        var soldByKey = salesItems
            .GroupBy(ii => (ProductId: ii.ProductId!.Value, WarehouseId: ii.Invoice!.WarehouseId))
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => InvoiceFilters.SignedSaleLineQuantity(x.Invoice!.InvoiceType, x.Quantity)));

        var warehouses = await _db.Warehouses.AsNoTracking().ToDictionaryAsync(w => w.Id, w => w.Name);
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .ToDictionaryAsync(p => p.Id);

        var replenishmentProductIds = stocks.Select(s => s.ProductId)
            .Concat(soldByKey.Keys.Select(k => k.ProductId))
            .Distinct()
            .ToList();
        var purchasesByProduct = await CloudProductCostHelper.GetPurchaseItemsByProductAsync(_db, replenishmentProductIds);

        var rows = new List<InventoryReplenishmentRow>();
        var processedKeys = new HashSet<(int ProductId, int WarehouseId)>();

        foreach (var s in stocks)
        {
            var key = (s.ProductId, s.WarehouseId);
            processedKeys.Add(key);
            soldByKey.TryGetValue(key, out var soldQty);
            var row = BuildReplenishmentRow(
                s.ProductId, s.WarehouseId,
                s.Product?.Name, s.Warehouse?.Name, s.Product?.Category?.Name,
                s.Quantity, soldQty, minStock, s.OpeningQuantity, s.UnitCost, filter,
                purchasesByProduct.GetValueOrDefault(s.ProductId) ?? []);
            if (row is not null)
                rows.Add(row);
        }

        foreach (var sale in soldByKey)
        {
            if (processedKeys.Contains(sale.Key))
                continue;

            products.TryGetValue(sale.Key.ProductId, out var product);
            warehouses.TryGetValue(sale.Key.WarehouseId, out var warehouseName);
            var row = BuildReplenishmentRow(
                sale.Key.ProductId, sale.Key.WarehouseId,
                product?.Name, warehouseName, product?.Category?.Name,
                0, sale.Value, minStock, 0, 0, filter,
                purchasesByProduct.GetValueOrDefault(sale.Key.ProductId) ?? []);
            if (row is not null)
                rows.Add(row);
        }

        var needs = rows.Where(r => r.Status != InventoryReplenishmentStatus.Sufficient).ToList();
        var sufficientCount = rows.Count - needs.Count;

        return new InventoryReplenishmentReportResult
        {
            TotalProducts = rows.Count,
            TotalCurrentQuantity = rows.Sum(r => r.CurrentQuantity),
            TotalSoldQuantity = rows.Sum(r => r.QuantitySold),
            TotalSuggestedOrderQuantity = rows.Sum(r => r.SuggestedOrderQuantity),
            ItemsNeedingReplenishment = needs.Count,
            TotalStockValue = rows.Sum(r => r.StockValue),
            EstimatedOrderValue = rows.Sum(r => r.EstimatedOrderValue),
            Rows = rows
                .OrderByDescending(r => r.SuggestedOrderQuantity)
                .ThenBy(r => r.CurrentQuantity)
                .ThenBy(r => r.ProductName)
                .ToList(),
            StatusChart =
            [
                new NameAmountPoint { Name = "يحتاج توريد", Amount = needs.Count },
                new NameAmountPoint { Name = "كافٍ", Amount = Math.Max(0, sufficientCount) }
            ],
            ReorderChart = rows
                .Where(r => r.SuggestedOrderQuantity > 0)
                .OrderByDescending(r => r.SuggestedOrderQuantity)
                .Take(10)
                .Select(r => new NameAmountPoint { Name = TruncateChartLabel(r.ProductName), Amount = r.SuggestedOrderQuantity })
                .ToList(),
            StockVsSoldChart = rows
                .OrderByDescending(r => r.QuantitySold)
                .ThenByDescending(r => r.CurrentQuantity)
                .Take(8)
                .ToList()
        };
    }

    public Task<ExpiryReportResult> GetExpiryReportAsync(
        int? warehouseId = null,
        int? productId = null,
        string? productSearch = null,
        DateTime? expiryFrom = null,
        DateTime? expiryTo = null,
        ExpiryStatusFilter statusFilter = ExpiryStatusFilter.All,
        bool hideZeroQuantity = true,
        int nearExpiryCriticalDays = 30,
        int nearExpiryWarningDays = 90)
    {
        // تتبع دفعات الصلاحية محلي على سطح المكتب وغير متزامن مع السحابة حالياً
        return Task.FromResult(new ExpiryReportResult());
    }

    private static InventoryReplenishmentRow? BuildReplenishmentRow(
        int productId,
        int warehouseId,
        string? productName,
        string? warehouseName,
        string? categoryName,
        decimal currentQty,
        decimal soldQty,
        decimal minStock,
        decimal openingQty,
        decimal unitCost,
        InventoryReplenishmentFilter filter,
        IReadOnlyList<CloudInvoiceItem> purchaseItems)
    {
        var targetStock = minStock + soldQty;
        var suggested = Math.Max(0, targetStock - currentQty);
        var status = currentQty <= 0
            ? InventoryReplenishmentStatus.Critical
            : suggested > 0
                ? InventoryReplenishmentStatus.NeedsReorder
                : InventoryReplenishmentStatus.Sufficient;

        if (filter == InventoryReplenishmentFilter.NeedsReplenishmentOnly
            && status == InventoryReplenishmentStatus.Sufficient)
            return null;

        var avgCost = CloudProductCostHelper.ComputeAverageUnitCost(purchaseItems, openingQty, unitCost);

        return new InventoryReplenishmentRow
        {
            ProductId = productId,
            ProductName = productName ?? "—",
            WarehouseName = warehouseName ?? "—",
            CategoryName = categoryName ?? "—",
            CurrentQuantity = currentQty,
            QuantitySold = soldQty,
            MinimumStock = minStock,
            SuggestedOrderQuantity = suggested,
            AverageCost = Math.Round(avgCost, 0),
            StockValue = Math.Round(currentQty * avgCost, 0),
            EstimatedOrderValue = Math.Round(suggested * avgCost, 0),
            Status = status
        };
    }

    public async Task<MinimumQuantityReportResult> GetMinimumQuantityReportAsync(
        int? warehouseId,
        int? categoryId,
        MinimumQuantityFilter filter = MinimumQuantityFilter.All,
        string? search = null)
    {
        var context = _db;

        var query = context.WarehouseStocks.AsNoTracking()
            .Include(ws => ws.Product)!.ThenInclude(p => p!.Category)
            .Include(ws => ws.Warehouse)
            .Where(ws => ws.MinQuantity > 0);

        if (warehouseId.HasValue)
            query = query.Where(ws => ws.WarehouseId == warehouseId.Value);
        if (categoryId.HasValue)
            query = query.Where(ws => ws.Product!.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(ws =>
                ws.Product!.Name.Contains(term) ||
                (ws.Product.Barcode != null && ws.Product.Barcode.Contains(term)) ||
                (ws.Product.Category != null && ws.Product.Category.Name.Contains(term)) ||
                (ws.Warehouse != null && ws.Warehouse.Name.Contains(term)));
        }

        var stocks = await query.ToListAsync();
        var rows = new List<MinimumQuantityRow>();

        foreach (var s in stocks)
        {
            var difference = s.Quantity - s.MinQuantity;
            var status = difference < 0
                ? MinimumQuantityStatus.BelowMinimum
                : difference == 0
                    ? MinimumQuantityStatus.AtMinimum
                    : MinimumQuantityStatus.AboveMinimum;

            if (filter == MinimumQuantityFilter.BelowMinimum && status != MinimumQuantityStatus.BelowMinimum)
                continue;
            if (filter == MinimumQuantityFilter.AtMinimum && status != MinimumQuantityStatus.AtMinimum)
                continue;
            if (filter == MinimumQuantityFilter.AboveMinimum && status != MinimumQuantityStatus.AboveMinimum)
                continue;

            rows.Add(new MinimumQuantityRow
            {
                ProductId = s.ProductId,
                ProductName = s.Product?.Name ?? "—",
                Barcode = s.Product?.Barcode,
                Description = s.Product?.Description,
                CategoryId = s.Product?.CategoryId ?? 0,
                CategoryName = s.Product?.Category?.Name ?? "—",
                WarehouseId = s.WarehouseId,
                WarehouseName = s.Warehouse?.Name ?? "—",
                CurrentQuantity = s.Quantity,
                MinQuantity = s.MinQuantity,
                Status = status
            });
        }

        rows = rows
            .OrderBy(r => r.Status == MinimumQuantityStatus.BelowMinimum ? 0 : r.Status == MinimumQuantityStatus.AtMinimum ? 1 : 2)
            .ThenBy(r => r.Difference)
            .ThenBy(r => r.ProductName)
            .ThenBy(r => r.WarehouseName)
            .ToList();

        var below = rows.Where(r => r.Status == MinimumQuantityStatus.BelowMinimum).ToList();
        return new MinimumQuantityReportResult
        {
            TotalItems = rows.Count,
            BelowMinimumCount = below.Count,
            AtMinimumCount = rows.Count(r => r.Status == MinimumQuantityStatus.AtMinimum),
            AboveMinimumCount = rows.Count(r => r.Status == MinimumQuantityStatus.AboveMinimum),
            TotalShortage = below.Sum(r => Math.Abs(r.Difference)),
            Rows = rows
        };
    }

    private static string TruncateChartLabel(string name) =>
        name.Length <= 18 ? name : string.Concat(name.AsSpan(0, 15), "...");

    private static decimal PercentChange(decimal previous, decimal current) =>
        previous == 0 ? (current == 0 ? 0 : 100) : Math.Round((current - previous) / previous * 100, 1);
}


