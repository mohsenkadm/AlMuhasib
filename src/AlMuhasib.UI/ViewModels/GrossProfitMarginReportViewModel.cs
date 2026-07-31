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

public partial class GrossProfitMarginReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _costOfGoodsSold = "0";
    [ObservableProperty] private string _grossProfit = "0";
    [ObservableProperty] private string _grossMarginPercent = "0";



    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<GrossProfitMarginRow> _allRows = [];
    public ObservableCollection<GrossProfitMarginRow> Rows { get; } = [];

    public GrossProfitMarginReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "هامش الربح الإجمالي";
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
            var result = await _reportService.GetGrossProfitMarginReportAsync(DateFrom, DateTo);

            TotalSales = FormatCurrency(result.TotalSales);
            CostOfGoodsSold = FormatCurrency(result.CostOfGoodsSold);
            GrossProfit = FormatCurrency(result.GrossProfit);
            GrossMarginPercent = result.GrossMarginPercent.ToString("N1");
            if (result.CompositionChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.CompositionChart);
            if (result.DailyGrossChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DailyGrossChart.Select(d => d.Amount).ToArray(), "القيمة", 2)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyGrossChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            _allRows = result.Rows;

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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "هامش_الربح_الإجمالي.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "الفاتورة", "العميل", "الإيراد", "التكلفة", "الربح", "الهامش %" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.InvoiceNumber, r.CustomerName, r.Revenue, r.Cost, r.GrossProfit, r.MarginPercent }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "هامش الربح الإجمالي", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "الفاتورة", "العميل", "الإيراد", "التكلفة", "الربح", "الهامش %" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.InvoiceNumber, r.CustomerName, r.Revenue, r.Cost, r.GrossProfit, r.MarginPercent }).ToList();
        _exportService.PrintTable("هامش الربح الإجمالي", cols, rows);
    }
}
