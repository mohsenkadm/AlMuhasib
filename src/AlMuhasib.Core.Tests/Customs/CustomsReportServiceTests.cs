using AlMuhasib.Core.Enums;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsReportServiceTests
{
    private static async Task<(CustomsTestHarness H, int CustomerId)> SeedApprovedFlowAsync()
    {
        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("عميل تقارير");
        var expenseTypeId = await h.SeedExpenseTypeAsync();

        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(
                customer.Id,
                AccountingCurrency.IQD,
                vehiclePrice: 300_000m,
                expenseTypeId: expenseTypeId,
                expenseAmount: 40_000m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");
        await h.Receipts.CreateAsync(
            CustomsTestHarness.ReceiptRequest(customer.Id, inv.Id, 50_000m, AccountingCurrency.IQD));

        return (h, customer.Id);
    }

    [Fact]
    public async Task Daily_monthly_customer_invoice_vehicles_debts_profit_reports()
    {
        var (h, customerId) = await SeedApprovedFlowAsync();
        var today = DateTime.Today;

        var daily = await h.Reports.GetDailyReportAsync(today, customerId);
        Assert.True(daily.InvoiceCount >= 1);
        Assert.True(daily.InvoicesIqd >= 300_000m);

        var monthly = await h.Reports.GetMonthlyReportAsync(today.AddDays(-1), today.AddDays(1));
        Assert.True(monthly.InvoicesIqd >= 300_000m);

        var customers = await h.Reports.GetCustomerReportAsync(today.AddDays(-1), today.AddDays(1));
        Assert.Contains(customers.Rows, r => r.CustomerId == customerId);

        var invoices = await h.Reports.GetInvoiceReportAsync(today.AddDays(-1), today.AddDays(1));
        Assert.NotEmpty(invoices.Rows);

        var vehicles = await h.Reports.GetVehiclesReportAsync(today.AddDays(-1), today.AddDays(1));
        Assert.True(vehicles.Total >= 1);

        var debts = await h.Reports.GetDebtsReportAsync(today, customerId);
        Assert.Contains(debts.Rows, r => r.RemainingAmount > 0);

        var profit = await h.Reports.GetProfitReportAsync(today.AddDays(-1), today.AddDays(1), customerId);
        Assert.True(profit.RevenueIqd >= 300_000m);
        // 300k revenue − 40k same-currency expense
        Assert.True(profit.ProfitIqd >= 260_000m);
    }

    [Fact]
    public async Task Customer_options_include_phone()
    {
        var h = CustomsTestHarness.CreateAdmin();
        await h.SeedCustomerAsync("مع هاتف", "07799999999");
        var options = await h.Reports.GetCustomerOptionsAsync();
        Assert.Contains(options, o => o.Name == "مع هاتف" && o.Phone == "07799999999");
    }
}
