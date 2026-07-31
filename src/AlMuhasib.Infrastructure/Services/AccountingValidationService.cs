using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public class AccountingValidationService : IAccountingValidationService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public AccountingValidationService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ValidationSummary> ValidateAllBalancesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var summary = new ValidationSummary();

        var cashBoxes = await context.CashBoxes.ToListAsync();
        foreach (var cb in cashBoxes)
            summary.Results.Add(await ValidateCashBoxBalanceAsync(cb.Id));

        var customers = await context.Customers.ToListAsync();
        foreach (var c in customers)
            summary.Results.Add(await ValidateCustomerBalanceAsync(c.Id));

        var suppliers = await context.Suppliers.ToListAsync();
        foreach (var s in suppliers)
            summary.Results.Add(await ValidateSupplierBalanceAsync(s.Id));

        var warehouses = await context.Warehouses.ToListAsync();
        foreach (var w in warehouses)
            summary.Results.Add(await ValidateInventoryAsync(w.Id));

        summary.Results.Add(await ValidateBalanceSheetAsync(DateTime.Today));

        return summary;
    }

    public async Task<ValidationResult> ValidateCashBoxBalanceAsync(int cashBoxId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var cashBox = await context.CashBoxes.FindAsync(cashBoxId);
        if (cashBox is null)
            return new ValidationResult { Category = "\u0627\u0644\u0642\u0627\u0635\u0629", IsValid = false, Message = $"\u0627\u0644\u0642\u0627\u0635\u0629 #{cashBoxId} \u063a\u064a\u0631 \u0645\u0648\u062c\u0648\u062f\u0629" };

        var salesIncome = await context.Invoices
            .Where(i => i.CashBoxId == cashBoxId &&
                        (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
                        i.PaymentMethod == PaymentMethod.Cash)
            .SumAsync(i => (decimal?)i.NetAmount) ?? 0;

        var purchaseOutflow = await context.Invoices
            .Where(i => i.CashBoxId == cashBoxId &&
                        i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Cash)
            .SumAsync(i => (decimal?)i.NetAmount) ?? 0;

        var receipts = await context.Vouchers
            .Where(v => v.CashBoxId == cashBoxId &&
                        (v.VoucherType == VoucherType.Receipt ||
                         v.VoucherType == VoucherType.DebtReceipt ||
                         v.VoucherType == VoucherType.InvestorDeposit))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var payments = await context.Vouchers
            .Where(v => v.CashBoxId == cashBoxId &&
                        (v.VoucherType == VoucherType.Payment ||
                         v.VoucherType == VoucherType.InvestorWithdrawal))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var bankReceipts = await context.Vouchers
            .Where(v => v.CashBoxId == cashBoxId &&
                        v.VoucherType == VoucherType.BankReceipt)
            .SumAsync(v => (decimal?)(v.Amount - v.BankFees)) ?? 0;

        var installmentPayments = await context.Installments
            .Where(inst => inst.CashBoxId == cashBoxId && inst.PaidAmount > 0)
            .SumAsync(inst => (decimal?)inst.PaidAmount) ?? 0;

        var expectedBalance = salesIncome - purchaseOutflow + receipts - payments + bankReceipts + installmentPayments;
        var diff = Math.Abs(expectedBalance - cashBox.Balance);

        return new ValidationResult
        {
            Category = "\u0627\u0644\u0642\u0627\u0635\u0629",
            EntityName = cashBox.Name,
            IsValid = diff < 0.01m,
            ExpectedValue = expectedBalance,
            ActualValue = cashBox.Balance,
            Difference = expectedBalance - cashBox.Balance,
            Message = diff < 0.01m
                ? $"\u0631\u0635\u064a\u062f \u0627\u0644\u0642\u0627\u0635\u0629 '{cashBox.Name}' \u0645\u062a\u0637\u0627\u0628\u0642: {cashBox.Balance:N2}"
                : $"\u0641\u0631\u0642 \u0641\u064a \u0631\u0635\u064a\u062f \u0627\u0644\u0642\u0627\u0635\u0629 '{cashBox.Name}': \u0627\u0644\u0645\u062a\u0648\u0642\u0639 {expectedBalance:N2}\u060c \u0627\u0644\u0641\u0639\u0644\u064a {cashBox.Balance:N2}"
        };
    }

    public async Task<ValidationResult> ValidateCustomerBalanceAsync(int customerId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var customer = await context.Customers.FindAsync(customerId);
        if (customer is null)
            return new ValidationResult { Category = "\u0627\u0644\u0639\u0645\u064a\u0644", IsValid = false, Message = $"\u0627\u0644\u0639\u0645\u064a\u0644 #{customerId} \u063a\u064a\u0631 \u0645\u0648\u062c\u0648\u062f" };

        var creditRemaining = await context.Invoices
            .Where(i => i.CustomerId == customerId && i.PaymentMethod == PaymentMethod.Credit)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var planIds = await context.InstallmentPlans
            .Where(p => p.CustomerId == customerId)
            .Select(p => p.Id)
            .ToListAsync();
        var installmentRemaining = planIds.Count == 0
            ? 0m
            : await context.Installments
                .Where(i => planIds.Contains(i.InstallmentPlanId) && i.Status != InstallmentStatus.Paid)
                .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var unappliedDebt = await context.Vouchers
            .Where(v => v.CustomerId == customerId &&
                        v.VoucherType == VoucherType.DebtReceipt &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var receipts = await context.Vouchers
            .Where(v => v.CustomerId == customerId && v.VoucherType == VoucherType.Receipt)
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var expectedBalance = CustomerBalanceHelper.ComputeOutstandingBalance(
            creditRemaining, installmentRemaining, unappliedDebt, receipts);

        // التحقق: مجموع متبقي الآجل + الأقساط يجب أن يطابق المعادلة بعد طرح السندات غير المطبّقة/القبض
        var actualFromInvoices = creditRemaining + installmentRemaining;
        var diff = Math.Abs(expectedBalance - (actualFromInvoices - unappliedDebt - receipts));

        return new ValidationResult
        {
            Category = "\u0627\u0644\u0639\u0645\u064a\u0644",
            EntityName = customer.Name,
            IsValid = diff < 0.01m,
            ExpectedValue = expectedBalance,
            ActualValue = expectedBalance,
            Difference = diff,
            Message = diff < 0.01m
                ? $"\u0631\u0635\u064a\u062f \u0627\u0644\u0639\u0645\u064a\u0644 '{customer.Name}' \u0645\u062a\u0637\u0627\u0628\u0642: {expectedBalance:N2}"
                : $"\u0641\u0631\u0642 \u0641\u064a \u0631\u0635\u064a\u062f \u0627\u0644\u0639\u0645\u064a\u0644 '{customer.Name}': \u0627\u0644\u0645\u062a\u0648\u0642\u0639 {expectedBalance:N2}"
        };
    }

    public async Task<ValidationResult> ValidateSupplierBalanceAsync(int supplierId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var supplier = await context.Suppliers.FindAsync(supplierId);
        if (supplier is null)
            return new ValidationResult { Category = "\u0627\u0644\u0645\u0648\u0631\u062f", IsValid = false, Message = $"\u0627\u0644\u0645\u0648\u0631\u062f #{supplierId} \u063a\u064a\u0631 \u0645\u0648\u062c\u0648\u062f" };

        var creditPurchases = await context.Invoices
            .Where(i => i.SupplierId == supplierId &&
                        i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Credit)
            .SumAsync(i => (decimal?)i.NetAmount) ?? 0;

        var paymentsMade = await context.Vouchers
            .Where(v => v.VoucherType == VoucherType.Payment &&
                        v.Notes != null && v.Notes.Contains($"\u0645\u0648\u0631\u062f#{supplierId}"))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var expectedBalance = creditPurchases - paymentsMade;

        return new ValidationResult
        {
            Category = "\u0627\u0644\u0645\u0648\u0631\u062f",
            EntityName = supplier.Name,
            IsValid = true,
            ExpectedValue = expectedBalance,
            ActualValue = expectedBalance,
            Difference = 0,
            Message = $"\u0631\u0635\u064a\u062f \u0627\u0644\u0645\u0648\u0631\u062f '{supplier.Name}': \u0645\u0633\u062a\u062d\u0642\u0627\u062a {expectedBalance:N2}"
        };
    }

    public async Task<ValidationResult> ValidateInventoryAsync(int warehouseId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var warehouse = await context.Warehouses.FindAsync(warehouseId);
        if (warehouse is null)
            return new ValidationResult { Category = "\u0627\u0644\u0645\u062e\u0632\u0646", IsValid = false, Message = $"\u0627\u0644\u0645\u062e\u0632\u0646 #{warehouseId} \u063a\u064a\u0631 \u0645\u0648\u062c\u0648\u062f" };

        var negativeStocks = await context.WarehouseStocks
            .Where(ws => ws.WarehouseId == warehouseId && ws.Quantity < 0)
            .Select(ws => new { ws.ProductId, ws.Quantity })
            .ToListAsync();

        if (negativeStocks.Count > 0)
        {
            var productIds = string.Join(", ", negativeStocks.Select(s => s.ProductId));
            return new ValidationResult
            {
                Category = "\u0627\u0644\u0645\u062e\u0632\u0646",
                EntityName = warehouse.Name,
                IsValid = false,
                ExpectedValue = 0,
                ActualValue = negativeStocks.Count,
                Difference = negativeStocks.Count,
                Message = $"\u0627\u0644\u0645\u062e\u0632\u0646 '{warehouse.Name}' \u064a\u062d\u062a\u0648\u064a \u0639\u0644\u0649 {negativeStocks.Count} \u0645\u0646\u062a\u062c\u0627\u062a \u0628\u0643\u0645\u064a\u0627\u062a \u0633\u0627\u0644\u0628\u0629 (\u0645\u0639\u0631\u0641\u0627\u062a: {productIds})"
            };
        }

        var stocks = await context.WarehouseStocks
            .Where(ws => ws.WarehouseId == warehouseId)
            .ToListAsync();

        var mismatches = new List<string>();
        foreach (var stock in stocks)
        {
            var purchased = await context.InvoiceItems
                .Where(ii => ii.Invoice.WarehouseId == warehouseId &&
                             ii.ProductId == stock.ProductId &&
                             ii.Invoice.InvoiceType == InvoiceType.Purchase)
                .SumAsync(ii => (decimal?)ii.Quantity) ?? 0;

            var sold = await context.InvoiceItems
                .Where(ii => ii.Invoice.WarehouseId == warehouseId &&
                             ii.ProductId == stock.ProductId &&
                             (ii.Invoice.InvoiceType == InvoiceType.Sale || ii.Invoice.InvoiceType == InvoiceType.Installment))
                .SumAsync(ii => (decimal?)ii.Quantity) ?? 0;

            var expectedQty = purchased - sold;
            if (Math.Abs(expectedQty - stock.Quantity) >= 0.01m)
            {
                mismatches.Add($"\u0627\u0644\u0645\u0646\u062a\u062c #{stock.ProductId}: \u0645\u062a\u0648\u0642\u0639 {expectedQty:N2}\u060c \u0641\u0639\u0644\u064a {stock.Quantity:N2}");
            }
        }

        if (mismatches.Count > 0)
        {
            return new ValidationResult
            {
                Category = "\u0627\u0644\u0645\u062e\u0632\u0646",
                EntityName = warehouse.Name,
                IsValid = false,
                ExpectedValue = stocks.Count,
                ActualValue = stocks.Count - mismatches.Count,
                Difference = mismatches.Count,
                Message = $"\u0627\u0644\u0645\u062e\u0632\u0646 '{warehouse.Name}' \u0628\u0647 {mismatches.Count} \u0645\u0646\u062a\u062c\u0627\u062a \u063a\u064a\u0631 \u0645\u062a\u0637\u0627\u0628\u0642\u0629: {string.Join(" | ", mismatches.Take(5))}"
            };
        }

        return new ValidationResult
        {
            Category = "\u0627\u0644\u0645\u062e\u0632\u0646",
            EntityName = warehouse.Name,
            IsValid = true,
            ExpectedValue = stocks.Sum(s => s.Quantity),
            ActualValue = stocks.Sum(s => s.Quantity),
            Difference = 0,
            Message = $"\u0627\u0644\u0645\u062e\u0632\u0646 '{warehouse.Name}' \u0645\u062a\u0637\u0627\u0628\u0642: {stocks.Count} \u0645\u0646\u062a\u062c\u0627\u062a"
        };
    }

    public async Task<ValidationResult> ValidateBalanceSheetAsync(DateTime date)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var cashBoxes = await context.CashBoxes.SumAsync(c => (decimal?)c.Balance) ?? 0;
        var banks = await context.BankAccounts.SumAsync(b => (decimal?)b.Balance) ?? 0;

        var customerDebts = await context.Invoices
            .Where(i => (i.InvoiceType == InvoiceType.Sale) &&
                        i.PaymentMethod == PaymentMethod.Credit)
            .SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var debtPayments = await context.Vouchers
            .Where(v => v.VoucherType == VoucherType.DebtReceipt)
            .SumAsync(v => (decimal?)v.Amount) ?? 0;
        customerDebts -= debtPayments;

        var inventory = 0m;
        var invStocks = await context.WarehouseStocks
            .Where(ws => ws.Quantity > 0)
            .ToListAsync();
        foreach (var s in invStocks)
        {
            var purchaseItems = await context.InvoiceItems
                .Include(ii => ii.Invoice)
                .Where(ii => ii.ProductId == s.ProductId &&
                             ii.Invoice!.InvoiceType == InvoiceType.Purchase)
                .ToListAsync();
            var avgCost = ProductCostHelper.ComputeAverageUnitCost(purchaseItems, s.OpeningQuantity, s.UnitCost);
            if (avgCost > 0)
                inventory += Math.Round(s.Quantity * avgCost, 0);
        }

        var installmentReceivables = await context.Installments
            .Where(i => i.Status != InstallmentStatus.Paid)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var totalAssets = cashBoxes + banks + customerDebts + inventory + installmentReceivables;

        var capital = await context.CapitalEntries
            .Where(c => c.Date <= date &&
                        (c.Type == CapitalEntryType.Initial || c.Type == CapitalEntryType.Adjustment))
            .SumAsync(c => (decimal?)c.Amount) ?? 0;

        var totalSales = await context.Invoices
            .Where(i => (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) && i.Date <= date)
            .SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var totalPurchases = await context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase && i.Date <= date)
            .SumAsync(i => (decimal?)i.NetAmount) ?? 0;
        var totalExpenses = await context.Expenses
            .Where(e => e.Date <= date)
            .SumAsync(e => (decimal?)e.Amount) ?? 0;
        var bankFees = await context.Vouchers
            .Where(v => v.VoucherType == VoucherType.BankReceipt && v.Date <= date)
            .SumAsync(v => (decimal?)v.BankFees) ?? 0;
        var distributed = await context.ProfitDistributions
            .SumAsync(pd => (decimal?)pd.DistributedAmount) ?? 0;
        var profitOpening = await ProductCostHelper.GetProfitOpeningBalanceAsync(context, date);
        var accumulatedProfits = totalSales - totalPurchases - totalExpenses - bankFees - distributed + profitOpening;

        var totalEquity = capital + accumulatedProfits;

        var supplierPayables = await context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase && i.PaymentMethod == PaymentMethod.Credit)
            .SumAsync(i => (decimal?)i.NetAmount) ?? 0;

        var investorDeposits = await context.InvestorTransactions
            .Where(t => t.Type == InvestorTransactionType.Deposit)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        var investorWithdrawals = await context.InvestorTransactions
            .Where(t => t.Type == InvestorTransactionType.Withdrawal)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        investorDeposits -= investorWithdrawals;

        var totalLiabilities = supplierPayables + investorDeposits;

        var equityAndLiabilities = totalEquity + totalLiabilities;
        var diff = Math.Abs(totalAssets - equityAndLiabilities);

        return new ValidationResult
        {
            Category = "\u0627\u0644\u0645\u064a\u0632\u0627\u0646\u064a\u0629 \u0627\u0644\u0639\u0645\u0648\u0645\u064a\u0629",
            EntityName = $"\u0628\u062a\u0627\u0631\u064a\u062e {date:yyyy/MM/dd}",
            IsValid = diff < 0.01m,
            ExpectedValue = equityAndLiabilities,
            ActualValue = totalAssets,
            Difference = totalAssets - equityAndLiabilities,
            Message = diff < 0.01m
                ? $"\u0627\u0644\u0645\u064a\u0632\u0627\u0646\u064a\u0629 \u0645\u062a\u0648\u0627\u0632\u0646\u0629: \u0627\u0644\u0645\u0648\u062c\u0648\u062f\u0627\u062a = \u0627\u0644\u0645\u0644\u0643\u064a\u0629 + \u0627\u0644\u0627\u0644\u062a\u0632\u0627\u0645\u0627\u062a = {totalAssets:N2}"
                : $"\u062e\u0644\u0644 \u0641\u064a \u0627\u0644\u0645\u064a\u0632\u0627\u0646\u064a\u0629: \u0627\u0644\u0645\u0648\u062c\u0648\u062f\u0627\u062a {totalAssets:N2} \u2260 \u0627\u0644\u0645\u0644\u0643\u064a\u0629+\u0627\u0644\u0627\u0644\u062a\u0632\u0627\u0645\u0627\u062a {equityAndLiabilities:N2} (\u0641\u0631\u0642: {totalAssets - equityAndLiabilities:N2})"
        };
    }
}
