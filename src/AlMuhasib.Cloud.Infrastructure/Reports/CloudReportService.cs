using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Core.Entities;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Cloud.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Reports;

public sealed class CloudReportService : Application.Abstractions.ICloudReportService
{
    private readonly CloudDbContext _db;

    public CloudReportService(CloudDbContext db) => _db = db;

    /// <summary>Normalize "to" date to include the entire day (start of next day).</summary>
    private static DateTime? EndOfDay(DateTime? to) => to?.Date.AddDays(1);

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // SALES
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<SalesReportResult> GetSalesReportAsync(DateTime? from, DateTime? to, int? customerId, PaymentMethod? method, int? warehouseId = null)
    {
        var context = _db;
        var query = context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Warehouse)
            .Include(i => i.InstallmentPlans)
            .Where(i => i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment);

        if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
        if (to.HasValue) query = query.Where(i => i.Date < EndOfDay(to));
        if (customerId.HasValue) query = query.Where(i => i.CustomerId == customerId.Value);
        if (method.HasValue) query = query.Where(i => i.PaymentMethod == method.Value);
        if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);

        var invoices = await query.OrderByDescending(i => i.Date).ToListAsync();

        var todaySales = invoices.Where(i => i.Date.Date == DateTime.Today).Sum(i => i.NetAmount);
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

        return new SalesReportResult
        {
            TotalSales = invoices.Sum(i => i.NetAmount),
            TotalCompanyFees = invoices.Sum(ResolveCompanyFee),
            CashSales = invoices.Where(i => i.PaymentMethod == PaymentMethod.Cash).Sum(i => i.NetAmount),
            CreditSales = invoices.Where(i => i.PaymentMethod == PaymentMethod.Credit).Sum(i => i.NetAmount),
            InstallmentSales = invoices.Where(i => i.PaymentMethod == PaymentMethod.Installment).Sum(i => i.NetAmount),
            InvoiceCount = invoices.Count,
            AverageInvoice = invoices.Count > 0 ? invoices.Sum(i => i.NetAmount) / invoices.Count : 0,
            TodaySales = todaySales,
            DailyChart = invoices.GroupBy(i => i.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(i => i.NetAmount) })
                .OrderBy(d => d.Date).ToList(),
            Rows = invoices.Select(i => new SalesReportRow
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                Date = i.Date,
                CustomerName = i.Customer?.Name ?? "\u2014",
                WarehouseName = i.Warehouse?.Name ?? "\u2014",
                PaymentMethod = i.PaymentMethod switch
                {
                    PaymentMethod.Cash => "\u0646\u0642\u062f\u064a",
                    PaymentMethod.Credit => "\u0622\u062c\u0644",
                    PaymentMethod.Installment => "\u0623\u0642\u0633\u0627\u0637",
                    _ => "\u2014"
                },
                TotalAmount = i.TotalAmount,
                Discount = i.DiscountAmount,
                NetAmount = i.NetAmount,
                CompanyFeeAmount = ResolveCompanyFee(i),
                CreditDueDate = i.CreditDueDate,
                PaidAmount = i.PaidAmount,
                RemainingAmount = i.RemainingAmount,
                IsCreditPaid = i.IsCreditPaid
            }).ToList()
        };
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // PURCHASES
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<PurchasesReportResult> GetPurchasesReportAsync(DateTime? from, DateTime? to, int? supplierId, int? warehouseId, PaymentMethod? method = null)
    {
        var context = _db;
        var query = context.Invoices
            .Include(i => i.Supplier)
            .Include(i => i.Warehouse)
            .Where(i => i.InvoiceType == InvoiceType.Purchase);

        if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
        if (to.HasValue) query = query.Where(i => i.Date < EndOfDay(to));
        if (supplierId.HasValue) query = query.Where(i => i.SupplierId == supplierId.Value);
        if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);
        if (method.HasValue) query = query.Where(i => i.PaymentMethod == method.Value);

        var invoices = await query.OrderByDescending(i => i.Date).ToListAsync();
        var todayPurchases = invoices.Where(i => i.Date.Date == DateTime.Today).Sum(i => i.NetAmount);

        return new PurchasesReportResult
        {
            TotalPurchases = invoices.Sum(i => i.NetAmount),
            InvoiceCount = invoices.Count,
            AverageInvoice = invoices.Count > 0 ? invoices.Sum(i => i.NetAmount) / invoices.Count : 0,
            TodayPurchases = todayPurchases,
            DailyChart = invoices.GroupBy(i => i.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(i => i.NetAmount) })
                .OrderBy(d => d.Date).ToList(),
            BySupplierChart = invoices.GroupBy(i => i.Supplier?.Name ?? "\u0623\u062e\u0631\u0649")
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(i => i.NetAmount) })
                .OrderByDescending(x => x.Amount).Take(6).ToList(),
            Rows = invoices.Select(i => new PurchasesReportRow
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
                TotalAmount = i.TotalAmount,
                Discount = i.DiscountAmount,
                NetAmount = i.NetAmount,
                PaidAmount = i.PaidAmount,
                RemainingAmount = i.RemainingAmount,
                IsCreditPaid = i.IsCreditPaid
            }).ToList()
        };
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // PROFIT
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<ProfitReportResult> GetProfitReportAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var salesQ = CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans);
        var expQ = context.Expenses.AsQueryable();
        var bankQ = context.Vouchers.Where(v => v.VoucherType == VoucherType.BankReceipt);
        var distQ = context.ProfitDistributions.AsQueryable();

        if (from.HasValue) { salesQ = salesQ.Where(i => i.Date >= from.Value); expQ = expQ.Where(e => e.Date >= from.Value); bankQ = bankQ.Where(v => v.Date >= from.Value); distQ = distQ.Where(p => p.Date >= from.Value); }
        if (to.HasValue) { salesQ = salesQ.Where(i => i.Date < EndOfDay(to)); expQ = expQ.Where(e => e.Date < EndOfDay(to)); bankQ = bankQ.Where(v => v.Date < EndOfDay(to)); distQ = distQ.Where(p => p.Date < EndOfDay(to)); }

        var totalSales = await salesQ.SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var cogs = await CalculateCogsAsync(context, from, EndOfDay(to));
        var totalExpenses = await expQ.SumAsync(e => (decimal?)e.Amount) ?? 0;
        var totalBankFees = await bankQ.SumAsync(v => (decimal?)v.BankFees) ?? 0;
        var distributed = await distQ.SumAsync(p => (decimal?)p.DistributedAmount) ?? 0;
        var grossProfit = totalSales - cogs;
        var profitOpening = await CloudProductCostHelper.GetProfitOpeningBalanceAsync(context, to);
        var netProfit = grossProfit - totalExpenses - totalBankFees - distributed + profitOpening;

        return new ProfitReportResult
        {
            TotalSales = totalSales, TotalPurchases = cogs, GrossProfit = grossProfit,
            TotalExpenses = totalExpenses, TotalBankFees = totalBankFees,
            DistributedProfits = distributed, NetProfit = netProfit,
            ProfitMargin = totalSales > 0 ? Math.Round(grossProfit / totalSales * 100, 1) : 0
        };
    }

    public async Task<List<MonthlyProfitRow>> GetMonthlyProfitAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
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

            var sales = await CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans)
                .Where(i => i.Date >= effectiveFrom && i.Date < effectiveToExclusive)
                .SumAsync(i => (decimal?)i.NetAmount) ?? 0;

            var purchases = await CalculateCogsAsync(context, effectiveFrom, effectiveToExclusive);
            var expenses = await context.Expenses
                .Where(e => e.Date >= effectiveFrom && e.Date < effectiveToExclusive)
                .SumAsync(e => (decimal?)e.Amount) ?? 0;

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
        var soldItemsQuery = context.InvoiceItems
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && ii.Invoice != null
                         && (ii.Invoice.InvoiceType == InvoiceType.Sale || ii.Invoice.InvoiceType == InvoiceType.Installment));

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

        var purchaseItemsQuery = context.InvoiceItems
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && productIds.Contains(ii.ProductId.Value)
                         && ii.Invoice != null
                         && ii.Invoice.InvoiceType == InvoiceType.Purchase);

        if (toExclusive.HasValue)
            purchaseItemsQuery = purchaseItemsQuery.Where(ii => ii.Invoice!.Date < toExclusive.Value);

        var purchaseItems = await purchaseItemsQuery.ToListAsync();
        var purchasesByProduct = purchaseItems
            .GroupBy(ii => ii.ProductId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        decimal cogs = 0;
        foreach (var sold in soldItems)
        {
            var productId = sold.ProductId!.Value;
            var productPurchases = purchasesByProduct.GetValueOrDefault(productId) ?? [];
            var avgCost = CloudProductCostHelper.ComputeAverageUnitCostForProduct(productPurchases, stocks, productId);
            cogs += Math.Round(sold.Quantity * avgCost, 0);
        }

        return cogs;
    }

    public async Task<List<ProfitInvoiceDetailRow>> GetProfitInvoiceDetailsAsync(DateTime? from, DateTime? to)
    {
        var invoicesQ = CloudInvoiceFilters.ForProfitAndSalesTotals(_db.Invoices, _db.InstallmentPlans).AsQueryable();

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

        var purchaseItems = await _db.InvoiceItems
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && productIds.Contains(ii.ProductId.Value)
                         && ii.Invoice != null
                         && ii.Invoice.InvoiceType == InvoiceType.Purchase
                         && (!to.HasValue || ii.Invoice.Date < EndOfDay(to)))
            .ToListAsync();

        var purchasesByProduct = purchaseItems
            .GroupBy(ii => ii.ProductId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<ProfitInvoiceDetailRow>();
        foreach (var invoice in invoices)
        {
            decimal cost = 0;
            var lineItems = invoice.Items.Where(ii => ii.ProductId != null).ToList();
            foreach (var item in lineItems)
            {
                var productId = item.ProductId!.Value;
                var productPurchases = purchasesByProduct.GetValueOrDefault(productId) ?? [];
                var avgCost = CloudProductCostHelper.ComputeAverageUnitCostForProduct(productPurchases, stocks, productId);
                cost += Math.Round(item.Quantity * avgCost, 0);
            }

            var revenue = invoice.NetAmount;
            var profit = revenue - cost;
            rows.Add(new ProfitInvoiceDetailRow
            {
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                Date = invoice.Date,
                CustomerName = invoice.Customer?.Name ?? "—",
                InvoiceTypeLabel = invoice.InvoiceType == InvoiceType.Installment ? "أقساط" : "مبيعات",
                ItemCount = lineItems.Count,
                Revenue = revenue,
                Cost = cost,
                GrossProfit = profit,
                MarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100, 1) : 0
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
        var plansQ = context.InstallmentPlans.Include(p => p.Customer).Include(p => p.Installments).AsQueryable();
        if (customerId.HasValue) plansQ = plansQ.Where(p => p.CustomerId == customerId.Value);
        var plans = await plansQ.ToListAsync();

        var rows = new List<InstallmentSummaryRow>();
        foreach (var plan in plans)
        {
            var insts = plan.Installments.Where(i => !i.IsDeleted).ToList();
            if (from.HasValue) insts = insts.Where(i => i.DueDate >= from.Value).ToList();
            if (to.HasValue) insts = insts.Where(i => i.DueDate < EndOfDay(to)).ToList();
            if (insts.Count == 0) continue;

            var total = insts.Sum(i => i.Amount);
            var paid = insts.Sum(i => i.PaidAmount);
            var remaining = insts.Sum(i => i.RemainingAmount);
            var hasOverdue = insts.Any(i => i.Status == InstallmentStatus.Overdue);
            var allPaid = insts.All(i => i.Status == InstallmentStatus.Paid);
            var statusText = allPaid ? "\u0645\u0633\u062f\u062f" : hasOverdue ? "\u0645\u062a\u0623\u062e\u0631" : "\u0642\u064a\u062f \u0627\u0644\u062a\u0633\u062f\u064a\u062f";

            if (!string.IsNullOrEmpty(status) && status != "\u0627\u0644\u0643\u0644" && statusText != status) continue;

            rows.Add(new InstallmentSummaryRow
            {
                CustomerName = plan.Customer?.Name ?? "\u2014",
                PlanNumber = plan.Id.ToString(),
                TotalAmount = total, PaidAmount = paid, RemainingAmount = remaining,
                InstallmentCount = insts.Count, Status = statusText
            });
        }

        var allInsts = plans.SelectMany(p => p.Installments.Where(i => !i.IsDeleted)).ToList();
        var paidInsts = allInsts.Where(i => i.Status == InstallmentStatus.Paid);
        var overdueInsts = allInsts.Where(i => i.Status == InstallmentStatus.Overdue);
        var unpaidInsts = allInsts.Where(i => i.Status != InstallmentStatus.Paid);

        return new InstallmentsSummaryResult
        {
            TotalAmount = allInsts.Sum(i => i.Amount),
            PaidAmount = allInsts.Sum(i => i.PaidAmount),
            UnpaidAmount = unpaidInsts.Sum(i => i.RemainingAmount),
            OverdueAmount = overdueInsts.Sum(i => i.RemainingAmount),
            TotalCount = allInsts.Count, PaidCount = paidInsts.Count(),
            UnpaidCount = unpaidInsts.Count(), OverdueCount = overdueInsts.Count(),
            Rows = rows,
            StatusChart =
            [
                new() { Name = "Ù…Ø³Ø¯Ø¯", Amount = paidInsts.Count() },
                new() { Name = "Ø¬Ø²Ø¦ÙŠ", Amount = allInsts.Count(i => i.Status == InstallmentStatus.PartiallyPaid) },
                new() { Name = "Ù…Ø¹Ù„Ù‚", Amount = allInsts.Count(i => i.Status == InstallmentStatus.Pending) },
                new() { Name = "Ù…ØªØ£Ø®Ø±", Amount = overdueInsts.Count() }
            ],
            MonthlyCollectionChart = allInsts.Where(i => i.PaymentDate.HasValue)
                .GroupBy(i => new DateTime(i.PaymentDate!.Value.Year, i.PaymentDate.Value.Month, 1))
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(i => i.PaidAmount) })
                .OrderBy(d => d.Date).ToList()
        };
    }

    public async Task<InstallmentDetailResult> GetInstallmentDetailAsync(int customerId)
    {
        var context = _db;
        var plans = await context.InstallmentPlans
            .Include(p => p.Customer).Include(p => p.Installments)
            .Where(p => p.CustomerId == customerId).ToListAsync();

        var allInsts = plans.SelectMany(p => p.Installments.Where(i => !i.IsDeleted)).ToList();
        var totalAmt = allInsts.Sum(i => i.Amount);
        var paidAmt = allInsts.Sum(i => i.PaidAmount);

        return new InstallmentDetailResult
        {
            CustomerName = plans.FirstOrDefault()?.Customer?.Name ?? "\u2014",
            PlanCount = plans.Count, TotalAmount = totalAmt,
            CollectionRate = totalAmt > 0 ? Math.Round(paidAmt / totalAmt * 100, 1) : 0,
            AverageInstallment = allInsts.Count > 0 ? totalAmt / allInsts.Count : 0,
            Rows = allInsts.OrderBy(i => i.DueDate).Select(i => new InstallmentDetailRow
            {
                DueDate = i.DueDate, Amount = i.Amount, PaidAmount = i.PaidAmount,
                RemainingAmount = i.RemainingAmount, PaymentDate = i.PaymentDate,
                PlanNumber = i.InstallmentPlanId.ToString(),
                Status = i.Status switch
                {
                    InstallmentStatus.Paid => "\u0645\u0633\u062f\u062f",
                    InstallmentStatus.PartiallyPaid => "\u062c\u0632\u0626\u064a",
                    InstallmentStatus.Overdue => "\u0645\u062a\u0623\u062e\u0631",
                    _ => "\u0645\u0639\u0644\u0642"
                }
            }).ToList(),
            MonthlyDueChart = allInsts.GroupBy(i => new DateTime(i.DueDate.Year, i.DueDate.Month, 1))
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(i => i.Amount) })
                .OrderBy(d => d.Date).ToList()
        };
    }

    public async Task<PaidInstallmentsResult> GetPaidInstallmentsAsync(DateTime? from, DateTime? to, int? customerId, int? cashBoxId)
    {
        var context = _db;
        var query = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Include(i => i.CashBox)
            .Where(i => i.Status == InstallmentStatus.Paid);

        if (from.HasValue) query = query.Where(i => i.PaymentDate >= from.Value);
        if (to.HasValue) query = query.Where(i => i.PaymentDate < EndOfDay(to));
        if (customerId.HasValue) query = query.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);
        if (cashBoxId.HasValue) query = query.Where(i => i.CashBoxId == cashBoxId.Value);

        var insts = await query.OrderByDescending(i => i.PaymentDate).ToListAsync();

        return new PaidInstallmentsResult
        {
            TotalPaid = insts.Sum(i => i.PaidAmount),
            PaidCount = insts.Count,
            MaxPaid = insts.Count > 0 ? insts.Max(i => i.PaidAmount) : 0,
            AveragePaymentDays = insts.Count > 0 ? (decimal)Math.Round(insts.Where(i => i.PaymentDate.HasValue).Average(i => (i.PaymentDate!.Value - i.DueDate).TotalDays), 0) : 0,
            Rows = insts.Select(i => new PaidInstallmentRow
            {
                CustomerName = i.InstallmentPlan?.Customer?.Name ?? "\u2014",
                PlanNumber = i.InstallmentPlanId.ToString(),
                Amount = i.PaidAmount, PaymentDate = i.PaymentDate ?? i.DueDate,
                CashBoxName = i.CashBox?.Name ?? "\u2014"
            }).ToList(),
            MonthlyChart = insts.Where(i => i.PaymentDate.HasValue)
                .GroupBy(i => new DateTime(i.PaymentDate!.Value.Year, i.PaymentDate.Value.Month, 1))
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(i => i.PaidAmount) })
                .OrderBy(d => d.Date).ToList(),
            ByCashBoxChart = insts.GroupBy(i => i.CashBox?.Name ?? "\u063a\u064a\u0631 \u0645\u062d\u062f\u062f")
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(i => i.PaidAmount) }).ToList()
        };
    }

    public async Task<UnpaidInstallmentsResult> GetUnpaidInstallmentsAsync(DateTime? from, DateTime? to, int? customerId)
    {
        var context = _db;
        var query = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Where(i => i.Status != InstallmentStatus.Paid);

        if (from.HasValue) query = query.Where(i => i.DueDate >= from.Value);
        if (to.HasValue) query = query.Where(i => i.DueDate < EndOfDay(to));
        if (customerId.HasValue) query = query.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);

        var insts = await query.OrderBy(i => i.DueDate).ToListAsync();
        var today = DateTime.Today;

        return new UnpaidInstallmentsResult
        {
            TotalUnpaid = insts.Sum(i => i.RemainingAmount),
            UnpaidCount = insts.Count,
            CustomerCount = insts.Select(i => i.InstallmentPlan?.CustomerId).Distinct().Count(),
            OldestOverdueDays = insts.Where(i => i.DueDate < today).Select(i => (today - i.DueDate).Days).DefaultIfEmpty(0).Max(),
            Rows = insts.Select(i => new UnpaidInstallmentRow
            {
                InstallmentId = i.Id,
                InvoiceId = i.InstallmentPlan?.InvoiceId ?? 0,
                CustomerName = i.InstallmentPlan?.Customer?.Name ?? "\u2014",
                PlanNumber = i.InstallmentPlanId.ToString(),
                DueDate = i.DueDate, Amount = i.Amount, RemainingAmount = i.RemainingAmount,
                OverdueDays = i.DueDate < today ? (today - i.DueDate).Days : 0
            }).ToList(),
            ByCustomerChart = insts.GroupBy(i => i.InstallmentPlan?.Customer?.Name ?? "\u2014")
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(i => i.RemainingAmount) })
                .OrderByDescending(x => x.Amount).Take(10).ToList()
        };
    }

    public async Task<OverdueResult> GetOverdueReportAsync(DateTime asOfDate, int? minDaysOverdue, int? customerId)
    {
        var context = _db;

        // â”€â”€ 1. Overdue installments â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var instQuery = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Where(i => i.Status != InstallmentStatus.Paid && i.DueDate < asOfDate);

        if (customerId.HasValue) instQuery = instQuery.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);

        var insts = await instQuery.OrderBy(i => i.DueDate).ToListAsync();
        if (minDaysOverdue.HasValue)
            insts = insts.Where(i => (asOfDate - i.DueDate).Days >= minDaysOverdue.Value).ToList();

        var rows = insts.Select(i => new OverdueRow
        {
            InstallmentId = i.Id,
            InvoiceId = i.InstallmentPlan?.InvoiceId ?? 0,
            CustomerName = i.InstallmentPlan?.Customer?.Name ?? "\u2014",
            Phone = i.InstallmentPlan?.Customer?.Phone ?? "\u2014",
            PlanNumber = i.InstallmentPlanId.ToString(),
            OverdueAmount = i.RemainingAmount,
            OverdueDays = (asOfDate - i.DueDate).Days,
            LastPaymentDate = i.PaymentDate,
            DueDate = i.DueDate
        }).ToList();

        // â”€â”€ 2. Overdue credit invoices (Ø¢Ø¬Ù„) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var creditQuery = context.Invoices
            .Include(i => i.Customer)
            .Where(i => i.PaymentMethod == PaymentMethod.Credit
                        && i.CreditDueDate.HasValue
                        && i.CreditDueDate.Value.Date < asOfDate.Date
                        && (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment));

        if (customerId.HasValue) creditQuery = creditQuery.Where(i => i.CustomerId == customerId.Value);

        var creditInvoices = await creditQuery.OrderBy(i => i.CreditDueDate).ToListAsync();
        if (minDaysOverdue.HasValue)
            creditInvoices = creditInvoices.Where(i => (asOfDate - i.CreditDueDate!.Value.Date).Days >= minDaysOverdue.Value).ToList();

        var creditRows = creditInvoices.Select(i => new OverdueRow
        {
            InstallmentId = 0,
            InvoiceId = i.Id,
            CustomerName = i.Customer?.Name ?? "\u2014",
            Phone = i.Customer?.Phone ?? "\u2014",
            PlanNumber = i.InvoiceNumber,
            OverdueAmount = i.RemainingAmount > 0 ? i.RemainingAmount : i.NetAmount,
            OverdueDays = (asOfDate.Date - i.CreditDueDate!.Value.Date).Days,
            LastPaymentDate = null,
            DueDate = i.CreditDueDate!.Value
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
        var customer = await context.Customers.FindAsync(customerId);
        if (customer is null) return new CustomerStatementResult { CustomerName = "\u2014" };

        var rows = new List<CustomerStatementRow>();

        var invQ = context.Invoices
            .Where(i => i.CustomerId == customerId &&
                        (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
                        (i.PaymentMethod == PaymentMethod.Credit || i.PaymentMethod == PaymentMethod.Installment));
        if (from.HasValue) invQ = invQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) invQ = invQ.Where(i => i.Date < EndOfDay(to));
        foreach (var inv in await invQ.OrderBy(i => i.Date).ToListAsync())
            rows.Add(new CustomerStatementRow { Date = inv.Date, Description = $"\u0641\u0627\u062a\u0648\u0631\u0629 \u0645\u0628\u064a\u0639\u0627\u062a {inv.InvoiceNumber}", Debit = inv.NetAmount });

        var vQ = context.Vouchers
            .Where(v => v.CustomerId == customerId && (v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt));
        if (from.HasValue) vQ = vQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) vQ = vQ.Where(v => v.Date < EndOfDay(to));
        foreach (var v in await vQ.OrderBy(v => v.Date).ToListAsync())
            rows.Add(new CustomerStatementRow { Date = v.Date, Description = v.VoucherType == VoucherType.Receipt ? $"\u0633\u0646\u062f \u0642\u0628\u0636 {v.VoucherNumber}" : $"\u0633\u0646\u062f \u062a\u0633\u062f\u064a\u062f \u062f\u064a\u0646 {v.VoucherNumber}", Credit = v.Amount });

        var planIds = await context.InstallmentPlans.Where(p => p.CustomerId == customerId).Select(p => p.Id).ToListAsync();
        if (planIds.Count > 0)
        {
            var instQ = context.Installments.Where(i => planIds.Contains(i.InstallmentPlanId) && i.PaidAmount > 0);
            if (from.HasValue) instQ = instQ.Where(i => (i.PaymentDate ?? i.DueDate) >= from.Value);
            if (to.HasValue) instQ = instQ.Where(i => (i.PaymentDate ?? i.DueDate) < EndOfDay(to));
            foreach (var inst in await instQ.OrderBy(i => i.PaymentDate).ToListAsync())
                rows.Add(new CustomerStatementRow { Date = inst.PaymentDate ?? inst.DueDate, Description = "\u062f\u0641\u0639\u0629 \u0642\u0633\u0637", Credit = inst.PaidAmount });
        }

        rows = rows.OrderBy(r => r.Date).ToList();
        decimal balance = 0;
        foreach (var r in rows) { balance += r.Debit - r.Credit; r.RunningBalance = balance; }

        return new CustomerStatementResult
        {
            CustomerName = customer.Name,
            TotalDebit = rows.Sum(r => r.Debit), TotalCredit = rows.Sum(r => r.Credit),
            Balance = balance, TransactionCount = rows.Count, Rows = rows
        };
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // SUPPLIER STATEMENT
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<SupplierStatementResult> GetSupplierStatementAsync(int supplierId, DateTime? from = null, DateTime? to = null)
    {
        var context = _db;
        var supplier = await context.Suppliers.FindAsync(supplierId);
        if (supplier is null) return new SupplierStatementResult { SupplierName = "\u2014" };

        var rows = new List<SupplierStatementRow>();

        var invQ = context.Invoices
            .Where(i => i.SupplierId == supplierId && i.InvoiceType == InvoiceType.Purchase && i.PaymentMethod == PaymentMethod.Credit);
        if (from.HasValue) invQ = invQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) invQ = invQ.Where(i => i.Date < EndOfDay(to));
        foreach (var inv in await invQ.OrderBy(i => i.Date).ToListAsync())
            rows.Add(new SupplierStatementRow { Date = inv.Date, Description = $"\u0641\u0627\u062a\u0648\u0631\u0629 \u0645\u0634\u062a\u0631\u064a\u0627\u062a {inv.InvoiceNumber}", Credit = inv.NetAmount });

        var vQ = context.Vouchers.Where(v => v.CustomerId == supplierId && v.VoucherType == VoucherType.Payment);
        if (from.HasValue) vQ = vQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) vQ = vQ.Where(v => v.Date < EndOfDay(to));
        foreach (var v in await vQ.OrderBy(v => v.Date).ToListAsync())
            rows.Add(new SupplierStatementRow { Date = v.Date, Description = $"\u0633\u0646\u062f \u0635\u0631\u0641 {v.VoucherNumber}", Debit = v.Amount });

        rows = rows.OrderBy(r => r.Date).ToList();
        decimal balance = 0;
        foreach (var r in rows) { balance += r.Credit - r.Debit; r.RunningBalance = balance; }

        var invoiceCount = rows.Count(r => r.Credit > 0);
        return new SupplierStatementResult
        {
            SupplierName = supplier.Name,
            TotalDebit = rows.Sum(r => r.Debit), TotalCredit = rows.Sum(r => r.Credit),
            Balance = balance, InvoiceCount = invoiceCount, Rows = rows
        };
    }

    public async Task<InvestorStatementResult> GetInvestorStatementAsync(int investorId, DateTime? from = null, DateTime? to = null)
    {
        var context = _db;
        var investor = await context.Investors.FindAsync(investorId);
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
        var query = context.Expenses.Include(e => e.ExpenseType).Include(e => e.CashBox).AsQueryable();
        if (from.HasValue) query = query.Where(e => e.Date >= from.Value);
        if (to.HasValue) query = query.Where(e => e.Date < EndOfDay(to));
        if (expenseTypeId.HasValue) query = query.Where(e => e.ExpenseTypeId == expenseTypeId.Value);
        if (cashBoxId.HasValue) query = query.Where(e => e.CashBoxId == cashBoxId.Value);

        var expenses = await query.OrderByDescending(e => e.Date).ToListAsync();
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        return new ExpensesReportResult
        {
            TotalExpenses = expenses.Sum(e => e.Amount),
            TodayExpenses = expenses.Where(e => e.Date.Date == today).Sum(e => e.Amount),
            MonthExpenses = expenses.Where(e => e.Date >= monthStart).Sum(e => e.Amount),
            TopExpenseType = expenses.GroupBy(e => e.ExpenseType?.Name ?? "\u0623\u062e\u0631\u0649")
                .OrderByDescending(g => g.Sum(e => e.Amount)).FirstOrDefault()?.Key ?? "\u2014",
            Rows = expenses.Select(e => new ExpenseReportRow
            {
                Date = e.Date, ExpenseTypeName = e.ExpenseType?.Name ?? "\u2014",
                Amount = e.Amount, CashBoxName = e.CashBox?.Name ?? "\u2014",
                Notes = e.Notes ?? "", CreatedBy = e.CreatedBy ?? "\u2014"
            }).ToList(),
            ByTypeChart = expenses.GroupBy(e => e.ExpenseType?.Name ?? "\u0623\u062e\u0631\u0649")
                .Select(g => new NameAmountPoint { Name = g.Key, Amount = g.Sum(e => e.Amount) }).ToList(),
            DailyChart = expenses.GroupBy(e => e.Date.Date)
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(e => e.Amount) })
                .OrderBy(d => d.Date).ToList()
        };
    }

    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
    // INCOME & EXPENSE
    // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

    public async Task<IncomeExpenseResult> GetIncomeExpenseReportAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var rows = new List<IncomeExpenseRow>();

        var salesQ = CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans);
        var expQ = context.Expenses.Include(e => e.ExpenseType).AsQueryable();
        if (from.HasValue) { salesQ = salesQ.Where(i => i.Date >= from.Value); expQ = expQ.Where(e => e.Date >= from.Value); }
        if (to.HasValue) { salesQ = salesQ.Where(i => i.Date < EndOfDay(to)); expQ = expQ.Where(e => e.Date < EndOfDay(to)); }

        var totalSales = await salesQ.SumAsync(i => (decimal?)i.NetAmount) ?? 0;

        var instQ = context.Installments.Where(i => i.PaidAmount > 0);
        if (from.HasValue) instQ = instQ.Where(i => i.PaymentDate >= from.Value);
        if (to.HasValue) instQ = instQ.Where(i => i.PaymentDate < EndOfDay(to));
        var instCollections = await instQ.SumAsync(i => (decimal?)i.PaidAmount) ?? 0;

        var recQ = context.Vouchers.Where(v => v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt);
        if (from.HasValue) recQ = recQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) recQ = recQ.Where(v => v.Date < EndOfDay(to));
        var receipts = await recQ.SumAsync(v => (decimal?)v.Amount) ?? 0;

        var totalIncome = totalSales + instCollections + receipts;
        rows.Add(new IncomeExpenseRow { Section = "\u0627\u0644\u0648\u0627\u0631\u062f\u0627\u062a", Type = "\u0625\u064a\u0631\u0627\u062f", Description = "\u0645\u0628\u064a\u0639\u0627\u062a \u0646\u0642\u062f\u064a\u0629", Amount = totalSales });
        rows.Add(new IncomeExpenseRow { Section = "\u0627\u0644\u0648\u0627\u0631\u062f\u0627\u062a", Type = "\u0625\u064a\u0631\u0627\u062f", Description = "\u062a\u062d\u0635\u064a\u0644 \u0623\u0642\u0633\u0627\u0637", Amount = instCollections });
        rows.Add(new IncomeExpenseRow { Section = "\u0627\u0644\u0648\u0627\u0631\u062f\u0627\u062a", Type = "\u0625\u064a\u0631\u0627\u062f", Description = "\u0633\u0646\u062f\u0627\u062a \u0642\u0628\u0636", Amount = receipts });

        var expenses = await expQ.ToListAsync();
        var grouped = expenses.GroupBy(e => e.ExpenseType?.Name ?? "\u0623\u062e\u0631\u0649");
        foreach (var g in grouped)
            rows.Add(new IncomeExpenseRow { Section = "\u0627\u0644\u0645\u0635\u0631\u0648\u0641\u0627\u062a", Type = "\u0645\u0635\u0631\u0648\u0641", Description = g.Key, Amount = g.Sum(e => e.Amount) });

        var totalExpenses = expenses.Sum(e => e.Amount);
        var net = totalIncome - totalExpenses;

        var f = from ?? DateTime.Today.AddMonths(-12);
        var t = to ?? DateTime.Today;
        var monthlyIncome = await context.Invoices
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) && i.Date >= f && i.Date <= t)
            .GroupBy(i => new { i.Date.Year, i.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(i => i.NetAmount) }).ToListAsync();
        var monthlyExp = await context.Expenses
            .Where(e => e.Date >= f && e.Date <= t)
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(e => e.Amount) }).ToListAsync();

        var monthlyChart = new List<MonthlyIncomeExpensePoint>();
        for (var d = new DateTime(f.Year, f.Month, 1); d <= t; d = d.AddMonths(1))
        {
            monthlyChart.Add(new MonthlyIncomeExpensePoint
            {
                Month = $"{d.Year}/{d.Month:D2}",
                Income = monthlyIncome.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Amount ?? 0,
                Expense = monthlyExp.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Amount ?? 0
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
        var result = new List<WarehouseStockRow>();
        foreach (var s in stocks)
        {
            var pi = await context.InvoiceItems.Include(ii => ii.Invoice)
                .Where(ii => ii.ProductId == s.ProductId && ii.Invoice!.InvoiceType == InvoiceType.Purchase).ToListAsync();
            var avgCost = CloudProductCostHelper.ComputeAverageUnitCost(pi, s.OpeningQuantity, s.UnitCost);

            result.Add(new WarehouseStockRow
            {
                ProductName = s.Product?.Name ?? "\u2014", WarehouseName = s.Warehouse?.Name ?? "\u2014",
                Quantity = s.Quantity, AverageCost = Math.Round(avgCost, 0), TotalValue = Math.Round(s.Quantity * avgCost, 0)
            });
        }
        return result;
    }

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
        var rows = new List<CashFlowRow>();

        var salesQ = context.Invoices.Include(i => i.CashBox)
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) && i.PaymentMethod == PaymentMethod.Cash);
        if (cashBoxId.HasValue) salesQ = salesQ.Where(i => i.CashBoxId == cashBoxId.Value);
        if (from.HasValue) salesQ = salesQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) salesQ = salesQ.Where(i => i.Date < EndOfDay(to));
        foreach (var inv in await salesQ.ToListAsync())
            rows.Add(new CashFlowRow { Date = inv.Date, Type = "\u0645\u0628\u064a\u0639\u0627\u062a", Description = $"\u0641\u0627\u062a\u0648\u0631\u0629 {inv.InvoiceNumber}", Incoming = inv.NetAmount, AccountName = inv.CashBox?.Name ?? "\u2014" });

        var purchQ = context.Invoices.Include(i => i.CashBox)
            .Where(i => i.InvoiceType == InvoiceType.Purchase && i.PaymentMethod == PaymentMethod.Cash);
        if (cashBoxId.HasValue) purchQ = purchQ.Where(i => i.CashBoxId == cashBoxId.Value);
        if (from.HasValue) purchQ = purchQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) purchQ = purchQ.Where(i => i.Date < EndOfDay(to));
        foreach (var inv in await purchQ.ToListAsync())
            rows.Add(new CashFlowRow { Date = inv.Date, Type = "\u0645\u0634\u062a\u0631\u064a\u0627\u062a", Description = $"\u0641\u0627\u062a\u0648\u0631\u0629 {inv.InvoiceNumber}", Outgoing = inv.NetAmount, AccountName = inv.CashBox?.Name ?? "\u2014" });

        var vouchQ = context.Vouchers.Include(v => v.CashBox).AsQueryable();
        if (cashBoxId.HasValue) vouchQ = vouchQ.Where(v => v.CashBoxId == cashBoxId.Value);
        if (from.HasValue) vouchQ = vouchQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) vouchQ = vouchQ.Where(v => v.Date < EndOfDay(to));
        foreach (var v in await vouchQ.ToListAsync())
        {
            bool isIncoming = v.VoucherType is VoucherType.Receipt or VoucherType.DebtReceipt or VoucherType.InvestorDeposit or VoucherType.BankReceipt;
            rows.Add(new CashFlowRow
            {
                Date = v.Date,
                Type = v.VoucherType switch { VoucherType.Receipt => "\u0633\u0646\u062f \u0642\u0628\u0636", VoucherType.Payment => "\u0633\u0646\u062f \u0635\u0631\u0641", VoucherType.DebtReceipt => "\u062a\u0633\u062f\u064a\u062f \u062f\u064a\u0646", VoucherType.BankReceipt => "\u0642\u0628\u0636 \u0628\u0646\u0643\u064a", VoucherType.InvestorDeposit => "\u0625\u064a\u062f\u0627\u0639 \u0645\u0633\u062a\u062b\u0645\u0631", VoucherType.InvestorWithdrawal => "\u0633\u062d\u0628 \u0645\u0633\u062a\u062b\u0645\u0631", _ => "\u0633\u0646\u062f" },
                Description = v.VoucherNumber,
                Incoming = isIncoming ? v.Amount : 0,
                Outgoing = !isIncoming ? v.Amount : 0,
                AccountName = v.CashBox?.Name ?? "\u2014"
            });
        }

        var expQ = context.Expenses.Include(e => e.CashBox).AsQueryable();
        if (cashBoxId.HasValue) expQ = expQ.Where(e => e.CashBoxId == cashBoxId.Value);
        if (from.HasValue) expQ = expQ.Where(e => e.Date >= from.Value);
        if (to.HasValue) expQ = expQ.Where(e => e.Date < EndOfDay(to));
        foreach (var e in await expQ.ToListAsync())
            rows.Add(new CashFlowRow { Date = e.Date, Type = "\u0645\u0635\u0631\u0648\u0641", Description = e.ExpenseType?.Name ?? "\u0645\u0635\u0631\u0648\u0641", Outgoing = e.Amount, AccountName = e.CashBox?.Name ?? "\u2014" });

        rows = rows.OrderBy(r => r.Date).ToList();
        decimal bal = 0;
        foreach (var r in rows) { bal += r.Incoming - r.Outgoing; r.Balance = bal; }

        var totalIn = rows.Sum(r => r.Incoming);
        var totalOut = rows.Sum(r => r.Outgoing);
        var currentBal = cashBoxId.HasValue
            ? (await context.CashBoxes.FindAsync(cashBoxId.Value))?.Balance ?? 0
            : (await context.CashBoxes.ToListAsync()).Sum(c => c.Balance);

        return new CashFlowResult
        {
            TotalIncoming = totalIn, TotalOutgoing = totalOut,
            NetFlow = totalIn - totalOut, CurrentBalance = currentBal,
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
        var endOfDay = date.Date.AddDays(1).AddTicks(-1);

        decimal capital = await context.CapitalEntries
            .Where(c => c.Type == CapitalEntryType.Initial && c.Date <= endOfDay)
            .SumAsync(c => c.Amount);

        decimal adjustments = await context.CapitalEntries
            .Where(c => c.Type == CapitalEntryType.Adjustment && c.Date <= endOfDay)
            .SumAsync(c => c.Amount);

        decimal profitOpening = await CloudProductCostHelper.GetProfitOpeningBalanceAsync(context, endOfDay);

        var salesQ = CloudInvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans)
            .Where(i => i.Date <= endOfDay);
        decimal totalSales = await salesQ.SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        decimal costOfSales = await CalculateCogsAsync(context, null, endOfDay.AddTicks(1));
        decimal totalExpenses = await context.Expenses
            .Where(e => e.Date <= endOfDay)
            .SumAsync(e => (decimal?)e.Amount) ?? 0;

        decimal salesProfit = totalSales - costOfSales;
        decimal accumulatedProfits = profitOpening + salesProfit - totalExpenses;
        decimal equityTotal = capital + adjustments + accumulatedProfits;

        // LIABILITIES
        decimal supplierCreditPurchases = await context.Invoices
            .Where(i => i.SupplierId != null &&
                        i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Date <= endOfDay)
            .SumAsync(i => i.NetAmount);
        var supplierIds = await context.Suppliers.Select(s => s.Id).ToListAsync();
        decimal supplierPaymentVouchers = await context.Vouchers
            .Where(v => v.VoucherType == VoucherType.Payment &&
                        v.CustomerId != null &&
                        supplierIds.Contains(v.CustomerId.Value) &&
                        v.Date <= endOfDay)
            .SumAsync(v => v.Amount);
        decimal supplierPayables = Math.Max(0, supplierCreditPurchases - supplierPaymentVouchers);

        decimal investorDeposits = await context.InvestorTransactions
            .Where(t => t.Type == InvestorTransactionType.Deposit && t.Date <= endOfDay)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        decimal investorWithdrawals = await context.InvestorTransactions
            .Where(t => t.Type == InvestorTransactionType.Withdrawal && t.Date <= endOfDay)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        investorDeposits = Math.Max(0, investorDeposits - investorWithdrawals);

        decimal liabilitiesTotal = supplierPayables + investorDeposits;
        decimal equityAndLiabilitiesTotal = equityTotal + liabilitiesTotal;

        // ASSETS
        var cashBoxes = await context.CashBoxes.ToListAsync();
        var cashBoxRows = cashBoxes.Select(c => new BalanceSheetCashBoxRow { Name = c.Name, Balance = c.Balance }).ToList();
        decimal cashBoxesTotal = cashBoxRows.Sum(c => c.Balance);

        var banks = await context.BankAccounts.ToListAsync();
        var bankRows = banks.Select(b => new BalanceSheetBankRow { Name = b.Name, Balance = b.Balance }).ToList();
        decimal banksTotal = bankRows.Sum(b => b.Balance);

        decimal customerCreditInvoices = await context.Invoices
            .Where(i => i.CustomerId != null &&
                        (i.InvoiceType == InvoiceType.Sale) &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Date <= endOfDay)
            .SumAsync(i => i.NetAmount);
        decimal customerReceiptVouchers = await context.Vouchers
            .Where(v => v.CustomerId != null &&
                        (v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt) &&
                        v.Date <= endOfDay)
            .SumAsync(v => v.Amount);
        decimal customerDebts = Math.Max(0, customerCreditInvoices - customerReceiptVouchers);

        var stocks = await context.WarehouseStocks
            .Include(ws => ws.Product)
            .ToListAsync();
        decimal inventoryValue = 0;
        foreach (var s in stocks)
        {
            if (s.Quantity <= 0) continue;

            var purchaseItems = await context.InvoiceItems
                .Include(ii => ii.Invoice)
                .Where(ii => ii.ProductId == s.ProductId &&
                             ii.Invoice!.InvoiceType == InvoiceType.Purchase)
                .ToListAsync();

            var avgCost = CloudProductCostHelper.ComputeAverageUnitCost(purchaseItems, s.OpeningQuantity, s.UnitCost);
            if (avgCost > 0)
                inventoryValue += Math.Round(s.Quantity * avgCost, 0);
        }

        decimal installmentReceivables = await context.Installments
            .Where(i => i.RemainingAmount > 0)
            .SumAsync(i => i.RemainingAmount);

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
        var query = context.InvoiceItems
            .Include(ii => ii.Product)
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && ii.Invoice != null
                         && (ii.Invoice.InvoiceType == InvoiceType.Sale || ii.Invoice.InvoiceType == InvoiceType.Installment));

        if (from.HasValue) query = query.Where(ii => ii.Invoice!.Date >= from.Value);
        if (to.HasValue) query = query.Where(ii => ii.Invoice!.Date < EndOfDay(to));
        if (warehouseId.HasValue) query = query.Where(ii => ii.Invoice!.WarehouseId == warehouseId.Value);

        var items = await query.ToListAsync();
        var grouped = items
            .GroupBy(ii => ii.ProductId!.Value)
            .Select(g => new TopProductRow
            {
                ProductId = g.Key,
                ProductName = g.First().Product?.Name ?? g.First().ItemName,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice)
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
        var query = context.InvoiceItems
            .Include(ii => ii.Product)
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId != null
                         && ii.Invoice != null
                         && (ii.Invoice.InvoiceType == InvoiceType.Sale || ii.Invoice.InvoiceType == InvoiceType.Installment));

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

        var rows = new List<ProductProfitMarginRow>();
        foreach (var g in soldItems.GroupBy(ii => ii.ProductId!.Value))
        {
            var revenue = g.Sum(x => x.TotalPrice);
            var qty = g.Sum(x => x.Quantity);
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

    public async Task<InstallmentAgingReportResult> GetInstallmentAgingReportAsync(DateTime asOfDate, int? customerId)
    {
        var context = _db;
        var query = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Where(i => i.Status != InstallmentStatus.Paid && i.RemainingAmount > 0);

        if (customerId.HasValue)
            query = query.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);

        var insts = await query.OrderBy(i => i.DueDate).ToListAsync();
        var asOf = asOfDate.Date;

        static string ResolveBucket(DateTime dueDate, DateTime asOfDate)
        {
            if (dueDate.Date >= asOfDate)
                return "ØºÙŠØ± Ù…Ø³ØªØ­Ù‚";

            var days = (asOfDate - dueDate.Date).Days;
            return days switch
            {
                <= 30 => "1-30 ÙŠÙˆÙ…",
                <= 60 => "31-60 ÙŠÙˆÙ…",
                <= 90 => "61-90 ÙŠÙˆÙ…",
                _ => "+90 ÙŠÙˆÙ…"
            };
        }

        var rows = insts.Select(i =>
        {
            var days = i.DueDate.Date < asOf ? (asOf - i.DueDate.Date).Days : 0;
            return new InstallmentAgingRow
            {
                InstallmentId = i.Id,
                InvoiceId = i.InstallmentPlan?.InvoiceId ?? 0,
                CustomerName = i.InstallmentPlan?.Customer?.Name ?? "\u2014",
                Phone = i.InstallmentPlan?.Customer?.Phone ?? "\u2014",
                PlanNumber = i.InstallmentPlanId.ToString(),
                DueDate = i.DueDate,
                Amount = i.Amount,
                RemainingAmount = i.RemainingAmount,
                DaysOverdue = days,
                AgingBucket = ResolveBucket(i.DueDate.Date, asOf)
            };
        }).OrderByDescending(r => r.DaysOverdue).ThenBy(r => r.DueDate).ToList();

        var bucketOrder = new[] { "ØºÙŠØ± Ù…Ø³ØªØ­Ù‚", "1-30 ÙŠÙˆÙ…", "31-60 ÙŠÙˆÙ…", "61-90 ÙŠÙˆÙ…", "+90 ÙŠÙˆÙ…" };
        var buckets = bucketOrder.Select(name => new InstallmentAgingBucketSummary
        {
            BucketName = name,
            Count = rows.Count(r => r.AgingBucket == name),
            Amount = rows.Where(r => r.AgingBucket == name).Sum(r => r.RemainingAmount)
        }).ToList();

        return new InstallmentAgingReportResult
        {
            TotalOutstanding = rows.Sum(r => r.RemainingAmount),
            InstallmentCount = rows.Count,
            CustomerCount = rows.Select(r => r.CustomerName).Distinct().Count(),
            Buckets = buckets,
            Rows = rows
        };
    }

    public async Task<CustomersOverviewReportResult> GetCustomersOverviewReportAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var customers = await context.Customers.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var rows = new List<CustomerOverviewRow>();

        foreach (var customer in customers)
        {
            var invQ = context.Invoices.AsNoTracking()
                .Where(i => i.CustomerId == customer.Id &&
                            (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment));
            if (from.HasValue) invQ = invQ.Where(i => i.Date >= from.Value);
            if (to.HasValue) invQ = invQ.Where(i => i.Date < EndOfDay(to));

            var invoices = await invQ.ToListAsync();
            var invoiceCount = invoices.Count;
            var salesAmount = invoices.Sum(i => i.NetAmount);

            var voucherQ = context.Vouchers.AsNoTracking()
                .Where(v => v.CustomerId == customer.Id &&
                            (v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt));
            if (from.HasValue) voucherQ = voucherQ.Where(v => v.Date >= from.Value);
            if (to.HasValue) voucherQ = voucherQ.Where(v => v.Date < EndOfDay(to));
            var collected = await voucherQ.SumAsync(v => (decimal?)v.Amount) ?? 0m;

            var planIds = await context.InstallmentPlans.AsNoTracking()
                .Where(p => p.CustomerId == customer.Id)
                .Select(p => p.Id)
                .ToListAsync();
            if (planIds.Count > 0)
            {
                var instQ = context.Installments.AsNoTracking()
                    .Where(i => planIds.Contains(i.InstallmentPlanId) && i.PaidAmount > 0);
                if (from.HasValue) instQ = instQ.Where(i => (i.PaymentDate ?? i.DueDate) >= from.Value);
                if (to.HasValue) instQ = instQ.Where(i => (i.PaymentDate ?? i.DueDate) < EndOfDay(to));
                collected += await instQ.SumAsync(i => (decimal?)i.PaidAmount) ?? 0m;
            }

            var outstanding = await context.Invoices.AsNoTracking()
                .Where(i => i.CustomerId == customer.Id && i.RemainingAmount > 0)
                .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;

            if (invoiceCount == 0 && collected == 0 && outstanding == 0)
                continue;

            rows.Add(new CustomerOverviewRow
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                Phone = customer.Phone ?? "-",
                InvoiceCount = invoiceCount,
                SalesAmount = salesAmount,
                CollectedAmount = collected,
                OutstandingBalance = outstanding
            });
        }

        return new CustomersOverviewReportResult
        {
            TotalSales = rows.Sum(r => r.SalesAmount),
            TotalCollected = rows.Sum(r => r.CollectedAmount),
            TotalOutstanding = rows.Sum(r => r.OutstandingBalance),
            CustomerCount = rows.Count,
            Rows = rows.OrderByDescending(r => r.OutstandingBalance).ThenByDescending(r => r.SalesAmount).ToList()
        };
    }

    public async Task<SuppliersOverviewReportResult> GetSuppliersOverviewReportAsync(DateTime? from, DateTime? to)
    {
        var context = _db;
        var suppliers = await context.Suppliers.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        var rows = new List<SupplierOverviewRow>();

        foreach (var supplier in suppliers)
        {
            var invQ = context.Invoices.AsNoTracking()
                .Where(i => i.SupplierId == supplier.Id && i.InvoiceType == InvoiceType.Purchase);
            if (from.HasValue) invQ = invQ.Where(i => i.Date >= from.Value);
            if (to.HasValue) invQ = invQ.Where(i => i.Date < EndOfDay(to));

            var invoices = await invQ.ToListAsync();
            var invoiceCount = invoices.Count;
            var purchaseAmount = invoices.Sum(i => i.NetAmount);

            var voucherQ = context.Vouchers.AsNoTracking()
                .Where(v => v.CustomerId == supplier.Id && v.VoucherType == VoucherType.Payment);
            if (from.HasValue) voucherQ = voucherQ.Where(v => v.Date >= from.Value);
            if (to.HasValue) voucherQ = voucherQ.Where(v => v.Date < EndOfDay(to));
            var paid = await voucherQ.SumAsync(v => (decimal?)v.Amount) ?? 0m;

            paid += invoices.Where(i => i.PaymentMethod == PaymentMethod.Cash).Sum(i => i.NetAmount);

            var outstanding = await context.Invoices.AsNoTracking()
                .Where(i => i.SupplierId == supplier.Id && i.RemainingAmount > 0)
                .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0m;

            if (invoiceCount == 0 && paid == 0 && outstanding == 0)
                continue;

            rows.Add(new SupplierOverviewRow
            {
                SupplierId = supplier.Id,
                SupplierName = supplier.Name,
                Phone = supplier.Phone ?? "-",
                InvoiceCount = invoiceCount,
                PurchaseAmount = purchaseAmount,
                PaidAmount = paid,
                OutstandingBalance = outstanding
            });
        }

        return new SuppliersOverviewReportResult
        {
            TotalPurchases = rows.Sum(r => r.PurchaseAmount),
            TotalPaid = rows.Sum(r => r.PaidAmount),
            TotalOutstanding = rows.Sum(r => r.OutstandingBalance),
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
                  || inv.InvoiceType == InvoiceType.Sale
                  || inv.InvoiceType == InvoiceType.Installment
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
                decimal qtyIn = g.Where(x => x.inv.InvoiceType == InvoiceType.Purchase).Sum(x => x.ii.Quantity);
                decimal qtyOut = g.Where(x => x.inv.InvoiceType != InvoiceType.Purchase).Sum(x => x.ii.Quantity);
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

            var pi = await context.InvoiceItems.AsNoTracking()
                .Include(ii => ii.Invoice)
                .Where(ii => ii.ProductId == productId && ii.Invoice!.InvoiceType == InvoiceType.Purchase)
                .ToListAsync();
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
                             || ii.Invoice.InvoiceType == InvoiceType.Installment));
        if (from.HasValue) salesQ = salesQ.Where(ii => ii.Invoice!.Date >= from.Value);
        if (to.HasValue) salesQ = salesQ.Where(ii => ii.Invoice!.Date < EndOfDay(to));
        if (warehouseId.HasValue) salesQ = salesQ.Where(ii => ii.Invoice!.WarehouseId == warehouseId.Value);

        var salesItems = await salesQ.ToListAsync();
        var soldByKey = salesItems
            .GroupBy(ii => (ProductId: ii.ProductId!.Value, WarehouseId: ii.Invoice!.WarehouseId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var warehouses = await _db.Warehouses.AsNoTracking().ToDictionaryAsync(w => w.Id, w => w.Name);
        var products = await _db.Products.AsNoTracking()
            .Include(p => p.Category)
            .ToDictionaryAsync(p => p.Id);

        var rows = new List<InventoryReplenishmentRow>();
        var processedKeys = new HashSet<(int ProductId, int WarehouseId)>();

        foreach (var s in stocks)
        {
            var key = (s.ProductId, s.WarehouseId);
            processedKeys.Add(key);
            soldByKey.TryGetValue(key, out var soldQty);
            var row = await BuildReplenishmentRowAsync(s.ProductId, s.WarehouseId,
                s.Product?.Name, s.Warehouse?.Name, s.Product?.Category?.Name,
                s.Quantity, soldQty, minStock, s.OpeningQuantity, s.UnitCost, filter);
            if (row is not null)
                rows.Add(row);
        }

        foreach (var sale in soldByKey)
        {
            if (processedKeys.Contains(sale.Key))
                continue;

            products.TryGetValue(sale.Key.ProductId, out var product);
            warehouses.TryGetValue(sale.Key.WarehouseId, out var warehouseName);
            var row = await BuildReplenishmentRowAsync(sale.Key.ProductId, sale.Key.WarehouseId,
                product?.Name, warehouseName, product?.Category?.Name,
                0, sale.Value, minStock, 0, 0, filter);
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

    private async Task<InventoryReplenishmentRow?> BuildReplenishmentRowAsync(
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
        InventoryReplenishmentFilter filter)
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

        var purchaseItems = await _db.InvoiceItems.AsNoTracking()
            .Include(ii => ii.Invoice)
            .Where(ii => ii.ProductId == productId && ii.Invoice!.InvoiceType == InvoiceType.Purchase)
            .ToListAsync();
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


