using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsListSortTests
{
    [Fact]
    public async Task M04_invoice_sort_remaining_asc_and_desc()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("فرز فواتير");

        var low = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 100m, vehiclePlate: "منخفض"));
        await h.Invoices.ApproveAsync(low.Id, "admin");

        var high = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 500m, vehiclePlate: "مرتفع"));
        await h.Invoices.ApproveAsync(high.Id, "admin");

        var mid = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 300m, vehiclePlate: "وسط"));
        await h.Invoices.ApproveAsync(mid.Id, "admin");

        var (asc, _) = await h.Invoices.GetPagedAsync(
            1, 20, customerId: customer.Id, sortBy: "RemainingAmount", sortDescending: false);
        Assert.Equal([100m, 300m, 500m], asc.Select(i => i.RemainingAmount).ToArray());

        var (desc, _) = await h.Invoices.GetPagedAsync(
            1, 20, customerId: customer.Id, sortBy: "RemainingAmount", sortDescending: true);
        Assert.Equal([500m, 300m, 100m], desc.Select(i => i.RemainingAmount).ToArray());
    }

    [Fact]
    public async Task M04_invoice_sort_with_search_filter_and_paging()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("فرز صفحة");

        for (var i = 0; i < 5; i++)
        {
            var inv = await h.Invoices.CreateAsync(
                CustomsTestHarness.InvoiceRequest(
                    customer.Id, AccountingCurrency.IQD, (i + 1) * 100m, vehiclePlate: $"صفحة{i}"));
            await h.Invoices.ApproveAsync(inv.Id, "admin");
        }

        var (page1, total) = await h.Invoices.GetPagedAsync(
            1, 2, search: "صفحة", customerId: customer.Id,
            sortBy: "TotalAmount", sortDescending: true);
        Assert.Equal(5, total);
        Assert.Equal(2, page1.Count);
        Assert.Equal(500m, page1[0].TotalAmount);
        Assert.Equal(400m, page1[1].TotalAmount);

        var (page2, _) = await h.Invoices.GetPagedAsync(
            2, 2, search: "صفحة", customerId: customer.Id,
            sortBy: "TotalAmount", sortDescending: true);
        Assert.Equal(2, page2.Count);
        Assert.Equal(300m, page2[0].TotalAmount);
        Assert.Equal(200m, page2[1].TotalAmount);
    }

    [Fact]
    public async Task M04_rejects_unknown_sort_column()
    {
        var h = CustomsTestHarness.CreateAdmin();

        var invEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Invoices.GetPagedAsync(1, 20, sortBy: "DropTable;--"));
        Assert.Contains("غير مسموح", invEx.Message);

        var custEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Customers.GetPagedAsync(1, 20, sortBy: "PasswordHash"));
        Assert.Contains("غير مسموح", custEx.Message);

        var recEx = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            h.Receipts.GetPagedAsync(1, 20, sortBy: "HackColumn"));
        Assert.Contains("غير مسموح", recEx.Message);
    }

    [Fact]
    public async Task M04_customer_and_receipt_sort_name_and_amount()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var a = await h.SeedCustomerAsync("أحمد فرز", "07710000001");
        var b = await h.SeedCustomerAsync("زيد فرز", "07710000002");

        var (byNameAsc, _) = await h.Customers.GetPagedAsync(
            1, 20, search: "فرز", sortBy: "Name", sortDescending: false);
        Assert.Equal(["أحمد فرز", "زيد فرز"], byNameAsc.Select(c => c.Name).ToArray());

        var invA = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(a.Id, AccountingCurrency.IQD, 1000m));
        await h.Invoices.ApproveAsync(invA.Id, "admin");
        var invB = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(b.Id, AccountingCurrency.IQD, 1000m));
        await h.Invoices.ApproveAsync(invB.Id, "admin");

        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(a.Id, invA.Id, 100m, AccountingCurrency.IQD));
        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(b.Id, invB.Id, 400m, AccountingCurrency.IQD));

        var (byAmountDesc, _) = await h.Receipts.GetPagedAsync(
            1, 20, sortBy: "Amount", sortDescending: true);
        Assert.True(byAmountDesc.Count >= 2);
        Assert.Equal(400m, byAmountDesc[0].Amount);
        Assert.Equal(100m, byAmountDesc[1].Amount);
    }

    [Fact]
    public async Task M04_default_sort_unchanged_when_sortBy_null()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync();

        var older = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 10m, vehiclePlate: "قديم"));
        await using (var db = h.Factory.CreateDbContext())
        {
            var entity = await db.CustomsInvoices.FindAsync(older.Id);
            Assert.NotNull(entity);
            entity!.InvoiceDate = DateTime.Today.AddDays(-2);
            await db.SaveChangesAsync();
        }

        var newer = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 20m, vehiclePlate: "جديد"));

        var (items, _) = await h.Invoices.GetPagedAsync(1, 20, customerId: customer.Id);
        Assert.Equal(newer.Id, items[0].Id);
        Assert.Equal(older.Id, items[1].Id);
    }

    [Fact]
    public void M05_invoice_form_has_no_duplicate_attachment_dialogs()
    {
        var formPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "AlMuhasib.UI", "Views", "Customs", "CustomsInvoiceFormView.xaml"));
        Assert.True(File.Exists(formPath), $"Form XAML missing at {formPath}");
        var xaml = File.ReadAllText(formPath);

        Assert.DoesNotContain("IsVehicleAttachmentsDialogOpen", xaml);
        Assert.DoesNotContain("IsExpenseAttachmentsDialogOpen", xaml);
        Assert.DoesNotContain("OpenVehicleAttachmentsCommand", xaml);
        Assert.DoesNotContain("OpenExpenseAttachmentsCommand", xaml);

        // Single invoice-level panel + panels inside detail/edit dialogs only.
        var panelCount = CountOccurrences(xaml, "<controls:AttachmentsPanel");
        Assert.Equal(3, panelCount);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
