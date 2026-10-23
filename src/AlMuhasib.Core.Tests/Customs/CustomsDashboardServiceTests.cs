using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsDashboardServiceTests
{
    [Fact]
    public async Task H03_dashboard_profit_excludes_draft_and_cancelled_and_respects_date_range()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("داشبورد");
        var expenseTypeId = await h.SeedExpenseTypeAsync();
        var today = DateTime.Today;

        await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id, AccountingCurrency.IQD, 999_000m,
                expenseTypeId: expenseTypeId, expenseAmount: 1_000m, vehiclePlate: "مسودة د"));

        var approved = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id, AccountingCurrency.IQD, 100_000m,
                expenseTypeId: expenseTypeId, expenseAmount: 20_000m, vehiclePlate: "معتمد د"));
        await h.Invoices.ApproveAsync(approved.Id, "admin");

        var cancelled = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id, AccountingCurrency.IQD, 50_000m, vehiclePlate: "ملغى د"));
        await h.Invoices.ApproveAsync(cancelled.Id, "admin");
        await h.Invoices.CancelAsync(cancelled.Id, "admin");

        var outside = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id, AccountingCurrency.IQD, 70_000m, vehiclePlate: "خارج د"));
        await h.Invoices.ApproveAsync(outside.Id, "admin");
        await using (var db = h.Factory.CreateDbContext())
        {
            var row = await db.CustomsInvoices.FindAsync(outside.Id);
            row!.InvoiceDate = today.AddMonths(-3);
            await db.SaveChangesAsync();
        }

        var dash = await h.Dashboard.GetDashboardStatsAsync(today.AddDays(-1), today.AddDays(1));
        Assert.Equal(80_000m, dash.ProfitIqd);
        Assert.Equal(0m, dash.ProfitUsd);
        // Period invoice count includes Draft + Approved (not Cancelled); outside month excluded from count/profit.
        Assert.Equal(2, dash.InvoiceCount);
        // Debts / unpaid are global Approved+Remaining (not period-scoped) — includes outside invoice.
        Assert.Equal(2, dash.UnpaidInvoiceCount);
        Assert.Equal(170_000m, dash.DebtsIqd);
    }

    [Fact]
    public async Task H03_dashboard_keeps_iqd_and_usd_separate()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("عملات داشبورد");
        var expenseTypeId = await h.SeedExpenseTypeAsync();
        var today = DateTime.Today;

        var iqd = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id, AccountingCurrency.IQD, 200_000m,
                expenseTypeId: expenseTypeId, expenseAmount: 50_000m, vehiclePlate: "ع د1"));
        await h.Invoices.ApproveAsync(iqd.Id, "admin");

        var usd = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id, AccountingCurrency.USD, 500m,
                expenseTypeId: expenseTypeId, expenseAmount: 100m,
                expenseCurrency: AccountingCurrency.USD, vehiclePlate: "ع د2"));
        await h.Invoices.ApproveAsync(usd.Id, "admin");

        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, iqd.Id, 20_000m, AccountingCurrency.IQD));
        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, usd.Id, 50m, AccountingCurrency.USD));

        var dash = await h.Dashboard.GetDashboardStatsAsync(today.AddDays(-1), today.AddDays(1));
        Assert.Equal(150_000m, dash.ProfitIqd);
        Assert.Equal(400m, dash.ProfitUsd);
        Assert.Equal(20_000m, dash.ReceiptsIqd);
        Assert.Equal(50m, dash.ReceiptsUsd);
        Assert.Equal(180_000m, dash.DebtsIqd);
        Assert.Equal(450m, dash.DebtsUsd);
        Assert.Equal(12, dash.MonthlyInvoices.Count);
    }
}
