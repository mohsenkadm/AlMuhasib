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

public partial class CashBoxMovementReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _openingBalance = "0";
    [ObservableProperty] private string _totalIncoming = "0";
    [ObservableProperty] private string _totalOutgoing = "0";
    [ObservableProperty] private string _closingBalance = "0";

    [ObservableProperty] private int? _selectedCashBoxId;
    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];
    [ObservableProperty] private ISeries[] _outSeries = [];
    [ObservableProperty] private Axis[] _outXAxes = [];
    [ObservableProperty] private Axis[] _outYAxes = [];

    private List<CashBoxMovementRow> _allRows = [];
    public ObservableCollection<CashBoxMovementRow> Rows { get; } = [];

    public CashBoxMovementReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "حركة صندوق / قاصة";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var x in await _unitOfWork.CashBoxes.GetAllAsync()) CashBoxes.Add(x);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetCashBoxMovementReportAsync(SelectedCashBoxId, DateFrom, DateTo);

            OpeningBalance = FormatCurrency(result.OpeningBalance);
            TotalIncoming = FormatCurrency(result.TotalIncoming);
            TotalOutgoing = FormatCurrency(result.TotalOutgoing);
            ClosingBalance = FormatCurrency(result.ClosingBalance);
            if (result.DailyIncomingChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DailyIncomingChart.Select(d => d.Amount).ToArray(), "وارد", 2)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyIncomingChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            if (result.DailyOutgoingChart.Count > 0)
            {
                OutSeries = [ChartThemeConfig.Column(result.DailyOutgoingChart.Select(d => d.Amount).ToArray(), "صادر", 3)];
                OutXAxes = [ChartThemeConfig.CreateXAxis(result.DailyOutgoingChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                OutYAxes = [ChartThemeConfig.CreateYAxis()];
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "حركة_القاصة.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "النوع", "البيان", "وارد", "صادر", "الرصيد" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.Type, r.Description, r.Incoming, r.Outgoing, r.Balance }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "حركة صندوق / قاصة", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "النوع", "البيان", "وارد", "صادر", "الرصيد" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.Type, r.Description, r.Incoming, r.Outgoing, r.Balance }).ToList();
        _exportService.PrintTable("حركة صندوق / قاصة", cols, rows);
    }
}
