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

public partial class CapitalMovementReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _equityCapital = "0";
    [ObservableProperty] private string _initialCapital = "0";
    [ObservableProperty] private string _adjustments = "0";
    [ObservableProperty] private string _profitOpening = "0";



    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<CapitalMovementRow> _allRows = [];
    public ObservableCollection<CapitalMovementRow> Rows { get; } = [];

    public CapitalMovementReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "حركة رأس المال";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "SupervisoryReports");

        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetCapitalMovementReportAsync(DateFrom, DateTo);

            EquityCapital = FormatCurrency(result.EquityCapital);
            InitialCapital = FormatCurrency(result.InitialCapital);
            Adjustments = FormatCurrency(result.Adjustments);
            ProfitOpening = FormatCurrency(result.ProfitOpening);
            if (result.ByTypeChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.ByTypeChart);
            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DailyChart.Select(d => d.Amount).ToArray(), "القيمة", 2)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "حركة_رأس_المال.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "النوع", "المبلغ", "ملاحظات", "بواسطة" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.TypeDisplay, r.Amount, r.Notes, r.CreatedBy }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "حركة رأس المال", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "النوع", "المبلغ", "ملاحظات", "بواسطة" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.TypeDisplay, r.Amount, r.Notes, r.CreatedBy }).ToList();
        _exportService.PrintTable("حركة رأس المال", cols, rows);
    }
}
