using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsCustomerServiceTests
{
    [Fact]
    public async Task Create_and_edit_customer()
    {
        var h = CustomsTestHarness.CreateAdmin();

        var created = await h.SeedCustomerAsync("أحمد", "07711111111");
        Assert.True(created.Id > 0);
        Assert.Equal("أحمد", created.Name);

        created.Name = "أحمد المحدّث";
        created.Phone = "07722222222";
        created.CustomerType = CustomsCustomerType.Company;
        created.CompanyName = "شركة النقل";
        created.UpdatedBy = "admin";

        var updated = await h.Customers.UpdateAsync(created);
        Assert.Equal("أحمد المحدّث", updated.Name);
        Assert.Equal("07722222222", updated.Phone);
        Assert.Equal("شركة النقل", updated.CompanyName);

        var loaded = await h.Customers.GetByIdAsync(updated.Id);
        Assert.NotNull(loaded);
        Assert.Equal("أحمد المحدّث", loaded!.Name);
    }

    [Fact]
    public async Task Create_requires_name()
    {
        var h = CustomsTestHarness.CreateAdmin();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Customers.CreateAsync(new Core.Entities.Customs.CustomsCustomer
            {
                Name = "  ",
                CreatedBy = "admin",
                RowVersion = [1]
            }));
    }

    [Fact]
    public async Task H03_customer_file_summary_matches_approved_debit_and_receipt_credit()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("ملف ملخص");
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100_000m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");
        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 30_000m, AccountingCurrency.IQD));

        // Draft should not affect Debit (Approved only) but counts in InvoiceCount.
        await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 50_000m, vehiclePlate: "مسودة ملف"));

        var file = await h.Customers.GetCustomerFileAsync(customer.Id);
        Assert.NotNull(file);
        Assert.Equal(70_000m, file!.BalanceIqd);
        Assert.Equal(100_000m, file.DebitIqd);
        Assert.Equal(30_000m, file.CreditIqd);
        Assert.Equal(2, file.InvoiceCount);
        // M-03: Unpaid = Approved && Remaining > 0 (Draft excluded).
        Assert.Equal(1, file.UnpaidInvoiceCount);
        Assert.Equal(1, file.ReceiptCount);
        Assert.True(file.Invoices.Count <= 50);
        Assert.Equal(2, file.InvoicesTotalCount);
    }

    [Fact]
    public async Task H03_customer_file_pages_invoices_without_loading_full_history()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("عميل كبير");

        for (var i = 0; i < 55; i++)
        {
            var inv = await h.Invoices.CreateAsync(
                CustomsTestHarness.InvoiceRequest(
                    customer.Id,
                    AccountingCurrency.IQD,
                    1_000m + i,
                    vehiclePlate: $"ك {i}"));
            await h.Invoices.ApproveAsync(inv.Id, "admin");
        }

        var page1 = await h.Customers.GetCustomerFileAsync(customer.Id, invoicePage: 1, pageSize: 50);
        Assert.NotNull(page1);
        Assert.Equal(55, page1!.InvoicesTotalCount);
        Assert.Equal(55, page1.InvoiceCount);
        Assert.Equal(50, page1.Invoices.Count);
        Assert.Equal(55_000m + Enumerable.Range(0, 55).Sum(), page1.DebitIqd);

        var page1Ids = page1.Invoices.Select(x => x.Id).ToHashSet();

        var page2 = await h.Customers.GetCustomerFileAsync(customer.Id, invoicePage: 2, pageSize: 50);
        Assert.Equal(5, page2!.Invoices.Count);
        Assert.Equal(55, page2.InvoicesTotalCount);
        Assert.DoesNotContain(page2.Invoices, r => page1Ids.Contains(r.Id));
        // Summary stable across pages.
        Assert.Equal(page1.DebitIqd, page2.DebitIqd);
        Assert.Equal(page1.BalanceIqd, page2.BalanceIqd);
    }

    [Fact]
    public async Task M03_unpaid_invoice_count_approved_remaining_only()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("عداد غير مسدد");

        var approvedUnpaid = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100_000m, vehiclePlate: "غيرمسدد"));
        await h.Invoices.ApproveAsync(approvedUnpaid.Id, "admin");

        var approvedPaid = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 50_000m, vehiclePlate: "مسددة"));
        await h.Invoices.ApproveAsync(approvedPaid.Id, "admin");
        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, approvedPaid.Id, 50_000m, AccountingCurrency.IQD));

        await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 80_000m, vehiclePlate: "مسودة"));

        var cancelled = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 70_000m, vehiclePlate: "ملغاة"));
        await h.Invoices.ApproveAsync(cancelled.Id, "admin");
        await h.Invoices.CancelAsync(cancelled.Id, "admin", "اختبار");

        var file = await h.Customers.GetCustomerFileAsync(customer.Id);
        Assert.NotNull(file);
        Assert.Equal(1, file!.UnpaidInvoiceCount);
        Assert.Equal(100_000m, file.BalanceIqd);
    }

    [Fact]
    public async Task OpenCount_list_matches_approved_unpaid_only()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("عداد مفتوح قائمة");

        var approvedUnpaid = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100_000m, vehiclePlate: "قائمة1"));
        await h.Invoices.ApproveAsync(approvedUnpaid.Id, "admin");

        var approvedPaid = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 40_000m, vehiclePlate: "قائمة2"));
        await h.Invoices.ApproveAsync(approvedPaid.Id, "admin");
        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, approvedPaid.Id, 40_000m, AccountingCurrency.IQD));

        await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 90_000m, vehiclePlate: "قائمة3"));

        var cancelled = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 60_000m, vehiclePlate: "قائمة4"));
        await h.Invoices.ApproveAsync(cancelled.Id, "admin");
        await h.Invoices.CancelAsync(cancelled.Id, "admin", "اختبار");

        var (items, _) = await h.Customers.GetPagedAsync(1, 50, search: "عداد مفتوح قائمة");
        var row = Assert.Single(items);
        Assert.Equal(1, row.OpenInvoiceCount);
        Assert.Equal(100_000m, row.BalanceIqd);
    }
}
