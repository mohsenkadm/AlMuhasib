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

public partial class StatementOfFinancialPositionReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalAssets = "0";
    [ObservableProperty] private string _totalLiabilities = "0";
    [ObservableProperty] private string _totalEquity = "0";
    [ObservableProperty] private string _difference = "0";

    [ObservableProperty] private DateTime? _asOfDate = DateTime.Today;

    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _secondPieSeries = [];

    private List<StatementOfFinancialPositionLineRow> _allRows = [];
    public ObservableCollection<StatementOfFinancialPositionLineRow> Rows { get; } = [];

    public StatementOfFinancialPositionReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "الميزانية العمومية";
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
            var result = await _reportService.GetStatementOfFinancialPositionReportAsync(AsOfDate ?? DateTime.Today);

            TotalAssets = FormatCurrency(result.TotalAssets);
            TotalLiabilities = FormatCurrency(result.TotalLiabilities);
            TotalEquity = FormatCurrency(result.TotalEquity);
            Difference = FormatCurrency(result.Difference);
            if (result.AssetsChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.AssetsChart);
            if (result.EquityLiabilitiesChart.Count > 0)
                SecondPieSeries = ChartThemeConfig.PieFromNameAmount(result.EquityLiabilitiesChart);
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "الميزانية_العمومية.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "القسم", "البند", "المبلغ" };
        var rows = _allRows.Select(r => new object[] { r.Section, r.LineName, r.Amount }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "الميزانية العمومية", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "القسم", "البند", "المبلغ" };
        var rows = _allRows.Select(r => new object[] { r.Section, r.LineName, r.Amount }).ToList();
        _exportService.PrintTable("الميزانية العمومية", cols, rows);
    }
}
