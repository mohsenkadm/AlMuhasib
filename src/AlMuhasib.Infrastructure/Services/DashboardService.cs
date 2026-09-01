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
            data.TodaySales = await InvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans)
                .Where(i => i.Date >= today && i.Date < tomorrow)
                .SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard TodaySales error: {ex.Message}");
        }

        try
        {
            data.TodayPurchases = await InvoiceFilters.ForPurchasesTotals(context.Invoices)
                .Where(i => i.Date >= today && i.Date < tomorrow)
                .SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard TodayPurchases error: {ex.Message}");
        }

        // Net profit = total sales - total purchases - total expenses - distributed profits (all-time)
        try
        {
            var totalSales = await InvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans)
                .SumAsync(i => (decimal?)i.NetAmount) ?? 0;
            var totalPurchases = await InvoiceFilters.ForPurchasesTotals(context.Invoices)
                .SumAsync(i => (decimal?)i.NetAmount) ?? 0;
            var totalExpenses = await context.Expenses
                .SumAsync(e => (decimal?)e.Amount) ?? 0;
            var distributedProfits = await context.ProfitDistributions
                .SumAsync(pd => (decimal?)pd.DistributedAmount) ?? 0;
            var profitOpening = await ProductCostHelper.GetProfitOpeningBalanceAsync(context);
            data.NetProfit = totalSales - totalPurchases - totalExpenses - distributedProfits + profitOpening;
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

        // ── Customer credit balance (آجل) ─────────────────────
        try
        {
            var creditRemaining = await context.Invoices
                .Where(i => i.PaymentMethod == PaymentMethod.Credit && !i.IsCreditPaid)
                .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
            var unappliedDebt = await context.Vouchers
                .Where(v => v.VoucherType == VoucherType.DebtReceipt &&
                            (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
                .SumAsync(v => (decimal?)v.Amount) ?? 0;
            data.CustomerCreditBalance = Math.Max(0, creditRemaining - unappliedDebt);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Dashboard CustomerCreditBalance error: {ex.Message}");
        }

        // ── Sales last 30 days ─────────────────────────────────
        try
        {
            var salesRaw = await context.Invoices
                .Where(i => i.InvoiceType == InvoiceType.Sale && i.Date >= thirtyDaysAgo && i.Date < tomorrow)
                .Select(i => new { i.Date, i.NetAmount })
                .ToListAsync();

            var salesByDay = salesRaw
                .GroupBy(i => i.Date.Date)
                .Select(g => new { Date = g.Key, Amount = g.Sum(i => i.NetAmount) })
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
                         : "أقساط",
                    Number = i.InvoiceNumber,
                    Party = i.CustomerId != null
                        ? (i.Customer != null ? i.Customer.Name : "-")
                        : i.SupplierId != null
                            ? (i.Supplier != null ? i.Supplier.Name : "-")
                            : "-",
                    Amount = i.NetAmount,
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

        return data;
    }
}
