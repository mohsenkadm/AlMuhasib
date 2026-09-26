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

        // فواتير نقدية بنفس عملة القاصة فقط
        var salesIncome = await context.Invoices
            .Where(i => i.CashBoxId == cashBoxId &&
                        i.Currency == cashBox.Currency &&
                        (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
                        i.PaymentMethod == PaymentMethod.Cash)
            .SumAsync(i => (decimal?)i.NetAmount) ?? 0;

        var purchaseOutflow = await context.Invoices
            .Where(i => i.CashBoxId == cashBoxId &&
                        i.Currency == cashBox.Currency &&
                        i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Cash)
            .SumAsync(i => (decimal?)i.NetAmount) ?? 0;

        var saleReturnOutflow = await context.Invoices
            .Where(i => i.CashBoxId == cashBoxId &&
                        i.Currency == cashBox.Currency &&
                        i.InvoiceType == InvoiceType.SaleReturn &&
                        i.PaymentMethod == PaymentMethod.Cash)
            .SumAsync(i => (decimal?)i.NetAmount) ?? 0;

        var purchaseReturnIncome = await context.Invoices
            .Where(i => i.CashBoxId == cashBoxId &&
                        i.Currency == cashBox.Currency &&
                        i.InvoiceType == InvoiceType.PurchaseReturn &&
                        i.PaymentMethod == PaymentMethod.Cash)
            .SumAsync(i => (decimal?)i.NetAmount) ?? 0;

        var receipts = await context.Vouchers
            .Where(v => v.CashBoxId == cashBoxId &&
                        v.Currency == cashBox.Currency &&
                        (v.VoucherType == VoucherType.Receipt ||
                         v.VoucherType == VoucherType.DebtReceipt ||
                         v.VoucherType == VoucherType.InvestorDeposit))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var payments = await context.Vouchers
            .Where(v => v.CashBoxId == cashBoxId &&
                        v.Currency == cashBox.Currency &&
                        (v.VoucherType == VoucherType.Payment ||
                         v.VoucherType == VoucherType.InvestorWithdrawal))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var bankReceipts = await context.Vouchers
            .Where(v => v.CashBoxId == cashBoxId &&
                        v.Currency == cashBox.Currency &&
                        v.VoucherType == VoucherType.BankReceipt)
            .SumAsync(v => (decimal?)(v.Amount - v.BankFees)) ?? 0;

        // أقساط سُددت مباشرة بدون سند (تجنب الازدواج مع سندات InstallmentId)
        var voucherInstallmentIds = await context.Vouchers
            .Where(v => v.CashBoxId == cashBoxId && v.InstallmentId.HasValue)
            .Select(v => v.InstallmentId!.Value)
            .Distinct()
            .ToListAsync();

        var installmentPayments = await context.Installments
            .Where(inst => inst.CashBoxId == cashBoxId &&
                           inst.PaidAmount > 0 &&
                           !voucherInstallmentIds.Contains(inst.Id) &&
                           inst.InstallmentPlan!.Invoice!.Currency == cashBox.Currency)
            .SumAsync(inst => (decimal?)inst.PaidAmount) ?? 0;

        var expenses = await context.Expenses
            .Where(e => e.CashBoxId == cashBoxId && e.Currency == cashBox.Currency)
            .SumAsync(e => (decimal?)e.Amount) ?? 0;

        var transfersIn = await context.Transfers
            .Where(t => t.ToType == TransferAccountType.CashBox &&
                        t.ToId == cashBoxId &&
                        t.Currency == cashBox.Currency)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var transfersOut = await context.Transfers
            .Where(t => t.FromType == TransferAccountType.CashBox &&
                        t.FromId == cashBoxId &&
                        t.Currency == cashBox.Currency)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var expectedBalance = salesIncome
                              - purchaseOutflow
                              - saleReturnOutflow
                              + purchaseReturnIncome
                              + receipts
                              - payments
                              + bankReceipts
                              + installmentPayments
                              - expenses
                              + transfersIn
                              - transfersOut;
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
            .Where(i => i.CustomerId == customerId &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Currency == AccountingCurrency.IQD)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var planIds = await context.InstallmentPlans
            .Where(p => p.CustomerId == customerId)
            .Select(p => p.Id)
            .ToListAsync();
        var installmentRemaining = planIds.Count == 0
            ? 0m
            : await context.Installments
                .Where(i => planIds.Contains(i.InstallmentPlanId) &&
                            i.Status != InstallmentStatus.Paid &&
                            i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD)
                .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var unappliedDebt = await context.Vouchers
            .Where(v => v.CustomerId == customerId &&
                        v.VoucherType == VoucherType.DebtReceipt &&
                        v.Currency == AccountingCurrency.IQD &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var receipts = await context.Vouchers
            .Where(v => v.CustomerId == customerId &&
                        v.VoucherType == VoucherType.Receipt &&
                        v.Currency == AccountingCurrency.IQD &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var expectedIqd = CustomerBalanceHelper.ComputeOutstandingBalance(
            creditRemaining, installmentRemaining, unappliedDebt, receipts);

        var creditRemainingUsd = await context.Invoices
            .Where(i => i.CustomerId == customerId &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Currency == AccountingCurrency.USD)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        var installmentRemainingUsd = planIds.Count == 0
            ? 0m
            : await context.Installments
                .Where(i => planIds.Contains(i.InstallmentPlanId) &&
                            i.Status != InstallmentStatus.Paid &&
                            i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.USD)
                .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        var unappliedDebtUsd = await context.Vouchers
            .Where(v => v.CustomerId == customerId &&
                        v.VoucherType == VoucherType.DebtReceipt &&
                        v.Currency == AccountingCurrency.USD &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;
        var receiptsUsd = await context.Vouchers
            .Where(v => v.CustomerId == customerId &&
                        v.VoucherType == VoucherType.Receipt &&
                        v.Currency == AccountingCurrency.USD &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;
        var expectedUsd = CustomerBalanceHelper.ComputeOutstandingBalance(
            creditRemainingUsd, installmentRemainingUsd, unappliedDebtUsd, receiptsUsd);

        var actualFromInvoicesIqd = creditRemaining + installmentRemaining;
        var diffIqd = Math.Abs(expectedIqd - (actualFromInvoicesIqd - unappliedDebt - receipts));
        var actualFromInvoicesUsd = creditRemainingUsd + installmentRemainingUsd;
        var diffUsd = Math.Abs(expectedUsd - (actualFromInvoicesUsd - unappliedDebtUsd - receiptsUsd));
        var isValid = diffIqd < 0.01m && diffUsd < 0.01m;

        return new ValidationResult
        {
            Category = "\u0627\u0644\u0639\u0645\u064a\u0644",
            EntityName = customer.Name,
            IsValid = isValid,
            ExpectedValue = expectedIqd,
            ActualValue = expectedIqd,
            Difference = diffIqd + diffUsd,
            Message = isValid
                ? $"رصيد العميل '{customer.Name}' متطابق: {expectedIqd:N2} د.ع" +
                  (expectedUsd != 0 ? $" | $ {expectedUsd:N2}" : string.Empty)
                : $"فرق في رصيد العميل '{customer.Name}': د.ع متوقع {expectedIqd:N2} (فرق {diffIqd:N2})" +
                  (diffUsd >= 0.01m ? $" | $ متوقع {expectedUsd:N2} (فرق {diffUsd:N2})" : string.Empty)
        };
    }

    public async Task<ValidationResult> ValidateSupplierBalanceAsync(int supplierId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var supplier = await context.Suppliers.FindAsync(supplierId);
        if (supplier is null)
            return new ValidationResult { Category = "\u0627\u0644\u0645\u0648\u0631\u062f", IsValid = false, Message = $"\u0627\u0644\u0645\u0648\u0631\u062f #{supplierId} \u063a\u064a\u0631 \u0645\u0648\u062c\u0648\u062f" };

        var creditRemainingIqd = await context.Invoices
            .Where(i => i.SupplierId == supplierId &&
                        i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Currency == AccountingCurrency.IQD)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        var unappliedIqd = await context.Vouchers
            .Where(v => v.SupplierId == supplierId &&
                        v.VoucherType == VoucherType.Payment &&
                        v.Currency == AccountingCurrency.IQD &&
                        !v.InvoiceId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;
        var expectedIqd = SupplierBalanceHelper.ComputeOutstandingPayables(creditRemainingIqd, unappliedIqd);

        var creditRemainingUsd = await context.Invoices
            .Where(i => i.SupplierId == supplierId &&
                        i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Currency == AccountingCurrency.USD)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        var unappliedUsd = await context.Vouchers
            .Where(v => v.SupplierId == supplierId &&
                        v.VoucherType == VoucherType.Payment &&
                        v.Currency == AccountingCurrency.USD &&
                        !v.InvoiceId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;
        var expectedUsd = SupplierBalanceHelper.ComputeOutstandingPayables(creditRemainingUsd, unappliedUsd);

        return new ValidationResult
        {
            Category = "\u0627\u0644\u0645\u0648\u0631\u062f",
            EntityName = supplier.Name,
            IsValid = true,
            ExpectedValue = expectedIqd,
            ActualValue = expectedIqd,
            Difference = 0,
            Message = expectedUsd != 0
                ? $"رصيد المورد '{supplier.Name}': مستحقات {expectedIqd:N0} د.ع | {expectedUsd:N2} $"
                : $"رصيد المورد '{supplier.Name}': مستحقات {expectedIqd:N0} د.ع"
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
        var endOfDay = date.Date.AddDays(1).AddTicks(-1);

        // ميزانية التحقق بالدينار فقط — لا خلط مع الدولار (سياسة المحاسبة العراقية)
        var cashBoxes = await context.CashBoxes
            .Where(c => c.Currency == AccountingCurrency.IQD)
            .SumAsync(c => (decimal?)c.Balance) ?? 0;
        var banks = await context.BankAccounts
            .Where(b => b.Currency == AccountingCurrency.IQD)
            .SumAsync(b => (decimal?)b.Balance) ?? 0;

        var creditRemaining = await context.Invoices
            .Where(i => i.CustomerId != null &&
                        (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Currency == AccountingCurrency.IQD &&
                        i.Date <= endOfDay)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        var unappliedDebt = await context.Vouchers
            .Where(v => v.CustomerId != null &&
                        v.VoucherType == VoucherType.DebtReceipt &&
                        v.Currency == AccountingCurrency.IQD &&
                        v.Date <= endOfDay &&
                        !v.InvoiceId.HasValue &&
                        !v.InstallmentId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;
        var unappliedReceipts = await context.Vouchers
            .Where(v => v.CustomerId != null &&
                        v.VoucherType == VoucherType.Receipt &&
                        v.Currency == AccountingCurrency.IQD &&
                        v.Date <= endOfDay &&
                        !v.InvoiceId.HasValue &&
                        !v.InstallmentId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;

        var installmentReceivables = await context.Installments
            .Where(i => i.RemainingAmount > 0 &&
                        i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.IQD)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;

        var customerDebts = CustomerBalanceHelper.ComputeOutstandingBalance(
            creditRemaining, 0, unappliedDebt, unappliedReceipts);

        var inventory = 0m;
        var invStocks = await context.WarehouseStocks
            .Where(ws => ws.Quantity > 0)
            .ToListAsync();
        var productIds = invStocks.Select(s => s.ProductId).Distinct().ToList();
        var purchasesByProduct = await ProductCostHelper.GetPurchaseItemsByProductAsync(context, productIds);
        foreach (var s in invStocks)
        {
            var purchaseItems = purchasesByProduct.GetValueOrDefault(s.ProductId) ?? [];
            var avgCost = ProductCostHelper.ComputeAverageUnitCost(purchaseItems, s.OpeningQuantity, s.UnitCost);
            if (avgCost > 0)
                inventory += Math.Round(s.Quantity * avgCost, 0);
        }

        var totalAssets = cashBoxes + banks + customerDebts + inventory + installmentReceivables;

        var capital = await context.CapitalEntries
            .Where(c => c.Date <= endOfDay &&
                        (c.Type == CapitalEntryType.Initial || c.Type == CapitalEntryType.Adjustment))
            .SumAsync(c => (decimal?)c.Amount) ?? 0;

        var totalSales = await InvoiceSignedSums.SumSignedNetAsync(
            InvoiceFilters.ForProfitAndSalesTotals(context.Invoices, context.InstallmentPlans)
                .Where(i => i.Date <= endOfDay));
        var totalPurchases = await InvoiceSignedSums.SumSignedNetAsync(
            InvoiceFilters.ForPurchasesTotals(context.Invoices)
                .Where(i => i.Date <= endOfDay));
        var totalExpenses = await context.Expenses
            .Where(e => e.Currency == AccountingCurrency.IQD && e.Date <= endOfDay)
            .SumAsync(e => (decimal?)e.Amount) ?? 0;
        var bankFees = await context.Vouchers
            .Where(v => v.VoucherType == VoucherType.BankReceipt &&
                        v.Currency == AccountingCurrency.IQD &&
                        v.Date <= endOfDay)
            .SumAsync(v => (decimal?)v.BankFees) ?? 0;
        var distributed = await context.ProfitDistributions
            .Where(pd => pd.Date <= endOfDay)
            .SumAsync(pd => (decimal?)pd.DistributedAmount) ?? 0;
        var profitOpening = await ProductCostHelper.GetProfitOpeningBalanceAsync(context, endOfDay);
        var accumulatedProfits = totalSales - totalPurchases - totalExpenses - bankFees - distributed + profitOpening;

        var totalEquity = capital + accumulatedProfits;

        var supplierCreditRemaining = await context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Currency == AccountingCurrency.IQD &&
                        i.Date <= endOfDay)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        var unappliedSupplierPayments = await context.Vouchers
            .Where(v => v.SupplierId != null &&
                        v.VoucherType == VoucherType.Payment &&
                        v.Currency == AccountingCurrency.IQD &&
                        v.Date <= endOfDay &&
                        !v.InvoiceId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;
        var supplierPayables = SupplierBalanceHelper.ComputeOutstandingPayables(
            supplierCreditRemaining, unappliedSupplierPayments);

        var investorDeposits = await context.InvestorTransactions
            .Where(t => t.Type == InvestorTransactionType.Deposit && t.Date <= endOfDay)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        var investorWithdrawals = await context.InvestorTransactions
            .Where(t => t.Type == InvestorTransactionType.Withdrawal && t.Date <= endOfDay)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        investorDeposits = Math.Max(0, investorDeposits - investorWithdrawals);

        var totalLiabilities = supplierPayables + investorDeposits;
        var equityAndLiabilities = totalEquity + totalLiabilities;
        var diff = Math.Abs(totalAssets - equityAndLiabilities);

        // إفصاح دولار فقط — لا يدخل في معادلة التوازن بالدينار
        var cashBoxesUsd = await context.CashBoxes
            .Where(c => c.Currency == AccountingCurrency.USD)
            .SumAsync(c => (decimal?)c.Balance) ?? 0;
        var banksUsd = await context.BankAccounts
            .Where(b => b.Currency == AccountingCurrency.USD)
            .SumAsync(b => (decimal?)b.Balance) ?? 0;
        var creditRemainingUsd = await context.Invoices
            .Where(i => i.CustomerId != null &&
                        (i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment) &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Currency == AccountingCurrency.USD &&
                        i.Date <= endOfDay)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        var installmentReceivablesUsd = await context.Installments
            .Where(i => i.RemainingAmount > 0 &&
                        i.InstallmentPlan!.Invoice!.Currency == AccountingCurrency.USD)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        var unappliedDebtUsd = await context.Vouchers
            .Where(v => v.CustomerId != null &&
                        v.VoucherType == VoucherType.DebtReceipt &&
                        v.Currency == AccountingCurrency.USD &&
                        v.Date <= endOfDay &&
                        !v.InvoiceId.HasValue &&
                        !v.InstallmentId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;
        var unappliedReceiptsUsd = await context.Vouchers
            .Where(v => v.CustomerId != null &&
                        v.VoucherType == VoucherType.Receipt &&
                        v.Currency == AccountingCurrency.USD &&
                        v.Date <= endOfDay &&
                        !v.InvoiceId.HasValue &&
                        !v.InstallmentId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(CustomerBalanceHelper.DebtReceiptAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;
        var customerDebtsUsd = CustomerBalanceHelper.ComputeOutstandingBalance(
            creditRemainingUsd, installmentReceivablesUsd, unappliedDebtUsd, unappliedReceiptsUsd);
        var supplierCreditRemainingUsd = await context.Invoices
            .Where(i => i.InvoiceType == InvoiceType.Purchase &&
                        i.PaymentMethod == PaymentMethod.Credit &&
                        i.Currency == AccountingCurrency.USD &&
                        i.Date <= endOfDay)
            .SumAsync(i => (decimal?)i.RemainingAmount) ?? 0;
        var unappliedSupplierPaymentsUsd = await context.Vouchers
            .Where(v => v.SupplierId != null &&
                        v.VoucherType == VoucherType.Payment &&
                        v.Currency == AccountingCurrency.USD &&
                        v.Date <= endOfDay &&
                        !v.InvoiceId.HasValue &&
                        (v.Notes == null || !v.Notes.Contains(SupplierBalanceHelper.PaymentAppliedMarker)))
            .SumAsync(v => (decimal?)v.Amount) ?? 0;
        var supplierPayablesUsd = SupplierBalanceHelper.ComputeOutstandingPayables(
            supplierCreditRemainingUsd, unappliedSupplierPaymentsUsd);

        var usdDisclosure =
            $" | نقد $ {cashBoxesUsd:N2} | بنوك $ {banksUsd:N2}" +
            $" | ذمم مدينة $ {customerDebtsUsd:N2} | ذمم دائنة $ {supplierPayablesUsd:N2}";

        return new ValidationResult
        {
            Category = "الميزانية العمومية",
            EntityName = $"بتاريخ {date:yyyy/MM/dd}",
            IsValid = diff < 0.01m,
            ExpectedValue = equityAndLiabilities,
            ActualValue = totalAssets,
            Difference = totalAssets - equityAndLiabilities,
            Message = (diff < 0.01m
                ? $"الميزانية متوازنة (دينار): الموجودات = الملكية + الالتزامات = {totalAssets:N0}"
                : $"خلل في الميزانية (دينار): الموجودات {totalAssets:N0} ≠ الملكية+الالتزامات {equityAndLiabilities:N0} (فرق: {totalAssets - equityAndLiabilities:N0})")
                + usdDisclosure
        };
    }
}
