using AlMuhasib.Core.Enums;
using Xunit;

namespace AlMuhasib.Core.Tests.Customs;

public class CustomsChartEmptyBindingTests
{
    [Fact]
    public async Task L03_dashboard_chart_sources_empty_without_data_and_populated_with_data()
    {
        var emptyHarness = CustomsTestHarness.CreateAdmin();
        var empty = await emptyHarness.Dashboard.GetDashboardStatsAsync(
            DateTime.Today.AddDays(-7), DateTime.Today.AddDays(1));
        Assert.DoesNotContain(empty.MonthlyInvoices, m => m.Count > 0);
        Assert.Equal(0, empty.VehicleStatusChart.Sum(p => p.Count));

        var h = CustomsTestHarness.CreateAdmin();
        var customer = await h.SeedCustomerAsync("رسوم بيانية");
        var inv = await h.Invoices.CreateAsync(
            CustomsTestHarness.InvoiceRequest(customer.Id, AccountingCurrency.IQD, 50_000m));
        await h.Invoices.ApproveAsync(inv.Id, "admin");

        var filled = await h.Dashboard.GetDashboardStatsAsync(
            DateTime.Today.AddDays(-7), DateTime.Today.AddDays(1));
        Assert.Contains(filled.MonthlyInvoices, m => m.Count > 0);
        Assert.True(filled.VehicleStatusChart.Sum(p => p.Count) > 0);
    }

    [Fact]
    public void L03_customs_views_bind_chartcard_isempty()
    {
        var uiRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "AlMuhasib.UI", "Views", "Customs"));
        Assert.True(Directory.Exists(uiRoot), $"Customs views missing at {uiRoot}");

        var files = new[]
        {
            "CustomsDashboardView.xaml",
            "CustomsMonthlyReportView.xaml",
            "CustomsProfitReportView.xaml",
            "CustomsInvoiceReportView.xaml",
            "CustomsDailyReportView.xaml",
            "CustomsVehiclesReportView.xaml",
            "CustomsCustomerReportView.xaml",
            "CustomsDebtsReportView.xaml"
        };

        foreach (var file in files)
        {
            var path = Path.Combine(uiRoot, file);
            Assert.True(File.Exists(path), path);
            var xaml = File.ReadAllText(path);
            Assert.Contains("IsEmpty=\"{Binding", xaml);
            Assert.Contains("<controls:ChartCard", xaml);
        }
    }
}
