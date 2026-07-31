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

public partial class CashBalancesSummaryReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalLiquid = "0";
    [ObservableProperty] private string _cashBoxesTotal = "0";
    [ObservableProperty] private string _banksTotal = "0";
    [ObservableProperty] private string _accountCount = "0";



    [ObservableProperty] private ISeries[] _pieSeries = [];

    private List<CashBalanceRow> _allRows = [];
    public ObservableCollection<CashBalanceRow> Rows { get; } = [];

    public CashBalancesSummaryReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "ملخص أرصدة نقدية";
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
            var result = await _reportService.GetCashBalancesSummaryReportAsync();

            TotalLiquid = FormatCurrency(result.TotalLiquid);
            CashBoxesTotal = FormatCurrency(result.CashBoxesTotal);
            BanksTotal = FormatCurrency(result.BanksTotal);
            AccountCount = result.AccountCount.ToString("N0");
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "ملخص_أرصدة_نقدية.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "النوع", "الاسم", "رقم الحساب", "الرصيد" };
        var rows = _allRows.Select(r => new object[] { r.AccountType, r.Name, r.AccountNumber, r.Balance }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "ملخص أرصدة نقدية", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "النوع", "الاسم", "رقم الحساب", "الرصيد" };
        var rows = _allRows.Select(r => new object[] { r.AccountType, r.Name, r.AccountNumber, r.Balance }).ToList();
        _exportService.PrintTable("ملخص أرصدة نقدية", cols, rows);
    }
}
