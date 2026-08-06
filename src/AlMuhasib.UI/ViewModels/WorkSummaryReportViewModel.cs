using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels;

public partial class WorkSummaryReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _newCustomersCount = "0";
    [ObservableProperty] private string _totalSalesAmount = "0";
    [ObservableProperty] private string _dealCount = "0";
    [ObservableProperty] private string _distinctProductCount = "0";
    [ObservableProperty] private string _totalProductQuantity = "0";

    [ObservableProperty] private ISeries[] _salesByYearSeries = [];
    [ObservableProperty] private Axis[] _salesByYearXAxes = [];
    [ObservableProperty] private Axis[] _salesByYearYAxes = [];

    [ObservableProperty] private ISeries[] _topCustomersSeries = [];
    [ObservableProperty] private Axis[] _topCustomersXAxes = [];
    [ObservableProperty] private Axis[] _topCustomersYAxes = [];

    [ObservableProperty] private ISeries[] _busiestHoursSeries = [];
    [ObservableProperty] private Axis[] _busiestHoursXAxes = [];
    [ObservableProperty] private Axis[] _busiestHoursYAxes = [];

    private List<WorkSummaryHourRow> _allRows = [];
    public ObservableCollection<WorkSummaryHourRow> Rows { get; } = [];

    public WorkSummaryReportViewModel(
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "ملخص العمل";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetWorkSummaryAsync(DateFrom, DateTo);

            NewCustomersCount = result.NewCustomersCount.ToString("N0");
            TotalSalesAmount = FormatCurrency(result.TotalSalesAmount);
            DealCount = result.DealCount.ToString("N0");
            DistinctProductCount = result.DistinctProductCount.ToString("N0");
            TotalProductQuantity = result.TotalProductQuantity.ToString("N2");

            if (result.SalesByYearChart.Count > 0)
            {
                SalesByYearSeries =
                [
                    ChartThemeConfig.Column(
                        result.SalesByYearChart.Select(x => x.Amount).ToArray(),
                        "المبيعات",
                        0)
                ];
                SalesByYearXAxes = [ChartThemeConfig.CreateXAxis(result.SalesByYearChart.Select(x => x.Name).ToArray())];
                SalesByYearYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                SalesByYearSeries = [];
                SalesByYearXAxes = [];
                SalesByYearYAxes = [];
            }

            if (result.TopCustomersChart.Count > 0)
            {
                TopCustomersSeries =
                [
                    ChartThemeConfig.Column(
                        result.TopCustomersChart.Select(x => x.Amount).ToArray(),
                        "المبيعات",
                        1)
                ];
                TopCustomersXAxes =
                [
                    ChartThemeConfig.CreateXAxis(
                        result.TopCustomersChart.Select(x => TruncateLabel(x.Name, 14)).ToArray(),
                        rotation: 25)
                ];
                TopCustomersYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                TopCustomersSeries = [];
                TopCustomersXAxes = [];
                TopCustomersYAxes = [];
            }

            if (result.BusiestHoursChart.Count > 0)
            {
                BusiestHoursSeries =
                [
                    ChartThemeConfig.Column(
                        result.BusiestHoursChart.Select(x => x.Amount).ToArray(),
                        "عدد التعاملات",
                        2)
                ];
                BusiestHoursXAxes =
                [
                    ChartThemeConfig.CreateXAxis(
                        result.BusiestHoursChart.Select(x => x.Name).ToArray(),
                        rotation: 45)
                ];
                BusiestHoursYAxes = [ChartThemeConfig.CreateYAxis(suffix: null)];
            }
            else
            {
                BusiestHoursSeries = [];
                BusiestHoursXAxes = [];
                BusiestHoursYAxes = [];
            }

            _allRows = result.HourRows.OrderByDescending(r => r.ActivityCount).ThenBy(r => r.Hour).ToList();
            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "ملخص_العمل.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "الساعة", "عدد التعاملات", "قيمة المبيعات" };
        var rows = _allRows.Select(r => new object[] { r.HourLabel, r.ActivityCount, r.SalesAmount }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "ملخص العمل", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "الساعة", "عدد التعاملات", "قيمة المبيعات" };
        var rows = _allRows.Select(r => new object[] { r.HourLabel, r.ActivityCount, r.SalesAmount }).ToList();
        _exportService.PrintTable("ملخص العمل", cols, rows);
    }

    private static string TruncateLabel(string value, int max)
        => string.IsNullOrWhiteSpace(value) ? "—"
            : value.Length <= max ? value
            : value[..max] + "…";
}
