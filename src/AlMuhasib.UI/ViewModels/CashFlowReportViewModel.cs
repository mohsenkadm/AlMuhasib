using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Charts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class CashFlowReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalIncoming = "0";
    [ObservableProperty] private string _totalOutgoing = "0";
    [ObservableProperty] private string _netFlow = "0";
    [ObservableProperty] private string _currentBalance = "0";

    [ObservableProperty] private int? _selectedCashBoxId;
    public ObservableCollection<CashBox> CashBoxes { get; } = [];

    [ObservableProperty] private ISeries[] _flowSeries = [];
    [ObservableProperty] private Axis[] _flowXAxes = [];
    [ObservableProperty] private Axis[] _flowYAxes = [];

    private List<CashFlowRow> _allRows = [];
    public ObservableCollection<CashFlowRow> Rows { get; } = [];

    public CashFlowReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    { PageTitle = "التدفق النقدي"; }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var cb in await _unitOfWork.CashBoxes.GetAllAsync()) CashBoxes.Add(cb);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetCashFlowReportAsync(_selectedCashBoxId, DateFrom, DateTo);

            TotalIncoming = FormatCurrency(result.TotalIncoming);
            TotalOutgoing = FormatCurrency(result.TotalOutgoing);
            NetFlow = FormatCurrency(result.NetFlow);
            CurrentBalance = FormatCurrency(result.CurrentBalance);

            var allDates = result.DailyIncomingChart.Select(d => d.Date).Union(result.DailyOutgoingChart.Select(d => d.Date)).Distinct().OrderBy(d => d).ToList();
            if (allDates.Count > 0)
            {
                FlowSeries = [
                    ChartThemeConfig.Column(allDates.Select(d => result.DailyIncomingChart.FirstOrDefault(x => x.Date == d)?.Amount ?? 0).ToArray(), "الواردات", 2),
                    ChartThemeConfig.Column(allDates.Select(d => result.DailyOutgoingChart.FirstOrDefault(x => x.Date == d)?.Amount ?? 0).ToArray(), "الصادرات", 3)
                ];
                FlowXAxes = [ChartThemeConfig.CreateXAxis(allDates.Select(d => d.ToString("MM/dd")).ToArray())];
                FlowYAxes = [ChartThemeConfig.CreateYAxis()];
            }

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePagination(_allRows, Rows);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    protected override void OnPageChanged() => UpdatePagination(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "التدفق_النقدي.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "النوع", "البيان", "وارد", "صادر", "الرصيد", "الحساب" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.Type, r.Description, r.Incoming, r.Outgoing, r.Balance, r.AccountName }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "التدفق النقدي", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "النوع", "البيان", "وارد", "صادر", "الرصيد", "الحساب" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.Type, r.Description, r.Incoming, r.Outgoing, r.Balance, r.AccountName }).ToList();
        _exportService.PrintTable("التدفق النقدي", cols, rows);
    }
}
