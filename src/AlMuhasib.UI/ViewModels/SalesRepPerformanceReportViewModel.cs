using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.SalesRep;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels;

public partial class SalesRepPerformanceReportViewModel : ReportViewModelBase
{
    private readonly ISalesRepService _salesRepService;

    private List<SalesRepPerformanceRow> _allRows = [];
    public ObservableCollection<SalesRepPerformanceRow> Rows { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _totalCollections = "0";
    [ObservableProperty] private string _totalCommissions = "0";
    [ObservableProperty] private string _repCount = "0";
    [ObservableProperty] private string _averageAchievement = "0%";

    [ObservableProperty] private ISeries[] _chartSeries = [];
    [ObservableProperty] private Axis[] _chartXAxes = [];
    [ObservableProperty] private Axis[] _chartYAxes = [];

    public SalesRepPerformanceReportViewModel(
        ISalesRepService salesRepService,
        IReportService reportService,
        IUnitOfWork unitOfWork,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        _salesRepService = salesRepService;
        PageTitle = "أداء المندوبين";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "SalesRepPerformance");
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var data = await _salesRepService.GetPerformanceComparisonAsync(DateFrom, DateTo);
            _allRows = data.ToList();

            TotalSales = FormatCurrency(_allRows.Sum(r => r.TotalSales));
            TotalCollections = FormatCurrency(_allRows.Sum(r => r.TotalCollections));
            TotalCommissions = FormatCurrency(_allRows.Sum(r => r.TotalCommissions));
            RepCount = _allRows.Count.ToString("N0");
            AverageAchievement = _allRows.Count == 0
                ? "0%"
                : $"{_allRows.Average(r => r.AchievementPercent):N1}%";

            var top = _allRows.OrderByDescending(r => r.TotalSales).Take(12).ToList();
            if (top.Count > 0)
            {
                ChartSeries = [ChartThemeConfig.Column(top.Select(r => r.TotalSales).ToArray(), "المبيعات", 0)];
                ChartXAxes = [ChartThemeConfig.CreateXAxis(top.Select(r => r.Name).ToArray(), -35)];
                ChartYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                ChartSeries = [];
            }

            CurrentPage = 1;
            ApplyFilterAndPage();
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

    partial void OnSearchTextChanged(string value)
    {
        CurrentPage = 1;
        ApplyFilterAndPage();
    }

    protected override void OnPageChanged() => ApplyFilterAndPage();

    private void ApplyFilterAndPage()
    {
        IEnumerable<SalesRepPerformanceRow> filtered = _allRows;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            filtered = filtered.Where(r =>
                r.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (r.Region?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        UpdatePaginationWithFilters(filtered.ToList(), Rows);
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        try
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel|*.xlsx",
                FileName = $"أداء_المندوبين_{DateTime.Now:yyyyMMdd}.xlsx"
            };
            if (dlg.ShowDialog() != true) return;

            var cols = new[]
            {
                "المندوب", "المنطقة", "الحالة", "الفواتير", "العملاء",
                "المبيعات", "التحصيل", "الذمم", "العمولات", "غير المدفوعة",
                "الهدف", "المحقق", "نسبة التحقق %"
            };
            var rows = _allRows.Select(r => new object[]
            {
                r.Name, r.Region ?? "", r.IsActive ? "فعال" : "غير فعال",
                r.InvoiceCount, r.CustomerCount,
                r.TotalSales, r.TotalCollections, r.RemainingReceivables,
                r.TotalCommissions, r.UnpaidCommissions,
                r.TargetAmount, r.AchievedAmount, r.AchievementPercent
            }).ToList();
            _exportService.ExportToExcel(dlg.FileName, "الأداء", cols, rows);
            BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void Print()
    {
        try
        {
            var cols = new[] { "المندوب", "المنطقة", "المبيعات", "التحصيل", "العمولات", "نسبة التحقق %" };
            IList<object[]> rows = _allRows.Select(r => new object[]
            {
                r.Name, r.Region ?? "",
                r.TotalSales.ToString("N0"), r.TotalCollections.ToString("N0"),
                r.TotalCommissions.ToString("N0"), r.AchievementPercent.ToString("N1")
            }).ToList();
            _exportService.PrintTable("تقرير أداء المندوبين", cols, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(ex.Message);
        }
    }
}
