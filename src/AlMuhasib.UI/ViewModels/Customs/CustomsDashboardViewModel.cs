using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels.Customs;

public partial class CustomsDashboardViewModel : ViewModelBase
{
    private readonly ICustomsDashboardService _dashboardService;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private bool _isLoaded;

    [ObservableProperty] private int _customerCount;
    [ObservableProperty] private int _invoiceCount;
    [ObservableProperty] private int _vehicleCount;
    [ObservableProperty] private int _vehiclesInProgress;
    [ObservableProperty] private int _vehiclesExited;
    [ObservableProperty] private int _vehiclesArrived;
    [ObservableProperty] private int _unpaidInvoiceCount;

    [ObservableProperty] private string _customerCountDisplay = "0";
    [ObservableProperty] private string _invoiceCountDisplay = "0";
    [ObservableProperty] private string _vehicleCountDisplay = "0";
    [ObservableProperty] private string _unpaidInvoiceCountDisplay = "0";
    [ObservableProperty] private string _vehiclesInProgressDisplay = "0";
    [ObservableProperty] private string _vehiclesExitedDisplay = "0";
    [ObservableProperty] private string _vehiclesArrivedDisplay = "0";

    [ObservableProperty] private string _receiptsDisplay = "—";
    [ObservableProperty] private string _expensesDisplay = "—";
    [ObservableProperty] private string _profitDisplay = "—";
    [ObservableProperty] private string _debtsDisplay = "—";

    [ObservableProperty] private string _periodLabel = string.Empty;

    [ObservableProperty] private ISeries[] _monthlyInvoiceSeries = [];
    [ObservableProperty] private Axis[] _monthlyXAxes = [];
    [ObservableProperty] private Axis[] _monthlyYAxes = [];
    [ObservableProperty] private ISeries[] _vehicleStatusSeries = [];

    public CustomsDashboardViewModel(
        ICustomsDashboardService dashboardService,
        ICurrentUserService currentUserService)
    {
        _dashboardService = dashboardService;
        _currentUserService = currentUserService;
        PageTitle = "لوحة التحكم";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, CustomsPermissionRegistry.Dashboard);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        IsLoaded = false;
        try
        {
            var today = DateTime.Today;
            var from = new DateTime(today.Year, today.Month, 1);
            PeriodLabel = $"الفترة: {from:yyyy/MM/dd} — {today:yyyy/MM/dd}";

            var stats = await _dashboardService.GetDashboardStatsAsync(from, today);
            ApplyStats(stats);
            BuildCharts(stats);
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            IsLoaded = true;
            Controls.BeautifulMessageDialog.ShowError($"تعذر تحميل لوحة التحكم:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyStats(CustomsDashboardStats stats)
    {
        CustomerCount = stats.CustomerCount;
        InvoiceCount = stats.InvoiceCount;
        VehicleCount = stats.VehicleCount;
        VehiclesInProgress = stats.VehiclesInProgress;
        VehiclesExited = stats.VehiclesExited;
        VehiclesArrived = stats.VehiclesArrived;
        UnpaidInvoiceCount = stats.UnpaidInvoiceCount;

        CustomerCountDisplay = stats.CustomerCount.ToString("N0");
        InvoiceCountDisplay = stats.InvoiceCount.ToString("N0");
        VehicleCountDisplay = stats.VehicleCount.ToString("N0");
        UnpaidInvoiceCountDisplay = stats.UnpaidInvoiceCount.ToString("N0");
        VehiclesInProgressDisplay = stats.VehiclesInProgress.ToString("N0");
        VehiclesExitedDisplay = stats.VehiclesExited.ToString("N0");
        VehiclesArrivedDisplay = stats.VehiclesArrived.ToString("N0");

        ReceiptsDisplay = FormatDual(stats.ReceiptsIqd, stats.ReceiptsUsd);
        ExpensesDisplay = FormatDual(stats.ExpensesIqd, stats.ExpensesUsd);
        ProfitDisplay = FormatDual(stats.ProfitIqd, stats.ProfitUsd);
        DebtsDisplay = FormatDual(stats.DebtsIqd, stats.DebtsUsd);
    }

    private void BuildCharts(CustomsDashboardStats stats)
    {
        var monthly = stats.MonthlyInvoices;
        if (monthly.Count == 0)
        {
            MonthlyInvoiceSeries = [];
            MonthlyXAxes = [ChartThemeConfig.CreateXAxis([])];
            MonthlyYAxes = [ChartThemeConfig.CreateYAxis(suffix: null)];
        }
        else
        {
            var labels = monthly.Select(m => m.Name).ToArray();
            var values = monthly.Select(m => (decimal)m.Count).ToArray();
            MonthlyInvoiceSeries = [ChartThemeConfig.Column(values, "الفواتير", 0)];
            MonthlyXAxes = [ChartThemeConfig.CreateXAxis(labels, labels.Length > 6 ? -35 : 0)];
            MonthlyYAxes = [ChartThemeConfig.CreateYAxis(suffix: null)];
        }

        VehicleStatusSeries = stats.VehicleStatusChart
            .Where(p => p.Count > 0)
            .Select((p, i) => (ISeries)ChartThemeConfig.Pie(p.Count, p.Name, i))
            .ToArray();
    }

    private static string FormatDual(decimal iqd, decimal usd) =>
        $"{iqd:N0} د.ع\n{usd:N2} $";
}
