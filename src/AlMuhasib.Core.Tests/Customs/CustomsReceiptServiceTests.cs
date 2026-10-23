using AlMuhasib.Core.Enums;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsReceiptServiceTests
{
    [Fact]
    public async Task Allocate_receipt_updates_remaining_and_cancel_reverses()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 500_000m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");

        var receipt = await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 200_000m, AccountingCurrency.IQD));
        Assert.Single(receipt.Allocations);
        Assert.Equal(200_000m, receipt.Allocations[0].Amount);

        var after = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(200_000m, after!.PaidAmount);
        Assert.Equal(300_000m, after.RemainingAmount);

        await h.Receipts.CancelAsync(receipt.Id, "admin");

        var reversed = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(0m, reversed!.PaidAmount);
        Assert.Equal(500_000m, reversed.RemainingAmount);

        var open = await h.Receipts.GetOpenInvoicesAsync(customer.Id, AccountingCurrency.IQD);
        Assert.Contains(open, o => o.InvoiceId == inv.Id && o.RemainingAmount == 500_000m);
    }

    [Fact]
    public async Task Allocation_sum_must_equal_receipt_amount()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100_000m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");

        var bad = CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 50_000m, AccountingCurrency.IQD);
        bad.Amount = 60_000m;

        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Receipts.CreateAsync(bad));
    }

    [Fact]
    public async Task Cannot_over_allocate_remaining()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100_000m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Receipts.CreateAsync(
                CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 150_000m, AccountingCurrency.IQD)));
    }
}
