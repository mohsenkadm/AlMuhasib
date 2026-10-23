using AlMuhasib.Core.Entities.Customs;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task C02_rejects_duplicate_invoice_lines_that_exceed_remaining()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");

        var request = CustomsTestHarness.ReceiptRequest(
            customer.Id,
            150m,
            AccountingCurrency.IQD,
            [(inv.Id, 80m), (inv.Id, 70m)]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => h.Receipts.CreateAsync(request));

        await using var db = h.Factory.CreateDbContext();
        Assert.Equal(0, await db.CustomsReceipts.IgnoreQueryFilters().CountAsync());
        Assert.Equal(0, await db.CustomsReceiptAllocations.IgnoreQueryFilters().CountAsync());

        var after = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(0m, after!.PaidAmount);
        Assert.Equal(100m, after.RemainingAmount);

        var file = await h.Customers.GetCustomerFileAsync(customer.Id);
        Assert.Equal(100m, file!.BalanceIqd);
    }

    [Fact]
    public async Task C02_accepts_split_lines_within_remaining_and_updates_balance()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");

        var receipt = await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(
                customer.Id,
                100m,
                AccountingCurrency.IQD,
                [(inv.Id, 60m), (inv.Id, 40m)]));

        Assert.Equal(2, receipt.Allocations.Count);

        var after = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(100m, after!.PaidAmount);
        Assert.Equal(0m, after.RemainingAmount);

        var file = await h.Customers.GetCustomerFileAsync(customer.Id);
        Assert.Equal(0m, file!.BalanceIqd);
    }

    [Fact]
    public async Task C01_recompute_failure_rolls_back_receipt_and_allocations()
    {
        await using var h = CustomsTestHarness.CreateAdminTransactional();
        var customer = await h.SeedCustomerAsync();
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");

        // Valid receipt: Paid=40 Remaining=60
        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 40m, AccountingCurrency.IQD));

        // Corrupt DB outside the service: orphan receipt+alloc that pushes future sum over Total
        // without updating Paid/Remaining (so Validate still sees Remaining=60).
        await using (var corrupt = h.Factory.CreateDbContext())
        {
            var ghost = new CustomsReceipt
            {
                CustomerId = customer.Id,
                ReceiptNumber = "GHOST-1",
                ReceiptDate = DateTime.Today,
                Amount = 70m,
                Currency = AccountingCurrency.IQD,
                FxRate = 1m,
                PaymentMethod = CustomsPaymentMethod.Cash,
                ReceiverName = "ghost",
                CreatedBy = "corrupt",
                RowVersion = [1]
            };
            ghost.Allocations.Add(new CustomsReceiptAllocation
            {
                InvoiceId = inv.Id,
                Amount = 70m,
                CreatedBy = "corrupt",
                RowVersion = [1]
            });
            corrupt.CustomsReceipts.Add(ghost);
            await corrupt.SaveChangesAsync();
        }

        var before = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(40m, before!.PaidAmount);
        Assert.Equal(60m, before.RemainingAmount);

        var failing = CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 10m, AccountingCurrency.IQD);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => h.Receipts.CreateAsync(failing));
        Assert.Contains("يتجاوز", ex.Message);

        await using var db = h.Factory.CreateDbContext();
        // Ghost remains; the failed create must not leave a 10-IQD receipt.
        Assert.Equal(2, await db.CustomsReceipts.IgnoreQueryFilters().CountAsync());
        Assert.DoesNotContain(
            await db.CustomsReceipts.IgnoreQueryFilters().Select(r => r.Amount).ToListAsync(),
            a => a == 10m);
        Assert.Equal(0, await db.CustomsReceipts.CountAsync(r => r.Amount == 10m));

        var after = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(40m, after!.PaidAmount);
        Assert.Equal(60m, after.RemainingAmount);

        // No semantic audit for the failed create (EntityName CustomsReceipt with Amount 10).
        var semanticCreates = await db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityName == "CustomsReceipt" && a.NewValues != null && a.NewValues.Contains("\"Amount\":10"))
            .ToListAsync();
        Assert.Empty(semanticCreates);
    }

    [Fact]
    public async Task Update_receipt_with_multiple_allocations_recomputes_settlement_and_balance()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var inv1 = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100m, vehiclePlate: "أ 1"));
        var inv2 = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100m, vehiclePlate: "أ 2"));
        await h.Invoices.ApproveAsync(inv1.Id, "admin");
        await h.Invoices.ApproveAsync(inv2.Id, "admin");

        var created = await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(
                customer.Id,
                80m,
                AccountingCurrency.IQD,
                [(inv1.Id, 50m), (inv2.Id, 30m)]));

        var update = CustomsTestHarness.ReceiptRequest(
            customer.Id,
            90m,
            AccountingCurrency.IQD,
            [(inv1.Id, 40m), (inv2.Id, 50m)]);
        // Keep allocation ids so Upsert updates in place where possible.
        update.Allocations[0].Id = created.Allocations.First(a => a.InvoiceId == inv1.Id).Id;
        update.Allocations[1].Id = created.Allocations.First(a => a.InvoiceId == inv2.Id).Id;

        await h.Receipts.UpdateAsync(created.Id, update);

        var after1 = await h.Invoices.GetByIdAsync(inv1.Id);
        var after2 = await h.Invoices.GetByIdAsync(inv2.Id);
        Assert.Equal(40m, after1!.PaidAmount);
        Assert.Equal(60m, after1.RemainingAmount);
        Assert.Equal(50m, after2!.PaidAmount);
        Assert.Equal(50m, after2.RemainingAmount);

        var file = await h.Customers.GetCustomerFileAsync(customer.Id);
        Assert.Equal(110m, file!.BalanceIqd);
    }

    [Fact]
    public async Task Cancel_receipt_restores_paid_remaining_and_customer_balance()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 200m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");

        var receipt = await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 75m, AccountingCurrency.IQD));

        var midFile = await h.Customers.GetCustomerFileAsync(customer.Id);
        Assert.Equal(125m, midFile!.BalanceIqd);

        await h.Receipts.CancelAsync(receipt.Id, "admin");

        var after = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(0m, after!.PaidAmount);
        Assert.Equal(200m, after.RemainingAmount);

        var file = await h.Customers.GetCustomerFileAsync(customer.Id);
        Assert.Equal(200m, file!.BalanceIqd);

        await using var db = h.Factory.CreateDbContext();
        Assert.True(await db.CustomsReceipts.IgnoreQueryFilters().AnyAsync(r => r.Id == receipt.Id && r.IsDeleted));
    }

    [Fact]
    public async Task Currency_mismatch_rejected_and_dual_currency_balances_stay_separate()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var iqd = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100_000m, vehiclePlate: "ع 1"));
        var usd = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.USD, 500m, vehiclePlate: "ع 2"));
        await h.Invoices.ApproveAsync(iqd.Id, "admin");
        await h.Invoices.ApproveAsync(usd.Id, "admin");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Receipts.CreateAsync(
                CustomsTestHarness.ReceiptRequest(customer.Id, iqd.Id, 10m, AccountingCurrency.USD)));

        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, iqd.Id, 20_000m, AccountingCurrency.IQD));
        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, usd.Id, 100m, AccountingCurrency.USD));

        var file = await h.Customers.GetCustomerFileAsync(customer.Id);
        Assert.Equal(80_000m, file!.BalanceIqd);
        Assert.Equal(400m, file.BalanceUsd);
    }

    [Fact]
    public async Task Audit_log_written_on_create_and_cancel_not_on_failed_validate()
    {
        await using var h = CustomsTestHarness.CreateAdminTransactional();
        var customer = await h.SeedCustomerAsync();
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Receipts.CreateAsync(
                CustomsTestHarness.ReceiptRequest(
                    customer.Id,
                    150m,
                    AccountingCurrency.IQD,
                    [(inv.Id, 80m), (inv.Id, 70m)])));

        await using (var db = h.Factory.CreateDbContext())
        {
            Assert.Empty(await db.AuditLogs.AsNoTracking()
                .Where(a => a.EntityName == "CustomsReceipt" || a.EntityName == "CustomsReceipt.Cancel")
                .ToListAsync());
        }

        var receipt = await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 40m, AccountingCurrency.IQD));

        await using (var db = h.Factory.CreateDbContext())
        {
            Assert.Contains(
                await db.AuditLogs.AsNoTracking().ToListAsync(),
                a => a.EntityName == "CustomsReceipt" && a.EntityId == receipt.Id);
        }

        await h.Receipts.CancelAsync(receipt.Id, "admin");

        await using (var db = h.Factory.CreateDbContext())
        {
            Assert.Contains(
                await db.AuditLogs.AsNoTracking().ToListAsync(),
                a => a.EntityName == "CustomsReceipt.Cancel" && a.EntityId == receipt.Id);
        }
    }
}
