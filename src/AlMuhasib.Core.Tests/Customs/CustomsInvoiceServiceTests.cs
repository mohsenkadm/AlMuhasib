using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Models.Customs;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsInvoiceServiceTests
{
    [Fact]
    public async Task Create_invoice_with_vehicles_expenses_calculates_total_iqd()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var expenseTypeId = await h.SeedExpenseTypeAsync();

        var created = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id,
                AccountingCurrency.IQD,
                vehiclePrice: 1_000_000m,
                discount: 50_000m,
                expenseTypeId: expenseTypeId,
                expenseAmount: 100_000m));

        Assert.Equal(950_000m, created.TotalAmount);
        Assert.Equal(950_000m, created.RemainingAmount);
        Assert.Equal(0m, created.PaidAmount);
        Assert.Equal(CustomsInvoiceStatus.Draft, created.Status);
        Assert.Single(created.Vehicles);
        Assert.Single(created.Expenses);

        var profit = CustomsFinancialRules.ComputeInternalProfit(
            created.TotalAmount,
            created.Currency,
            created.Expenses.Select(e => (e.Currency, e.Amount)));
        Assert.Equal(850_000m, profit);
    }

    [Fact]
    public async Task Edit_vehicles_recalculates_total_and_approve_cancel()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();

        var created = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 500_000m));

        var vehicleId = created.Vehicles[0].Id;
        var update = CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 700_000m);
        update.Vehicles[0].Id = vehicleId;
        update.Vehicles[0].IraqiPlate = "بصرة 99";
        update.Vehicles.Add(new CustomsInvoiceVehicleDraft
        {
            IraqiPlate = "نينوى 5",
            VehiclePrice = 100_000m,
            CargoType = "إضافية"
        });

        var updated = await h.Invoices.UpdateAsync(created.Id, update);
        Assert.Equal(800_000m, updated.TotalAmount);
        Assert.Equal(2, updated.Vehicles.Count);
        Assert.Contains(updated.Vehicles, v => v.IraqiPlate == "بصرة 99");

        await h.Invoices.ApproveAsync(updated.Id, "admin");
        var approved = await h.Invoices.GetByIdAsync(updated.Id);
        Assert.Equal(CustomsInvoiceStatus.Approved, approved!.Status);

        await h.Invoices.CancelAsync(updated.Id, "admin", "اختبار إلغاء");
        var cancelled = await h.Invoices.GetByIdAsync(updated.Id);
        Assert.Equal(CustomsInvoiceStatus.Cancelled, cancelled!.Status);
    }

    [Fact]
    public async Task Usd_invoice_flow_with_expense_and_remaining()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var expenseTypeId = await h.SeedExpenseTypeAsync();

        var created = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id,
                AccountingCurrency.USD,
                vehiclePrice: 1_000m,
                fxRate: 1500m,
                expenseTypeId: expenseTypeId,
                expenseAmount: 100m,
                expenseCurrency: AccountingCurrency.USD));

        Assert.Equal(1_000m, created.TotalAmount);
        Assert.Equal(AccountingCurrency.USD, created.Currency);
        Assert.Equal(1500m, created.FxRate);

        await h.Invoices.ApproveAsync(created.Id, "admin");

        var receipt = await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, created.Id, 400m, AccountingCurrency.USD, 1500m));
        Assert.Equal(400m, receipt.Amount);

        var afterPay = await h.Invoices.GetByIdAsync(created.Id);
        Assert.Equal(400m, afterPay!.PaidAmount);
        Assert.Equal(600m, afterPay.RemainingAmount);

        var profit = CustomsFinancialRules.ComputeInternalProfit(
            afterPay.TotalAmount,
            afterPay.Currency,
            afterPay.Expenses.Select(e => (e.Currency, e.Amount)));
        Assert.Equal(900m, profit);
    }

    [Fact]
    public async Task Mixed_currency_customer_keeps_iqd_and_usd_separate()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var expenseTypeId = await h.SeedExpenseTypeAsync();

        var iqd = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id, AccountingCurrency.IQD, 1_000_000m,
                expenseTypeId: expenseTypeId, expenseAmount: 50_000m));
        await h.Invoices.ApproveAsync(iqd.Id, "admin");

        var usd = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id, AccountingCurrency.USD, 500m, fxRate: 1500m,
                expenseTypeId: expenseTypeId, expenseAmount: 20m, expenseCurrency: AccountingCurrency.USD));
        await h.Invoices.ApproveAsync(usd.Id, "admin");

        // USD receipt must not allocate to IQD invoice
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Receipts.CreateAsync(
                CustomsTestHarness.ReceiptRequest(customer.Id, iqd.Id, 100m, AccountingCurrency.USD, 1500m)));

        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, iqd.Id, 200_000m, AccountingCurrency.IQD));
        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, usd.Id, 100m, AccountingCurrency.USD, 1500m));

        var iqdAfter = await h.Invoices.GetByIdAsync(iqd.Id);
        var usdAfter = await h.Invoices.GetByIdAsync(usd.Id);
        Assert.Equal(200_000m, iqdAfter!.PaidAmount);
        Assert.Equal(800_000m, iqdAfter.RemainingAmount);
        Assert.Equal(100m, usdAfter!.PaidAmount);
        Assert.Equal(400m, usdAfter.RemainingAmount);

        // Other-currency expense on IQD invoice does not reduce IQD profit
        var iqdWithUsdExpense = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id, AccountingCurrency.IQD, 100_000m,
                expenseTypeId: expenseTypeId, expenseAmount: 999m,
                expenseCurrency: AccountingCurrency.USD));
        var profit = CustomsFinancialRules.ComputeInternalProfit(
            iqdWithUsdExpense.TotalAmount,
            iqdWithUsdExpense.Currency,
            iqdWithUsdExpense.Expenses.Select(e => (e.Currency, e.Amount)));
        Assert.Equal(100_000m, profit);

        var file = await h.Customers.GetCustomerFileAsync(customer.Id);
        Assert.NotNull(file);
        // Draft invoice is excluded from customer balance; only approved remainings count.
        Assert.Equal(iqdAfter.RemainingAmount, file!.BalanceIqd);
        Assert.Equal(usdAfter.RemainingAmount, file.BalanceUsd);
    }

    [Fact]
    public async Task Cannot_cancel_invoice_with_payments()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100_000m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");
        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 10_000m, AccountingCurrency.IQD));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Invoices.CancelAsync(inv.Id, "admin"));
    }
}
