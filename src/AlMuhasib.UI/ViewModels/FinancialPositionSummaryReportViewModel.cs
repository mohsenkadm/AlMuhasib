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

public partial class FinancialPositionSummaryReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalAssets = "0";
    [ObservableProperty] private string _totalLiabilities = "0";
    [ObservableProperty] private string _totalEquity = "0";
    [ObservableProperty] private string _netWorkingCapital = "0";

    [ObservableProperty] private DateTime? _asOfDate = DateTime.Today;

    [ObservableProperty] private ISeries[] _pieSeries = [];

    private List<FinancialPositionLineRow> _allRows = [];
    public ObservableCollection<FinancialPositionLineRow> Rows { get; } = [];

    public FinancialPositionSummaryReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "ملخص المركز المالي";
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
            var result = await _reportService.GetFinancialPositionSummaryReportAsync(AsOfDate);

            TotalAssets = FormatCurrency(result.TotalAssets);
            TotalLiabilities = FormatCurrency(result.TotalLiabilities);
            TotalEquity = FormatCurrency(result.TotalEquity);
            NetWorkingCapital = FormatCurrency(result.NetWorkingCapital);
            if (result.CompositionChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.CompositionChart);
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "ملخص_المركز_المالي.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "القسم", "البند", "المبلغ" };
        var rows = _allRows.Select(r => new object[] { r.Section, r.LineName, r.Amount }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "ملخص المركز المالي", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "القسم", "البند", "المبلغ" };
        var rows = _allRows.Select(r => new object[] { r.Section, r.LineName, r.Amount }).ToList();
        _exportService.PrintTable("ملخص المركز المالي", cols, rows);
    }
}
