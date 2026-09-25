using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICurrentUserService _currentUserService;

    public DashboardService(IDbContextFactory<AppDbContext> contextFactory, ICurrentUserService currentUserService)
    {
        _contextFactory = contextFactory;
        _currentUserService = currentUserService;
    }

    public async Task<DashboardData> GetDashboardDataAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var thirtyDaysAgo = today.AddDays(-30);

        var data = new DashboardData();

        // ── Summary cards ──────────────────────────────────────
        try
        {
            data.TodaySales = await InvoiceSignedSums.SumSignedNetAsync(
                InvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans)
                    .Where(i => i.Date >= today && i.Date < tomorrow));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard TodaySales error: {ex.Message}");
        }

        try
        {
            data.TodayPurchases = await InvoiceSignedSums.SumSignedNetAsync(
                InvoiceFilters.ForPurchasesTotals(context.Invoices)
                    .Where(i => i.Date >= today && i.Date < tomorrow));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard TodayPurchases error: {ex.Message}");
        }

        // Net profit = net sales − net purchases − opening stock − expenses − distributions + profit opening
        try
        {
            var totalSales = await InvoiceSignedSums.SumSignedNetAsync(
                InvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans));
            var totalPurchases = await InvoiceSignedSums.SumSignedNetAsync(
                InvoiceFilters.ForPurchasesTotals(context.Invoices));
            var openingStockRows = await context.WarehouseStocks.AsNoTracking()
                .Where(s => s.OpeningQuantity > 0)
                .Select(s => new { s.OpeningQuantity, s.UnitCost })
                .ToListAsync();
            var openingStockValue = Math.Round(
                openingStockRows.Sum(s => s.OpeningQuantity * s.UnitCost), 0);
            var totalExpenses = await context.Expenses
                .SumAsync(e => (decimal?)e.Amount) ?? 0;
            var distributedProfits = await context.ProfitDistributions
                .SumAsync(pd => (decimal?)pd.DistributedAmount) ?? 0;
            var profitOpening = await ProductCostHelper.GetProfitOpeningBalanceAsync(context);
            data.NetProfitSales = totalSales;
            data.NetProfitPurchases = totalPurchases;
            data.NetProfitOpeningStock = openingStockValue;
            data.NetProfitExpenses = totalExpenses;
            data.NetProfitDistributions = distributedProfits;
            data.NetProfitOpening = profitOpening;
            data.NetProfit = totalSales - totalPurchases - openingStockValue - totalExpenses - distributedProfits + profitOpening;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard NetProfit error: {ex.Message}");
        }

        try
        {
            data.OverdueInstallmentsCount = await context.Installments
                .CountAsync(i => i.Status != InstallmentStatus.Paid && i.DueDate < today);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard OverdueInstallments error: {ex.Message}");
        }

        // ── Investor balance (total deposits) ──────────────────
        try
        {
            data.InvestorBalance = await context.Investors
                .SumAsync(i => (decimal?)i.TotalDeposit) ?? 0;
            data.InvestorOpeningTotal = await context.Investors
                .SumAsync(i => (decimal?)i.OpeningBalance) ?? 0;
            data.InvestorDepositsTotal = await context.InvestorTransactions
                .Where(t => t.Type == InvestorTransactionType.Deposit)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
            data.InvestorWithdrawalsTotal = await context.InvestorTransactions
                .Where(t => t.Type == InvestorTransactionType.Withdrawal)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard InvestorBalance error: {ex.Message}");
        }

        // ── Unpaid installments balance ────────────────────────
        try
        {
            data.UnpaidInstallmentsBalance = await context.Installments
                .Where(i => i.Status != InstallmentStatus.Paid)
                .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard UnpaidInstallmentsBalance error: {ex.Message}");
        }

        // ── Customer credit balance (آجل مبيعات/أقساط فقط) ────
        try
        {
            var creditRemaining = await context.Invoices
                .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
                            i.PaymentMethod == PaymentMethod.Credit && !i.IsCreditPaid)
                .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
            var unappliedDebt = await context.Vouchers
                .Where(v => v.VoucherType == VoucherType.DebtReceipt &&
                            (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
                .SumAsync(v => (decimal?)v.Amount) ?? 0;
            var unappliedReceipts = await context.Vouchers
                .Where(v => v.VoucherType == VoucherType.Receipt &&
                            !v.InvoiceId.HasValue &&
                            !v.InstallmentId.HasValue &&
                            (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
                .SumAsync(v => (decimal?)v.Amount) ?? 0;
            data.CustomerCreditInvoiceRemaining = creditRemaining;
            data.CustomerCreditUnappliedDebt = unappliedDebt;
            data.CustomerCreditUnappliedReceipts = unappliedReceipts;
            data.CustomerCreditBalance = Math.Max(0, creditRemaining - unappliedDebt - unappliedReceipts);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard CustomerCreditBalance error: {ex.Message}");
        }

        // ── Supplier credit balance (آجل مشتريات) ──────────────
        try
        {
            var supplierRemaining = await context.Invoices
                .Where(i => i.InvoiceType == InvoiceType.Purchase &&
                            i.PaymentMethod == PaymentMethod.Credit && !i.IsCreditPaid)
                .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
            var unappliedPayments = await context.Vouchers
                .Where(v => v.VoucherType == VoucherType.Payment &&
                            v.SupplierId != null &&
                            !v.InvoiceId.HasValue &&
                            (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
                .SumAsync(v => (decimal?)v.Amount) ?? 0;
            data.SupplierCreditInvoiceRemaining = supplierRemaining;
            data.SupplierCreditUnappliedPayments = unappliedPayments;
            data.SupplierCreditBalance = Math.Max(0, supplierRemaining - unappliedPayments);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard SupplierCreditBalance error: {ex.Message}");
        }

        // ── Sales last 30 days ─────────────────────────────────
        try
        {
            var salesRaw = await InvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans)
                .Where(i => i.Date >= thirtyDaysAgo && i.Date < tomorrow)
                .Select(i => new { i.Date, i.InvoiceType, i.NetAmount })
                .ToListAsync();

            var salesByDay = salesRaw
                .GroupBy(i => i.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(i => InvoiceFilters.SignedNetAmount(i.InvoiceType, i.NetAmount))
                })
                .ToList();

            data.SalesLast30Days = Enumerable.Range(0, 30)
                .Select(offset =>
                {
                    var d = thirtyDaysAgo.AddDays(offset);
                    var match = salesByDay.FirstOrDefault(s => s.Date == d);
                    return new DailySalesPoint { Date = d, Amount = match?.Amount ?? 0 };
                })
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard SalesLast30Days error: {ex.Message}");
        }

        // ── Expense distribution ───────────────────────────────
        try
        {
            var expensesRaw = await context.Expenses
                .Include(e => e.ExpenseType)
                .Select(e => new { ExpenseTypeName = e.ExpenseType.Name, e.Amount })
                .ToListAsync();

            data.ExpenseDistribution = expensesRaw
                .GroupBy(e => e.ExpenseTypeName)
                .Select(g => new ExpenseCategoryShare
                {
                    Category = g.Key,
                    Amount = g.Sum(e => e.Amount)
                })
                .OrderByDescending(x => x.Amount)
                .Take(8)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard ExpenseDistribution error: {ex.Message}");
        }

        // ── Recent transactions (last 10 invoices + vouchers) ──
        try
        {
            var recentInvoices = await context.Invoices
                .OrderByDescending(i => i.Date)
                .ThenByDescending(i => i.Id)
                .Take(5)
                .Select(i => new RecentTransaction
                {
                    Type = i.InvoiceType == InvoiceType.Sale ? "مبيعات"
                         : i.InvoiceType == InvoiceType.Purchase ? "مشتريات"
                         : i.InvoiceType == InvoiceType.SaleReturn ? "مرتجع مبيعات"
                         : i.InvoiceType == InvoiceType.PurchaseReturn ? "مرتجع مشتريات"
                         : i.InvoiceType == InvoiceType.Installment ? "أقساط"
                         : "فاتورة",
                    Number = i.InvoiceNumber,
                    Party = i.CustomerId != null
                        ? (i.Customer != null ? i.Customer.Name : "-")
                        : i.SupplierId != null
                            ? (i.Supplier != null ? i.Supplier.Name : "-")
                            : "-",
                    Amount = i.InvoiceType == InvoiceType.SaleReturn || i.InvoiceType == InvoiceType.PurchaseReturn
                        ? -i.NetAmount
                        : i.NetAmount,
                    Date = i.Date
                })
                .ToListAsync();

            var recentVouchers = await context.Vouchers
                .OrderByDescending(v => v.Date)
                .ThenByDescending(v => v.Id)
                .Take(5)
                .Select(v => new RecentTransaction
                {
                    Type = v.VoucherType == VoucherType.Receipt ? "سند قبض"
                         : v.VoucherType == VoucherType.Payment ? "سند صرف"
                         : "سند",
                    Number = v.VoucherNumber,
                    Party = v.CustomerId != null
                        ? (v.Customer != null ? v.Customer.Name : "-")
                        : v.InvestorId != null
                            ? (v.Investor != null ? v.Investor.Name : "-")
                            : "-",
                    Amount = v.Amount,
                    Date = v.Date
                })
                .ToListAsync();

            data.RecentTransactions = recentInvoices
                .Concat(recentVouchers)
                .OrderByDescending(t => t.Date)
                .Take(8)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard RecentTransactions error: {ex.Message}");
        }

        // ── Upcoming installments ──────────────────────────────
        try
        {
            var upcomingRaw = await context.Installments
                .Where(i => i.Status != InstallmentStatus.Paid && i.DueDate >= today)
                .OrderBy(i => i.DueDate)
                .Take(6)
                .Select(i => new
                {
                    CustomerName = i.InstallmentPlan.Customer.Name,
                    CustomerFileNumber = i.InstallmentPlan.Customer.FileNumber,
                    i.RemainingAmount,
                    i.DueDate
                })
                .ToListAsync();

            data.UpcomingInstallments = upcomingRaw
                .Select(i => new UpcomingInstallment
                {
                    CustomerName = i.CustomerName,
                    CustomerFileNumber = i.CustomerFileNumber,
                    Amount = i.RemainingAmount,
                    DueDate = i.DueDate,
                    DaysRemaining = (int)(i.DueDate - today).TotalDays
                })
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard UpcomingInstallments error: {ex.Message}");
        }

        // ── Bottom row ─────────────────────────────────────────
        try
        {
            data.CashBoxes = await context.CashBoxes
                .Select(c => new CashBoxSummary { Name = c.Name, Balance = c.Balance })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard CashBoxes error: {ex.Message}");
        }

        try
        {
            data.BankBalance = await context.BankAccounts
                .SumAsync(b => (decimal?)b.Balance) ?? 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard BankBalance error: {ex.Message}");
        }

        // Inventory value
        try
        {
            var stockValues = await context.WarehouseStocks
                .GroupBy(ws => ws.ProductId)
                .Select(g => new
                {
                    TotalQty = g.Sum(ws => ws.Quantity),
                    ProductId = g.Key
                })
                .ToListAsync();

            if (stockValues.Count > 0)
            {
                var productIds = stockValues.Select(s => s.ProductId).ToList();
                var allStocks = await context.WarehouseStocks
                    .Where(ws => productIds.Contains(ws.ProductId))
                    .ToListAsync();
                var purchasesByProduct = await ProductCostHelper.GetPurchaseItemsByProductAsync(context, productIds);

                data.TotalInventoryValue = stockValues.Sum(s =>
                {
                    var avg = ProductCostHelper.ComputeAverageUnitCostForProduct(
                        purchasesByProduct.GetValueOrDefault(s.ProductId) ?? [],
                        allStocks,
                        s.ProductId);
                    return s.TotalQty * avg;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard InventoryValue error: {ex.Message}");
        }

        // ── KPI sparkline trends (last 14 days) ───────────────
        try
        {
            await PopulateKpiTrendsAsync(context, data, today, tomorrow);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard KPI trends error: {ex.Message}");
        }

        return data;
    }

    private static async Task PopulateKpiTrendsAsync(
        AppDbContext context, DashboardData data, DateTime today, DateTime tomorrow)
    {
        const int days = 14;
        var from = today.AddDays(-(days - 1));
        var prevFrom = from.AddDays(-days);

        static List<DailySalesPoint> FillDays(DateTime fromDate, int dayCount, IReadOnlyDictionary<DateTime, decimal> byDay)
        {
            return Enumerable.Range(0, dayCount)
                .Select(offset =>
                {
                    var d = fromDate.AddDays(offset);
                    byDay.TryGetValue(d, out var amount);
                    return new DailySalesPoint { Date = d, Amount = amount };
                })
                .ToList();
        }

        static decimal TrendPercent(decimal currentPeriod, decimal previousPeriod)
        {
            if (previousPeriod == 0)
                return currentPeriod == 0 ? 0 : 100;
            return Math.Round((currentPeriod - previousPeriod) / Math.Abs(previousPeriod) * 100m, 1);
        }

        // Sales last 14 (slice from 30-day series when available)
        if (data.SalesLast30Days.Count >= days)
        {
            data.SalesLast30Days = data.SalesLast30Days; // keep full for main chart
        }

        var sales14 = data.SalesLast30Days
            .Where(p => p.Date >= from && p.Date < tomorrow)
            .ToList();
        if (sales14.Count < days)
        {
            var salesRaw = await InvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans)
                .Where(i => i.Date >= from && i.Date < tomorrow)
                .Select(i => new { i.Date, i.InvoiceType, i.NetAmount })
                .ToListAsync();
            var salesByDay = salesRaw
                .GroupBy(i => i.Date.Date)
                .ToDictionary(g => g.Key, g => g.Sum(i => InvoiceFilters.SignedNetAmount(i.InvoiceType, i.NetAmount)));
            sales14 = FillDays(from, days, salesByDay);
        }
        else
        {
            sales14 = FillDays(from, days, sales14.ToDictionary(p => p.Date.Date, p => p.Amount));
        }

        var salesPrev = await InvoiceSignedSums.SumSignedNetAsync(
            InvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans)
                .Where(i => i.Date >= prevFrom && i.Date < from));
        var salesCurr = sales14.Sum(p => p.Amount);
        data.TodaySalesTrendPercent = TrendPercent(salesCurr, salesPrev);

        // Purchases last 14
        var purchaseRaw = await InvoiceFilters.ForPurchasesTotals(context.Invoices)
            .Where(i => i.Date >= from && i.Date < tomorrow)
            .Select(i => new { i.Date, i.InvoiceType, i.NetAmount })
            .ToListAsync();
        var purchasesByDay = purchaseRaw
            .GroupBy(i => i.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(i => InvoiceFilters.SignedNetAmount(i.InvoiceType, i.NetAmount)));
        data.PurchasesLast14Days = FillDays(from, days, purchasesByDay);
        var purchasesPrev = await InvoiceSignedSums.SumSignedNetAsync(
            InvoiceFilters.ForPurchasesTotals(context.Invoices)
                .Where(i => i.Date >= prevFrom && i.Date < from));
        data.TodayPurchasesTrendPercent = TrendPercent(data.PurchasesLast14Days.Sum(p => p.Amount), purchasesPrev);

        // Daily expenses
        var expenseRaw = await context.Expenses
            .Where(e => e.Date >= from && e.Date < tomorrow)
            .Select(e => new { e.Date, e.Amount })
            .ToListAsync();
        var expensesByDay = expenseRaw
            .GroupBy(e => e.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        data.NetProfitLast14Days = Enumerable.Range(0, days)
            .Select(offset =>
            {
                var d = from.AddDays(offset);
                purchasesByDay.TryGetValue(d, out var purch);
                expensesByDay.TryGetValue(d, out var exp);
                var sale = sales14.FirstOrDefault(s => s.Date == d)?.Amount ?? 0;
                return new DailySalesPoint { Date = d, Amount = sale - purch - exp };
            })
            .ToList();

        var expensesPrev = await context.Expenses
            .Where(e => e.Date >= prevFrom && e.Date < from)
            .SumAsync(e => (decimal?)e.Amount) ?? 0;
        var profitCurr = data.NetProfitLast14Days.Sum(p => p.Amount);
        var profitPrev = salesPrev - purchasesPrev - expensesPrev;
        data.NetProfitTrendPercent = TrendPercent(profitCurr, profitPrev);

        // Overdue installment counts by due date (snapshot of currently unpaid)
        var overdueRaw = await context.Installments
            .Where(i => i.Status != InstallmentStatus.Paid && i.DueDate < tomorrow && i.DueDate >= from)
            .Select(i => new { i.DueDate, i.RemainingAmount })
            .ToListAsync();
        var overdueByDay = overdueRaw
            .GroupBy(i => i.DueDate.Date)
            .ToDictionary(g => g.Key, g => (decimal)g.Count());
        data.OverdueInstallmentsLast14Days = FillDays(from, days, overdueByDay);
        var overduePrevCount = await context.Installments
            .CountAsync(i => i.Status != InstallmentStatus.Paid && i.DueDate >= prevFrom && i.DueDate < from);
        data.OverdueInstallmentsTrendPercent = TrendPercent(
            data.OverdueInstallmentsLast14Days.Sum(p => p.Amount), overduePrevCount);

        // Investor net daily flow (deposit - withdrawal)
        var invTx = await context.InvestorTransactions
            .Where(t => t.Date >= from && t.Date < tomorrow)
            .Select(t => new { t.Date, t.Type, t.Amount })
            .ToListAsync();
        var invByDay = invTx
            .GroupBy(t => t.Date.Date)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(t => t.Type == InvestorTransactionType.Deposit ? t.Amount
                    : t.Type == InvestorTransactionType.Withdrawal ? -t.Amount
                    : 0));
        // Reconstruct approximate running balance ending at current InvestorBalance
        var flowSeries = FillDays(from, days, invByDay);
        var flowSum = flowSeries.Sum(p => p.Amount);
        var startBalance = data.InvestorBalance - flowSum;
        var running = startBalance;
        data.InvestorBalanceLast14Days = flowSeries.Select(p =>
        {
            running += p.Amount;
            return new DailySalesPoint { Date = p.Date, Amount = running };
        }).ToList();
        var invPrevTx = await context.InvestorTransactions
            .Where(t => t.Date >= prevFrom && t.Date < from)
            .Select(t => new { t.Type, t.Amount })
            .ToListAsync();
        var invPrevFlow = invPrevTx.Sum(t => t.Type == InvestorTransactionType.Deposit ? t.Amount
            : t.Type == InvestorTransactionType.Withdrawal ? -t.Amount
            : 0);
        data.InvestorBalanceTrendPercent = TrendPercent(flowSum, invPrevFlow);

        // Unpaid installment remaining by due date in window
        var unpaidRaw = await context.Installments
            .Where(i => i.Status != InstallmentStatus.Paid && i.DueDate >= from && i.DueDate < tomorrow)
            .Select(i => new { i.DueDate, i.RemainingAmount })
            .ToListAsync();
        var unpaidByDay = unpaidRaw
            .GroupBy(i => i.DueDate.Date)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.RemainingAmount));
        data.UnpaidInstallmentsLast14Days = FillDays(from, days, unpaidByDay);
        var unpaidPrev = await context.Installments
            .Where(i => i.Status != InstallmentStatus.Paid && i.DueDate >= prevFrom && i.DueDate < from)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        data.UnpaidInstallmentsTrendPercent = TrendPercent(
            data.UnpaidInstallmentsLast14Days.Sum(p => p.Amount), unpaidPrev);

        // Customer credit invoices created per day
        var custCreditRaw = await context.Invoices
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Date >= from && i.Date < tomorrow)
            .Select(i => new { i.Date, i.RemainingAmount })
            .ToListAsync();
        var custByDay = custCreditRaw
            .GroupBy(i => i.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.RemainingAmount));
        data.CustomerCreditLast14Days = FillDays(from, days, custByDay);
        var custPrev = await context.Invoices
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Date >= prevFrom && i.Date < from)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        data.CustomerCreditTrendPercent = TrendPercent(
            data.CustomerCreditLast14Days.Sum(p => p.Amount), custPrev);

        // Supplier credit
        var suppCreditRaw = await context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Date >= from && i.Date < tomorrow)
            .Select(i => new { i.Date, i.RemainingAmount })
            .ToListAsync();
        var suppByDay = suppCreditRaw
            .GroupBy(i => i.Date.Date)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.RemainingAmount));
        data.SupplierCreditLast14Days = FillDays(from, days, suppByDay);
        var suppPrev = await context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Date >= prevFrom && i.Date < from)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        data.SupplierCreditTrendPercent = TrendPercent(
            data.SupplierCreditLast14Days.Sum(p => p.Amount), suppPrev);

        // Cash flow from vouchers linked to cash boxes (receipt +, payment -)
        var cashVouchers = await context.Vouchers
            .Where(v => v.BankAccountId == null && v.Date >= from && v.Date < tomorrow)
            .Select(v => new { v.Date, v.VoucherType, v.Amount })
            .ToListAsync();
        var cashByDay = cashVouchers
            .GroupBy(v => v.Date.Date)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(v => v.VoucherType == VoucherType.Receipt ? v.Amount
                    : v.VoucherType == VoucherType.Payment ? -v.Amount
                    : 0));
        data.CashFlowLast14Days = FillDays(from, days, cashByDay);
        var cashPrevRaw = await context.Vouchers
            .Where(v => v.BankAccountId == null && v.Date >= prevFrom && v.Date < from)
            .Select(v => new { v.VoucherType, v.Amount })
            .ToListAsync();
        var cashPrev = cashPrevRaw.Sum(v => v.VoucherType == VoucherType.Receipt ? v.Amount
            : v.VoucherType == VoucherType.Payment ? -v.Amount
            : 0);
        data.CashBalanceTrendPercent = TrendPercent(data.CashFlowLast14Days.Sum(p => p.Amount), cashPrev);

        // Bank flow
        var bankVouchers = await context.Vouchers
            .Where(v => v.BankAccountId != null && v.Date >= from && v.Date < tomorrow)
            .Select(v => new { v.Date, v.VoucherType, v.Amount })
            .ToListAsync();
        var bankByDay = bankVouchers
            .GroupBy(v => v.Date.Date)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(v => v.VoucherType == VoucherType.Receipt ? v.Amount
                    : v.VoucherType == VoucherType.Payment ? -v.Amount
                    : 0));
        data.BankFlowLast14Days = FillDays(from, days, bankByDay);
        var bankPrevRaw = await context.Vouchers
            .Where(v => v.BankAccountId != null && v.Date >= prevFrom && v.Date < from)
            .Select(v => new { v.VoucherType, v.Amount })
            .ToListAsync();
        var bankPrev = bankPrevRaw.Sum(v => v.VoucherType == VoucherType.Receipt ? v.Amount
            : v.VoucherType == VoucherType.Payment ? -v.Amount
            : 0);
        data.BankBalanceTrendPercent = TrendPercent(data.BankFlowLast14Days.Sum(p => p.Amount), bankPrev);

        // Inventory proxy: purchase net amounts per day (stock inflow value)
        data.InventoryValueLast14Days = data.PurchasesLast14Days
            .Select(p => new DailySalesPoint { Date = p.Date, Amount = p.Amount })
            .ToList();
        data.InventoryValueTrendPercent = data.TodayPurchasesTrendPercent;
    }
}
