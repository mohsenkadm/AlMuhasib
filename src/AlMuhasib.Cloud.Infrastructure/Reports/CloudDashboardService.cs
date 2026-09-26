using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Cloud.Infrastructure.Reports;

public sealed class CloudDashboardService : ICloudDashboardService
{
    private readonly CloudDbContext _db;
    private readonly ITenantContext _tenantContext;

    public CloudDashboardService(CloudDbContext db, ITenantContext tenantContext)
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

    public async Task<DashboardData> GetDashboardAsync(CancellationToken ct = default)
    {
        var tenantId = RequireTenantId();
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var thirtyDaysAgo = today.AddDays(-30);
        var data = new DashboardData();

        data.TodaySales = await CloudInvoiceFilters.SumSignedNetAsync(
            CloudInvoiceFilters.ForProfitAndSalesTotals(_db.Invoices.ForTenant(tenantId), _db.InstallmentPlans.ForTenant(tenantId))
                .Where(i => i.Date >= today && i.Date < tomorrow));

        data.TodayPurchases = await CloudInvoiceFilters.SumSignedNetAsync(
            CloudInvoiceFilters.ForPurchasesTotals(_db.Invoices.ForTenant(tenantId))
                .Where(i => i.Date >= today && i.Date < tomorrow));

        var totalSales = await CloudInvoiceFilters.SumSignedNetAsync(
            CloudInvoiceFilters.ForProfitAndSalesTotals(_db.Invoices.ForTenant(tenantId), _db.InstallmentPlans.ForTenant(tenantId)));
        var totalPurchases = await CloudInvoiceFilters.SumSignedNetAsync(
            CloudInvoiceFilters.ForPurchasesTotals(_db.Invoices.ForTenant(tenantId)));
        var openingStockRows = await _db.WarehouseStocks.ForTenant(tenantId).AsNoTracking()
            .Where(s => s.OpeningQuantity > 0)
            .Select(s => new { s.OpeningQuantity, s.UnitCost })
            .ToListAsync(ct);
        var openingStockValue = Math.Round(
            openingStockRows.Sum(s => s.OpeningQuantity * s.UnitCost), 0);
        var totalExpenses = await _db.Expenses.ForTenant(tenantId)
            .Where(e => e.Currency == AccountingCurrency.IQD)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0;
        var distributedProfits = await _db.ProfitDistributions.ForTenant(tenantId)
            .SumAsync(pd => (decimal?)pd.DistributedAmount, ct) ?? 0;
        var profitOpening = await CloudProductCostHelper.GetProfitOpeningBalanceAsync(_db);
        data.NetProfitSales = totalSales;
        data.NetProfitPurchases = totalPurchases;
        data.NetProfitOpeningStock = openingStockValue;
        data.NetProfitExpenses = totalExpenses;
        data.NetProfitDistributions = distributedProfits;
        data.NetProfitOpening = profitOpening;
        data.NetProfit = totalSales - totalPurchases - openingStockValue - totalExpenses - distributedProfits + profitOpening;

        data.OverdueInstallmentsCount = await _db.Installments.ForTenant(tenantId)
            .CountAsync(i => i.Status != InstallmentStatus.Paid
                             && i.DueDate < today
                             && i.InstallmentPlan.Invoice.Currency == AccountingCurrency.IQD, ct);

        data.InvestorBalance = await _db.Investors.ForTenant(tenantId).SumAsync(i => (decimal?)i.TotalDeposit, ct) ?? 0;
        data.InvestorOpeningTotal = await _db.Investors.ForTenant(tenantId).SumAsync(i => (decimal?)i.OpeningBalance, ct) ?? 0;
        data.InvestorDepositsTotal = await _db.InvestorTransactions.ForTenant(tenantId)
            .Where(t => t.Type == InvestorTransactionType.Deposit)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;
        data.InvestorWithdrawalsTotal = await _db.InvestorTransactions.ForTenant(tenantId)
            .Where(t => t.Type == InvestorTransactionType.Withdrawal)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0;

        data.UnpaidInstallmentsBalance = await _db.Installments.ForTenant(tenantId)
            .Where(i => i.Status != InstallmentStatus.Paid &&
                        i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD)
            .SumAsync(i => (decimal?)i.RemainingAmount, ct) ?? 0;

        var creditRemaining = await _db.Invoices.ForTenant(tenantId)
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
                        i.PaymentMethod == PaymentMethod.Credit && !i.IsCreditPaid &&
                        i.Currency == AccountingCurrency.IQD)
            .SumAsync(i => (decimal?)i.RemainingAmount, ct) ?? 0;
        var unappliedDebt = await _db.Vouchers.ForTenant(tenantId)
            .Where(v => v.VoucherType == VoucherType.DebtReceipt &&
                        v.Currency == AccountingCurrency.IQD &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount, ct) ?? 0;
        var unappliedReceipts = await _db.Vouchers.ForTenant(tenantId)
            .Where(v => v.VoucherType == VoucherType.Receipt &&
                        v.Currency == AccountingCurrency.IQD &&
                        !v.InvoiceId.HasValue &&
                        !v.InstallmentId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount, ct) ?? 0;
        data.CustomerCreditInvoiceRemaining = creditRemaining;
        data.CustomerCreditUnappliedDebt = unappliedDebt;
        data.CustomerCreditUnappliedReceipts = unappliedReceipts;
        data.CustomerCreditBalance = Math.Max(0, creditRemaining - unappliedDebt - unappliedReceipts);

