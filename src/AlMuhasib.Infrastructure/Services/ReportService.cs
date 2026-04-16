using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ReportService(IDbContextFactory<AppDbContext> contextFactory) => _contextFactory = contextFactory;

    // ══════════════════════════════════════════════════════════════
    // SALES
    // ══════════════════════════════════════════════════════════════

    public async Task<SalesReportResult> GetSalesReportAsync(DateTime? from, DateTime? to, int? customerId, PaymentMethod? method, int? warehouseId = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Invoices
            .Include(i => i.Customer)
            .Include(i => i.Warehouse)
            .Where(i => i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment);

        if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
        if (to.HasValue) query = query.Where(i => i.Date <= to.Value);
        if (customerId.HasValue) query = query.Where(i => i.CustomerId == customerId.Value);
        if (method.HasValue) query = query.Where(i => i.PaymentMethod == method.Value);
        if (warehouseId.HasValue) query = query.Where(i => i.WarehouseId == warehouseId.Value);

        var invoices = await query.OrderByDescending(i => i.Date).ToListAsync();

        var todaySales = invoices.Where(i => i.Date.Date == DateTime.Today).Sum(i => i.NetAmount);

        return new SalesReportResult
        {
            TotalSales = invoices.Sum(i => i.NetAmount),
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
                    Core.Enums.PaymentMethod.Cash => "\u0646\u0642\u062f\u064a",
                    Core.Enums.PaymentMethod.Credit => "\u0622\u062c\u0644",
                    Core.Enums.PaymentMethod.Installment => "\u0623\u0642\u0633\u0627\u0637",
                    _ => "\u2014"
                },
                TotalAmount = i.TotalAmount,
                Discount = i.DiscountAmount,
                NetAmount = i.NetAmount,
                CreditDueDate = i.CreditDueDate
            }).ToList()
        };
    }

    // ══════════════════════════════════════════════════════════════
    // PURCHASES
    // ══════════════════════════════════════════════════════════════

    public async Task<PurchasesReportResult> GetPurchasesReportAsync(DateTime? from, DateTime? to, int? supplierId, int? warehouseId, PaymentMethod? method = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Invoices
            .Include(i => i.Supplier)
            .Include(i => i.Warehouse)
            .Where(i => i.InvoiceType == InvoiceType.Purchase);

        if (from.HasValue) query = query.Where(i => i.Date >= from.Value);
        if (to.HasValue) query = query.Where(i => i.Date <= to.Value);
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
                    Core.Enums.PaymentMethod.Cash => "\u0646\u0642\u062f\u064a",
                    Core.Enums.PaymentMethod.Credit => "\u0622\u062c\u0644",
                    _ => "\u2014"
                },
                TotalAmount = i.TotalAmount,
                Discount = i.DiscountAmount,
                NetAmount = i.NetAmount
            }).ToList()
        };
    }

    // ══════════════════════════════════════════════════════════════
    // PROFIT
    // ══════════════════════════════════════════════════════════════

    public async Task<ProfitReportResult> GetProfitReportAsync(DateTime? from, DateTime? to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var salesQ = context.Invoices.Where(i => i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment);
        var purchQ = context.Invoices.Where(i => i.InvoiceType == InvoiceType.Purchase);
        var expQ = context.Expenses.AsQueryable();
        var bankQ = context.Vouchers.Where(v => v.VoucherType == VoucherType.BankReceipt);
        var distQ = context.ProfitDistributions.AsQueryable();

        if (from.HasValue) { salesQ = salesQ.Where(i => i.Date >= from.Value); purchQ = purchQ.Where(i => i.Date >= from.Value); expQ = expQ.Where(e => e.Date >= from.Value); bankQ = bankQ.Where(v => v.Date >= from.Value); distQ = distQ.Where(p => p.Date >= from.Value); }
        if (to.HasValue) { salesQ = salesQ.Where(i => i.Date <= to.Value); purchQ = purchQ.Where(i => i.Date <= to.Value); expQ = expQ.Where(e => e.Date <= to.Value); bankQ = bankQ.Where(v => v.Date <= to.Value); distQ = distQ.Where(p => p.Date <= to.Value); }

        var totalSales = await salesQ.SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var totalPurchases = await purchQ.SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var totalExpenses = await expQ.SumAsync(e => (decimal?)e.Amount) ?? 0;
        var totalBankFees = await bankQ.SumAsync(v => (decimal?)v.BankFees) ?? 0;
        var distributed = await distQ.SumAsync(p => (decimal?)p.DistributedAmount) ?? 0;
        var grossProfit = totalSales - totalPurchases;
        var netProfit = grossProfit - totalExpenses - totalBankFees - distributed;

        return new ProfitReportResult
        {
            TotalSales = totalSales, TotalPurchases = totalPurchases, GrossProfit = grossProfit,
            TotalExpenses = totalExpenses, TotalBankFees = totalBankFees,
            DistributedProfits = distributed, NetProfit = netProfit,
            ProfitMargin = totalSales > 0 ? Math.Round(netProfit / totalSales * 100, 1) : 0
        };
    }

    public async Task<List<MonthlyProfitRow>> GetMonthlyProfitAsync(DateTime? from, DateTime? to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var f = from ?? DateTime.Today.AddMonths(-12);
        var t = to ?? DateTime.Today;

        var sales = await context.Invoices
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) && i.Date >= f && i.Date <= t)
            .GroupBy(i => new { i.Date.Year, i.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(i => i.NetAmount) }).ToListAsync();

        var purchases = await context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase && i.Date >= f && i.Date <= t)
            .GroupBy(i => new { i.Date.Year, i.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(i => i.NetAmount) }).ToListAsync();

        var expenses = await context.Expenses
            .Where(e => e.Date >= f && e.Date <= t)
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(e => e.Amount) }).ToListAsync();

        var result = new List<MonthlyProfitRow>();
        for (var d = new DateTime(f.Year, f.Month, 1); d <= t; d = d.AddMonths(1))
        {
            var s = sales.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Amount ?? 0;
            var p = purchases.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Amount ?? 0;
            var e = expenses.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Amount ?? 0;
            var gross = s - p;
            var net = gross - e;
            result.Add(new MonthlyProfitRow
            {
                Month = $"{d.Year}/{d.Month:D2}",
                Sales = s, Purchases = p, GrossProfit = gross,
                Expenses = e, NetProfit = net,
                ProfitMargin = s > 0 ? Math.Round(net / s * 100, 1) : 0
            });
        }
        return result;
    }

    // ══════════════════════════════════════════════════════════════
    // INSTALLMENTS
    // ══════════════════════════════════════════════════════════════

    public async Task<InstallmentsSummaryResult> GetInstallmentsSummaryAsync(DateTime? from, DateTime? to, int? customerId, string? status)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var plansQ = context.InstallmentPlans.Include(p => p.Customer).Include(p => p.Installments).AsQueryable();
        if (customerId.HasValue) plansQ = plansQ.Where(p => p.CustomerId == customerId.Value);
        var plans = await plansQ.ToListAsync();

        var rows = new List<InstallmentSummaryRow>();
        foreach (var plan in plans)
        {
            var insts = plan.Installments.Where(i => !i.IsDeleted).ToList();
            if (from.HasValue) insts = insts.Where(i => i.DueDate >= from.Value).ToList();
            if (to.HasValue) insts = insts.Where(i => i.DueDate <= to.Value).ToList();
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
                new() { Name = "\u0645\u0633\u062f\u062f", Amount = paidInsts.Count() },
                new() { Name = "\u062c\u0632\u0626\u064a", Amount = allInsts.Count(i => i.Status == InstallmentStatus.PartiallyPaid) },
                new() { Name = "\u0645\u0639\u0644\u0642", Amount = allInsts.Count(i => i.Status == InstallmentStatus.Pending) },
                new() { Name = "\u0645\u062a\u0623\u062e\u0631", Amount = overdueInsts.Count() }
            ],
            MonthlyCollectionChart = allInsts.Where(i => i.PaymentDate.HasValue)
                .GroupBy(i => new DateTime(i.PaymentDate!.Value.Year, i.PaymentDate.Value.Month, 1))
                .Select(g => new DailyAmountPoint { Date = g.Key, Amount = g.Sum(i => i.PaidAmount) })
                .OrderBy(d => d.Date).ToList()
        };
    }

    public async Task<InstallmentDetailResult> GetInstallmentDetailAsync(int customerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
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
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Include(i => i.CashBox)
            .Where(i => i.Status == InstallmentStatus.Paid);

        if (from.HasValue) query = query.Where(i => i.PaymentDate >= from.Value);
        if (to.HasValue) query = query.Where(i => i.PaymentDate <= to.Value);
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
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Where(i => i.Status != InstallmentStatus.Paid);

        if (from.HasValue) query = query.Where(i => i.DueDate >= from.Value);
        if (to.HasValue) query = query.Where(i => i.DueDate <= to.Value);
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
        await using var context = await _contextFactory.CreateDbContextAsync();

        // ── 1. Overdue installments ────────────────────────────────
        var instQuery = context.Installments
            .Include(i => i.InstallmentPlan).ThenInclude(p => p.Customer)
            .Where(i => i.Status != InstallmentStatus.Paid && i.DueDate < asOfDate);

        if (customerId.HasValue) instQuery = instQuery.Where(i => i.InstallmentPlan.CustomerId == customerId.Value);

        var insts = await instQuery.OrderBy(i => i.DueDate).ToListAsync();
        if (minDaysOverdue.HasValue)
            insts = insts.Where(i => (asOfDate - i.DueDate).Days >= minDaysOverdue.Value).ToList();

        var rows = insts.Select(i => new OverdueRow
        {
            CustomerName = i.InstallmentPlan?.Customer?.Name ?? "\u2014",
            Phone = i.InstallmentPlan?.Customer?.Phone ?? "\u2014",
            PlanNumber = i.InstallmentPlanId.ToString(),
            OverdueAmount = i.RemainingAmount,
            OverdueDays = (asOfDate - i.DueDate).Days,
            LastPaymentDate = i.PaymentDate,
            InstallmentId = i.Id
        }).ToList();

        // ── 2. Overdue credit invoices (آجل) ──────────────────────
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
            CustomerName = i.Customer?.Name ?? "\u2014",
            Phone = i.Customer?.Phone ?? "\u2014",
            PlanNumber = i.InvoiceNumber,
            OverdueAmount = i.NetAmount,
            OverdueDays = (asOfDate.Date - i.CreditDueDate!.Value.Date).Days,
            LastPaymentDate = null,
            InstallmentId = 0
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

    // ══════════════════════════════════════════════════════════════
    // CUSTOMER STATEMENT
    // ══════════════════════════════════════════════════════════════

    public async Task<CustomerStatementResult> GetCustomerStatementAsync(int customerId, DateTime? from = null, DateTime? to = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var customer = await context.Customers.FindAsync(customerId);
        if (customer is null) return new CustomerStatementResult { CustomerName = "\u2014" };

        var rows = new List<CustomerStatementRow>();

        var invQ = context.Invoices
            .Where(i => i.CustomerId == customerId &&
                        (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
                        (i.PaymentMethod == PaymentMethod.Credit || i.PaymentMethod == PaymentMethod.Installment));
        if (from.HasValue) invQ = invQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) invQ = invQ.Where(i => i.Date <= to.Value);
        foreach (var inv in await invQ.OrderBy(i => i.Date).ToListAsync())
            rows.Add(new CustomerStatementRow { Date = inv.Date, Description = $"\u0641\u0627\u062a\u0648\u0631\u0629 \u0645\u0628\u064a\u0639\u0627\u062a {inv.InvoiceNumber}", Debit = inv.NetAmount });

        var vQ = context.Vouchers
            .Where(v => v.CustomerId == customerId && (v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt));
        if (from.HasValue) vQ = vQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) vQ = vQ.Where(v => v.Date <= to.Value);
        foreach (var v in await vQ.OrderBy(v => v.Date).ToListAsync())
            rows.Add(new CustomerStatementRow { Date = v.Date, Description = v.VoucherType == VoucherType.Receipt ? $"\u0633\u0646\u062f \u0642\u0628\u0636 {v.VoucherNumber}" : $"\u0633\u0646\u062f \u062a\u0633\u062f\u064a\u062f \u062f\u064a\u0646 {v.VoucherNumber}", Credit = v.Amount });

        var planIds = await context.InstallmentPlans.Where(p => p.CustomerId == customerId).Select(p => p.Id).ToListAsync();
        if (planIds.Count > 0)
        {
            var instQ = context.Installments.Where(i => planIds.Contains(i.InstallmentPlanId) && i.PaidAmount > 0);
            if (from.HasValue) instQ = instQ.Where(i => (i.PaymentDate ?? i.DueDate) >= from.Value);
            if (to.HasValue) instQ = instQ.Where(i => (i.PaymentDate ?? i.DueDate) <= to.Value);
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

    // ══════════════════════════════════════════════════════════════
    // SUPPLIER STATEMENT
    // ══════════════════════════════════════════════════════════════

    public async Task<SupplierStatementResult> GetSupplierStatementAsync(int supplierId, DateTime? from = null, DateTime? to = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var supplier = await context.Suppliers.FindAsync(supplierId);
        if (supplier is null) return new SupplierStatementResult { SupplierName = "\u2014" };

        var rows = new List<SupplierStatementRow>();

        var invQ = context.Invoices
            .Where(i => i.SupplierId == supplierId && i.InvoiceType == InvoiceType.Purchase && i.PaymentMethod == PaymentMethod.Credit);
        if (from.HasValue) invQ = invQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) invQ = invQ.Where(i => i.Date <= to.Value);
        foreach (var inv in await invQ.OrderBy(i => i.Date).ToListAsync())
            rows.Add(new SupplierStatementRow { Date = inv.Date, Description = $"\u0641\u0627\u062a\u0648\u0631\u0629 \u0645\u0634\u062a\u0631\u064a\u0627\u062a {inv.InvoiceNumber}", Credit = inv.NetAmount });

        var vQ = context.Vouchers.Where(v => v.CustomerId == supplierId && v.VoucherType == VoucherType.Payment);
        if (from.HasValue) vQ = vQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) vQ = vQ.Where(v => v.Date <= to.Value);
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

    // ══════════════════════════════════════════════════════════════
    // EXPENSES
    // ══════════════════════════════════════════════════════════════

    public async Task<ExpensesReportResult> GetExpensesReportAsync(DateTime? from, DateTime? to, int? expenseTypeId, int? cashBoxId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Expenses.Include(e => e.ExpenseType).Include(e => e.CashBox).AsQueryable();
        if (from.HasValue) query = query.Where(e => e.Date >= from.Value);
        if (to.HasValue) query = query.Where(e => e.Date <= to.Value);
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

    // ══════════════════════════════════════════════════════════════
    // INCOME & EXPENSE
    // ══════════════════════════════════════════════════════════════

    public async Task<IncomeExpenseResult> GetIncomeExpenseReportAsync(DateTime? from, DateTime? to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var rows = new List<IncomeExpenseRow>();

        var salesQ = context.Invoices.Where(i => i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment);
        var expQ = context.Expenses.Include(e => e.ExpenseType).AsQueryable();
        if (from.HasValue) { salesQ = salesQ.Where(i => i.Date >= from.Value); expQ = expQ.Where(e => e.Date >= from.Value); }
        if (to.HasValue) { salesQ = salesQ.Where(i => i.Date <= to.Value); expQ = expQ.Where(e => e.Date <= to.Value); }

        var totalSales = await salesQ.SumAsync(i => (decimal?)i.NetAmount) ?? 0;

        var instQ = context.Installments.Where(i => i.PaidAmount > 0);
        if (from.HasValue) instQ = instQ.Where(i => i.PaymentDate >= from.Value);
        if (to.HasValue) instQ = instQ.Where(i => i.PaymentDate <= to.Value);
        var instCollections = await instQ.SumAsync(i => (decimal?)i.PaidAmount) ?? 0;

        var recQ = context.Vouchers.Where(v => v.VoucherType == VoucherType.Receipt || v.VoucherType == VoucherType.DebtReceipt);
        if (from.HasValue) recQ = recQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) recQ = recQ.Where(v => v.Date <= to.Value);
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

    // ══════════════════════════════════════════════════════════════
    // WAREHOUSE
    // ══════════════════════════════════════════════════════════════

    public async Task<List<WarehouseStockRow>> GetWarehouseReportAsync(int? warehouseId, bool includeZero = false)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.WarehouseStocks.Include(ws => ws.Product).Include(ws => ws.Warehouse).AsQueryable();
        if (warehouseId.HasValue) query = query.Where(ws => ws.WarehouseId == warehouseId.Value);
        if (!includeZero) query = query.Where(ws => ws.Quantity > 0);

        var stocks = await query.OrderBy(ws => ws.Warehouse!.Name).ThenBy(ws => ws.Product!.Name).ToListAsync();
        var result = new List<WarehouseStockRow>();
        foreach (var s in stocks)
        {
            decimal avgCost = 0;
            var pi = await context.InvoiceItems.Include(ii => ii.Invoice)
                .Where(ii => ii.ProductId == s.ProductId && ii.Invoice!.InvoiceType == InvoiceType.Purchase).ToListAsync();
            if (pi.Count > 0) { var tc = pi.Sum(ii => ii.TotalPrice); var tq = pi.Sum(ii => ii.Quantity); if (tq > 0) avgCost = tc / tq; }

            result.Add(new WarehouseStockRow
            {
                ProductName = s.Product?.Name ?? "\u2014", WarehouseName = s.Warehouse?.Name ?? "\u2014",
                Quantity = s.Quantity, AverageCost = Math.Round(avgCost, 0), TotalValue = Math.Round(s.Quantity * avgCost, 0)
            });
        }
        return result;
    }

    // ══════════════════════════════════════════════════════════════
    // INVESTORS
    // ══════════════════════════════════════════════════════════════

    public async Task<InvestorsReportResult> GetInvestorsReportAsync(int? investorId, DateTime? from, DateTime? to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
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

    // ══════════════════════════════════════════════════════════════
    // CASH FLOW
    // ══════════════════════════════════════════════════════════════

    public async Task<CashFlowResult> GetCashFlowReportAsync(int? cashBoxId, DateTime? from, DateTime? to)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var rows = new List<CashFlowRow>();

        var salesQ = context.Invoices.Include(i => i.CashBox)
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) && i.PaymentMethod == PaymentMethod.Cash);
        if (cashBoxId.HasValue) salesQ = salesQ.Where(i => i.CashBoxId == cashBoxId.Value);
        if (from.HasValue) salesQ = salesQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) salesQ = salesQ.Where(i => i.Date <= to.Value);
        foreach (var inv in await salesQ.ToListAsync())
            rows.Add(new CashFlowRow { Date = inv.Date, Type = "\u0645\u0628\u064a\u0639\u0627\u062a", Description = $"\u0641\u0627\u062a\u0648\u0631\u0629 {inv.InvoiceNumber}", Incoming = inv.NetAmount, AccountName = inv.CashBox?.Name ?? "\u2014" });

        var purchQ = context.Invoices.Include(i => i.CashBox)
            .Where(i => i.InvoiceType == InvoiceType.Purchase && i.PaymentMethod == PaymentMethod.Cash);
        if (cashBoxId.HasValue) purchQ = purchQ.Where(i => i.CashBoxId == cashBoxId.Value);
        if (from.HasValue) purchQ = purchQ.Where(i => i.Date >= from.Value);
        if (to.HasValue) purchQ = purchQ.Where(i => i.Date <= to.Value);
        foreach (var inv in await purchQ.ToListAsync())
            rows.Add(new CashFlowRow { Date = inv.Date, Type = "\u0645\u0634\u062a\u0631\u064a\u0627\u062a", Description = $"\u0641\u0627\u062a\u0648\u0631\u0629 {inv.InvoiceNumber}", Outgoing = inv.NetAmount, AccountName = inv.CashBox?.Name ?? "\u2014" });

        var vouchQ = context.Vouchers.Include(v => v.CashBox).AsQueryable();
        if (cashBoxId.HasValue) vouchQ = vouchQ.Where(v => v.CashBoxId == cashBoxId.Value);
        if (from.HasValue) vouchQ = vouchQ.Where(v => v.Date >= from.Value);
        if (to.HasValue) vouchQ = vouchQ.Where(v => v.Date <= to.Value);
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
        if (to.HasValue) expQ = expQ.Where(e => e.Date <= to.Value);
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

    // ══════════════════════════════════════════════════════════════
    // BALANCE SHEET
    // ══════════════════════════════════════════════════════════════

    public async Task<BalanceSheetResult> GetBalanceSheetAsync(DateTime date)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var endOfDay = date.Date.AddDays(1).AddTicks(-1);

        decimal capital = await context.CapitalEntries
            .Where(c => c.Type == CapitalEntryType.Initial && c.Date <= endOfDay)
            .SumAsync(c => c.Amount);

        decimal adjustments = await context.CapitalEntries
            .Where(c => c.Type == CapitalEntryType.Adjustment && c.Date <= endOfDay)
            .SumAsync(c => c.Amount);

        decimal totalSales = await context.Invoices
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) && i.Date <= endOfDay)
            .SumAsync(i => i.NetAmount);
        decimal totalPurchases = await context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase && i.Date <= endOfDay)
            .SumAsync(i => i.NetAmount);
        decimal totalExpenses = await context.Expenses
            .Where(e => e.Date <= endOfDay)
            .SumAsync(e => e.Amount);
        decimal totalBankFees = await context.Vouchers
            .Where(v => v.VoucherType == VoucherType.BankReceipt && v.Date <= endOfDay)
            .SumAsync(v => v.BankFees);
        decimal distributedProfits = await context.ProfitDistributions
            .Where(p => p.Date <= endOfDay)
            .SumAsync(p => p.DistributedAmount);

        decimal accumulatedProfits = totalSales - totalPurchases - totalExpenses - totalBankFees - distributedProfits;
        decimal equityTotal = capital + adjustments + accumulatedProfits;

        // LIABILITIES
        decimal supplierCreditPurchases = await context.Invoices
            .Where(i => i.SupplierId != null &&
                        i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Date <= endOfDay)
            .SumAsync(i => i.NetAmount);
        decimal supplierPaymentVouchers = await context.Vouchers
            .Where(v => v.VoucherType == VoucherType.Payment &&
                        v.CustomerId != null &&
                        v.Date <= endOfDay)
            .SumAsync(v => v.Amount);
        decimal supplierPayables = Math.Max(0, supplierCreditPurchases - supplierPaymentVouchers);

        decimal investorDeposits = await context.Investors
            .SumAsync(i => i.TotalDeposit);

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

            if (purchaseItems.Count > 0)
            {
                decimal totalCost = purchaseItems.Sum(ii => ii.TotalPrice);
                decimal totalQty = purchaseItems.Sum(ii => ii.Quantity);
                decimal avgCost = totalQty > 0 ? totalCost / totalQty : 0;
                inventoryValue += Math.Round(s.Quantity * avgCost, 0);
            }
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
}
