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

public partial class BankAccountStatementReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _openingBalance = "0";
    [ObservableProperty] private string _totalIn = "0";
    [ObservableProperty] private string _totalOut = "0";
    [ObservableProperty] private string _closingBalance = "0";

    [ObservableProperty] private int? _selectedBankAccountId;
    public ObservableCollection<BankAccount> BankAccounts { get; } = [];

    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];
    [ObservableProperty] private ISeries[] _outSeries = [];
    [ObservableProperty] private Axis[] _outXAxes = [];
    [ObservableProperty] private Axis[] _outYAxes = [];

    private List<BankAccountStatementRow> _allRows = [];
    public ObservableCollection<BankAccountStatementRow> Rows { get; } = [];

    public BankAccountStatementReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "كشف حساب مصرف";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var x in await _unitOfWork.BankAccounts.GetAllAsync()) BankAccounts.Add(x);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetBankAccountStatementReportAsync(SelectedBankAccountId, DateFrom, DateTo);

            OpeningBalance = FormatCurrency(result.OpeningBalance);
            TotalIn = FormatCurrency(result.TotalIn);
            TotalOut = FormatCurrency(result.TotalOut);
            ClosingBalance = FormatCurrency(result.ClosingBalance);
            if (result.DailyInChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DailyInChart.Select(d => d.Amount).ToArray(), "وارد", 2)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyInChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            if (result.DailyOutChart.Count > 0)
            {
                OutSeries = [ChartThemeConfig.Column(result.DailyOutChart.Select(d => d.Amount).ToArray(), "صادر", 3)];
                OutXAxes = [ChartThemeConfig.CreateXAxis(result.DailyOutChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "كشف_حساب_مصرف.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "النوع", "البيان", "وارد", "صادر", "الرصيد" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.Type, r.Description, r.Incoming, r.Outgoing, r.Balance }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "كشف حساب مصرف", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "النوع", "البيان", "وارد", "صادر", "الرصيد" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.Type, r.Description, r.Incoming, r.Outgoing, r.Balance }).ToList();
        _exportService.PrintTable("كشف حساب مصرف", cols, rows);
    }
}