        var supplierRemaining = await _db.Invoices.ForTenant(tenantId)
            .Where(i => i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Credit && !i.IsCreditPaid &&
                        i.Currency == AccountingCurrency.IQD)
            .SumAsync(i => (decimal?)i.RemainingAmount, ct) ?? 0;
        var unappliedPayments = await _db.Vouchers.ForTenant(tenantId)
            .Where(v => v.VoucherType == VoucherType.Payment &&
                        v.SupplierId != null &&
                        v.Currency == AccountingCurrency.IQD &&
                        !v.InvoiceId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount, ct) ?? 0;
        data.SupplierCreditInvoiceRemaining = supplierRemaining;
        data.SupplierCreditUnappliedPayments = unappliedPayments;
        data.SupplierCreditBalance = Math.Max(0, supplierRemaining - unappliedPayments);

        var salesRaw = await CloudInvoiceFilters.ForProfitAndSalesTotals(
                _db.Invoices.ForTenant(tenantId), _db.InstallmentPlans.ForTenant(tenantId))
            .Where(i => i.Date >= thirtyDaysAgo && i.Date < tomorrow)
            .Select(i => new { i.Date, i.InvoiceType, i.NetAmount })
            .ToListAsync(ct);

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

        var expensesRaw = await _db.Expenses.ForTenant(tenantId)
            .Include(e => e.ExpenseType)
            .Where(e => e.Currency == AccountingCurrency.IQD)
            .Select(e => new { ExpenseTypeName = e.ExpenseType.Name, e.Amount })
            .ToListAsync(ct);

        data.ExpenseDistribution = expensesRaw
            .GroupBy(e => e.ExpenseTypeName)
            .Select(g => new ExpenseCategoryShare { Category = g.Key, Amount = g.Sum(e => e.Amount) })
            .OrderByDescending(x => x.Amount)
            .Take(8)
            .ToList();

        var recentInvoices = await _db.Invoices.ForTenant(tenantId)
            .Where(i => i.Currency == AccountingCurrency.IQD)
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
            .ToListAsync(ct);

        var recentVouchers = await _db.Vouchers.ForTenant(tenantId)
            .Where(v => v.Currency == AccountingCurrency.IQD)
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
                    : v.SupplierId != null
                        ? (v.Supplier != null ? v.Supplier.Name : "-")
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

        var upcomingRaw = await _db.Installments.ForTenant(tenantId)
            .Where(i => i.Status != InstallmentStatus.Paid
                        && i.DueDate >= today
                        && i.InstallmentPlan.Invoice.Currency == AccountingCurrency.IQD)
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

        data.CashBoxes = await _db.CashBoxes.ForTenant(tenantId)
            .Select(c => new CashBoxSummary { Name = c.Name, Balance = c.Balance, Currency = c.Currency })
            .ToListAsync(ct);
        data.CashBalanceIqd = data.CashBoxes
            .Where(c => c.Currency == AccountingCurrency.IQD)
            .Sum(c => c.Balance);
        data.CashBalanceUsd = data.CashBoxes
            .Where(c => c.Currency == AccountingCurrency.USD)
            .Sum(c => c.Balance);

        var banks = await _db.BankAccounts.ForTenant(tenantId)
            .Select(b => new { b.Balance, b.Currency })
            .ToListAsync(ct);
        data.BankBalanceIqd = banks
            .Where(b => b.Currency == AccountingCurrency.IQD)
            .Sum(b => b.Balance);
        data.BankBalanceUsd = banks
            .Where(b => b.Currency == AccountingCurrency.USD)
            .Sum(b => b.Balance);
        data.BankBalance = data.BankBalanceIqd;

        var stockValues = await _db.WarehouseStocks.ForTenant(tenantId)
            .GroupBy(ws => ws.ProductId)
            .Select(g => new { TotalQty = g.Sum(ws => ws.Quantity), ProductId = g.Key })
            .ToListAsync(ct);

        if (stockValues.Count > 0)
        {
            var productIds = stockValues.Select(s => s.ProductId).ToList();
            var allStocks = await _db.WarehouseStocks.ForTenant(tenantId)
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
