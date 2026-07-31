using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Reports;

public sealed class CloudDashboardService : ICloudDashboardService
{
    private readonly CloudDbContext _db;

    public CloudDashboardService(CloudDbContext db) => _db = db;

    public async Task<DashboardData> GetDashboardAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var thirtyDaysAgo = today.AddDays(-30);
        var data = new DashboardData();

        data.TodaySales = await _db.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Sale && i.Date >= today && i.Date < tomorrow)
            .SumAsync(i => (decimal?)i.NetAmount, ct) ?? 0;

        data.TodayPurchases = await _db.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase && i.Date >= today && i.Date < tomorrow)
            .SumAsync(i => (decimal?)i.NetAmount, ct) ?? 0;

        var totalSales = await CloudInvoiceFilters.ForProfitAndSalesTotals(_db.Invoices, _db.InstallmentPlans)
            .SumAsync(i => (decimal?)i.NetAmount, ct) ?? 0;
        var totalPurchases = await _db.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase)
            .SumAsync(i => (decimal?)i.NetAmount, ct) ?? 0;
        var totalExpenses = await _db.Expenses.SumAsync(e => (decimal?)e.Amount, ct) ?? 0;
        var distributedProfits = await _db.ProfitDistributions
            .SumAsync(pd => (decimal?)pd.DistributedAmount, ct) ?? 0;
        var profitOpening = await CloudProductCostHelper.GetProfitOpeningBalanceAsync(_db);
        data.NetProfit = totalSales - totalPurchases - totalExpenses - distributedProfits + profitOpening;

        data.OverdueInstallmentsCount = await _db.Installments
            .CountAsync(i => i.Status != InstallmentStatus.Paid && i.DueDate < today, ct);

        data.InvestorBalance = await _db.Investors.SumAsync(i => (decimal?)i.TotalDeposit, ct) ?? 0;

        data.UnpaidInstallmentsBalance = await _db.Installments
            .Where(i => i.Status != InstallmentStatus.Paid)
            .SumAsync(i => (decimal?)i.RemainingAmount, ct) ?? 0;

        data.CustomerCreditBalance = await _db.Invoices
            .Where(i => i.PaymentMethod == PaymentMethod.Credit && !i.IsCreditPaid)
            .SumAsync(i => (decimal?)i.RemainingAmount, ct) ?? 0;
        var unappliedDebt = await _db.Vouchers
            .Where(v => v.VoucherType == VoucherType.DebtReceipt &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount, ct) ?? 0;
        data.CustomerCreditBalance = Math.Max(0, data.CustomerCreditBalance - unappliedDebt);

        var salesRaw = await _db.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Sale && i.Date >= thirtyDaysAgo && i.Date < tomorrow)
            .Select(i => new { i.Date, i.NetAmount })
            .ToListAsync(ct);

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

        var expensesRaw = await _db.Expenses
            .Include(e => e.ExpenseType)
            .Select(e => new { ExpenseTypeName = e.ExpenseType.Name, e.Amount })
            .ToListAsync(ct);

        data.ExpenseDistribution = expensesRaw
            .GroupBy(e => e.ExpenseTypeName)
            .Select(g => new ExpenseCategoryShare { Category = g.Key, Amount = g.Sum(e => e.Amount) })
            .OrderByDescending(x => x.Amount)
            .Take(8)
            .ToList();

        var recentInvoices = await _db.Invoices
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
            .ToListAsync(ct);

        var recentVouchers = await _db.Vouchers
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
            .ToListAsync(ct);

        data.RecentTransactions = recentInvoices
            .Concat(recentVouchers)
            .OrderByDescending(t => t.Date)
            .Take(8)
            .ToList();

        var upcomingRaw = await _db.Installments
            .Where(i => i.Status != InstallmentStatus.Paid && i.DueDate >= today)
            .OrderBy(i => i.DueDate)
            .Take(6)
            .Select(i => new
            {
                CustomerName = i.InstallmentPlan.Customer.Name,
                i.RemainingAmount,
                i.DueDate
            })
            .ToListAsync(ct);

        data.UpcomingInstallments = upcomingRaw
            .Select(i => new UpcomingInstallment
            {
                CustomerName = i.CustomerName,
                Amount = i.RemainingAmount,
                DueDate = i.DueDate,
                DaysRemaining = (int)(i.DueDate - today).TotalDays
            })
            .ToList();

        data.CashBoxes = await _db.CashBoxes
            .Select(c => new CashBoxSummary { Name = c.Name, Balance = c.Balance })
            .ToListAsync(ct);

        data.BankBalance = await _db.BankAccounts.SumAsync(b => (decimal?)b.Balance, ct) ?? 0;

        var stockValues = await _db.WarehouseStocks
            .GroupBy(ws => ws.ProductId)
            .Select(g => new { TotalQty = g.Sum(ws => ws.Quantity), ProductId = g.Key })
            .ToListAsync(ct);

        if (stockValues.Count > 0)
        {
            var productIds = stockValues.Select(s => s.ProductId).ToList();
            var allStocks = await _db.WarehouseStocks
                .Where(ws => productIds.Contains(ws.ProductId))
                .ToListAsync(ct);
            var purchasesByProduct = await CloudProductCostHelper.GetPurchaseItemsByProductAsync(_db, productIds);

            data.TotalInventoryValue = stockValues.Sum(s =>
            {
                var avg = CloudProductCostHelper.ComputeAverageUnitCostForProduct(
                    purchasesByProduct.GetValueOrDefault(s.ProductId) ?? [],
                    allStocks,
                    s.ProductId);
                return s.TotalQty * avg;
            });
        }

        return data;
    }
}
