using AlMuhasib.Core.Entities.Customs;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Models.Customs;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task H02_cancel_without_allocations_succeeds_and_writes_audit()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");

        await h.Invoices.CancelAsync(inv.Id, "admin", "سبب اختبار");

        var cancelled = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(CustomsInvoiceStatus.Cancelled, cancelled!.Status);

        await using var db = h.Factory.CreateDbContext();
        Assert.Contains(
            await db.AuditLogs.AsNoTracking().ToListAsync(),
            a => a.EntityName == "CustomsInvoice.Cancel" && a.EntityId == inv.Id);
    }

    [Fact]
    public async Task H02_cancel_rejected_when_active_allocation_exists_even_if_paid_is_zero()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");

        // Corrupt path: active allocation while PaidAmount left at 0.
        await using (var db = h.Factory.CreateDbContext())
        {
            var ghost = new CustomsReceipt
            {
                CustomerId = customer.Id,
                ReceiptNumber = "GHOST-INV",
                ReceiptDate = DateTime.Today,
                Amount = 10m,
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
                Amount = 10m,
                CreatedBy = "corrupt",
                RowVersion = [1]
            });
            db.CustomsReceipts.Add(ghost);
            await db.SaveChangesAsync();
        }

        var before = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(0m, before!.PaidAmount);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Invoices.CancelAsync(inv.Id, "admin"));

        var still = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(CustomsInvoiceStatus.Approved, still!.Status);

        await using var auditDb = h.Factory.CreateDbContext();
        Assert.DoesNotContain(
            await auditDb.AuditLogs.AsNoTracking().ToListAsync(),
            a => a.EntityName == "CustomsInvoice.Cancel" && a.EntityId == inv.Id);
    }

    [Fact]
    public async Task H02_cancel_allowed_after_receipt_soft_deleted()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");

        var receipt = await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 40m, AccountingCurrency.IQD));
        await h.Receipts.CancelAsync(receipt.Id, "admin");

        var afterReceiptCancel = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(0m, afterReceiptCancel!.PaidAmount);
        Assert.Equal(100m, afterReceiptCancel.RemainingAmount);

        await h.Invoices.CancelAsync(inv.Id, "admin");
        var cancelled = await h.Invoices.GetByIdAsync(inv.Id);
        Assert.Equal(CustomsInvoiceStatus.Cancelled, cancelled!.Status);
    }

    [Fact]
    public async Task M01_soft_delete_draft_cascades_owned_attachments_not_customer()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var expenseTypeId = await h.SeedExpenseTypeAsync();

        var draft = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id,
                AccountingCurrency.IQD,
                vehiclePrice: 100_000m,
                expenseTypeId: expenseTypeId,
                expenseAmount: 10_000m));

        var vehicleId = Assert.IsType<int>(draft.Vehicles[0].Id);
        var expenseId = Assert.IsType<int>(draft.Expenses[0].Id);

        async Task<string> MakeTempAsync(string suffix)
        {
            var path = Path.Combine(Path.GetTempPath(), $"customs-m01-{suffix}-{Guid.NewGuid():N}.txt");
            await File.WriteAllTextAsync(path, $"body-{suffix}");
            return path;
        }

        var srcInvoice = await MakeTempAsync("inv");
        var srcVehicle = await MakeTempAsync("veh");
        var srcExpense = await MakeTempAsync("exp");
        var srcCustomer = await MakeTempAsync("cust");

        try
        {
            var invAtt = await h.Attachments.AddFromFileAsync(
                CustomsAttachmentOwnerType.Invoice, draft.Id, srcInvoice, "فاتورة", "admin");
            var vehAtt = await h.Attachments.AddFromFileAsync(
                CustomsAttachmentOwnerType.Vehicle, vehicleId, srcVehicle, "سيارة", "admin");
            var expAtt = await h.Attachments.AddFromFileAsync(
                CustomsAttachmentOwnerType.Expense, expenseId, srcExpense, "مصروف", "admin");
            var custAtt = await h.Attachments.AddFromFileAsync(
                CustomsAttachmentOwnerType.Customer, customer.Id, srcCustomer, "عميل", "admin");

            Assert.True(File.Exists(invAtt.FilePath));
            Assert.True(File.Exists(vehAtt.FilePath));
            Assert.True(File.Exists(expAtt.FilePath));
            Assert.True(File.Exists(custAtt.FilePath));

            await h.Invoices.SoftDeleteAsync(draft.Id, "admin");

            Assert.Empty(await h.Attachments.GetByOwnerAsync(CustomsAttachmentOwnerType.Invoice, draft.Id));
            Assert.Empty(await h.Attachments.GetByOwnerAsync(CustomsAttachmentOwnerType.Vehicle, vehicleId));
            Assert.Empty(await h.Attachments.GetByOwnerAsync(CustomsAttachmentOwnerType.Expense, expenseId));

            var stillCustomer = await h.Attachments.GetByOwnerAsync(CustomsAttachmentOwnerType.Customer, customer.Id);
            Assert.Single(stillCustomer);
            Assert.Equal(custAtt.Id, stillCustomer[0].Id);
            Assert.True(File.Exists(custAtt.FilePath));

            Assert.False(File.Exists(invAtt.FilePath));
            Assert.False(File.Exists(vehAtt.FilePath));
            Assert.False(File.Exists(expAtt.FilePath));

            await using var db = h.Factory.CreateDbContext();
            Assert.Null(await db.CustomsInvoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == draft.Id));
            Assert.True(await db.CustomsInvoices.IgnoreQueryFilters()
                .AnyAsync(i => i.Id == draft.Id && i.IsDeleted));
            Assert.True(await db.CustomsAttachments.IgnoreQueryFilters()
                .CountAsync(a => a.IsDeleted && (a.Id == invAtt.Id || a.Id == vehAtt.Id || a.Id == expAtt.Id)) == 3);
            Assert.False(await db.CustomsAttachments.IgnoreQueryFilters()
                .AnyAsync(a => a.Id == custAtt.Id && a.IsDeleted));

            // Auto-audit on soft-delete (IsDeleted flip → Delete action).
            var logs = await db.AuditLogs.AsNoTracking().ToListAsync();
            Assert.Contains(logs, a => a.EntityName == nameof(CustomsInvoice) && a.EntityId == draft.Id);
            Assert.Contains(logs, a => a.EntityName == nameof(CustomsAttachment) && a.EntityId == invAtt.Id);
        }
        finally
        {
            foreach (var p in new[] { srcInvoice, srcVehicle, srcExpense, srcCustomer })
            {
                if (File.Exists(p))
                    File.Delete(p);
            }
        }
    }

    [Fact]
    public async Task M06_discount_less_than_vehicles_succeeds()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var created = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 1000m, discount: 200m));
        Assert.Equal(800m, created.TotalAmount);
        Assert.Equal(200m, created.DiscountAmount);
    }

    [Fact]
    public async Task M06_discount_equal_vehicles_total_zero()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var created = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 1000m, discount: 1000m));
        Assert.Equal(0m, created.TotalAmount);
        Assert.Equal(1000m, created.DiscountAmount);
    }

    [Fact]
    public async Task M06_discount_greater_than_vehicles_rejected_on_create_and_update()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();

        var createEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Invoices.CreateAsync(
                CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 1000m, discount: 1001m)));
        Assert.Contains("الخصم", createEx.Message);

        var draft = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 1000m, discount: 0m));
        var update = CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 1000m, discount: 1001m);
        update.Vehicles[0].Id = draft.Vehicles[0].Id;

        var updateEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Invoices.UpdateAsync(draft.Id, update));
        Assert.Contains("الخصم", updateEx.Message);
    }

    [Fact]
    public async Task M06_negative_discount_rejected()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Invoices.CreateAsync(
                CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 1000m, discount: -1m)));
        Assert.Contains("سالب", ex.Message);
    }
}
