using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Charts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class OperatingProfitReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _grossProfit = "0";
    [ObservableProperty] private string _totalExpenses = "0";
    [ObservableProperty] private string _totalBankFees = "0";
    [ObservableProperty] private string _operatingProfit = "0";



    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<OperatingProfitLineRow> _allRows = [];
    public ObservableCollection<OperatingProfitLineRow> Rows { get; } = [];

    public OperatingProfitReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "صافي الربح التشغيلي";
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
            var result = await _reportService.GetOperatingProfitReportAsync(DateFrom, DateTo);

            GrossProfit = FormatCurrency(result.GrossProfit);
            TotalExpenses = FormatCurrency(result.TotalExpenses);
            TotalBankFees = FormatCurrency(result.TotalBankFees);
            OperatingProfit = FormatCurrency(result.OperatingProfit);
            if (result.CompositionChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.CompositionChart);
            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DailyChart.Select(d => d.Amount).ToArray(), "القيمة", 2)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            _allRows = result.Lines;

            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "صافي_الربح_التشغيلي.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "البند", "المبلغ" };
        var rows = _allRows.Select(r => new object[] { r.LineName, r.Amount }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "صافي الربح التشغيلي", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "البند", "المبلغ" };
        var rows = _allRows.Select(r => new object[] { r.LineName, r.Amount }).ToList();
        _exportService.PrintTable("صافي الربح التشغيلي", cols, rows);
    }
}
