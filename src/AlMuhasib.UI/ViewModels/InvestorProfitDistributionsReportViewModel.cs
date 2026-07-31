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

public partial class InvestorProfitDistributionsReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalDistributed = "0";
    [ObservableProperty] private string _totalProfit = "0";
    [ObservableProperty] private string _distributionCount = "0";
    [ObservableProperty] private string _investorCount = "0";

    [ObservableProperty] private int? _selectedInvestorId;
    public ObservableCollection<Investor> Investors { get; } = [];

    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    [ObservableProperty] private bool _isDetailsVisible;
    private List<InvestorProfitDistributionDetailRow> _details = [];
    public ObservableCollection<InvestorProfitDistributionDetailRow> DetailRows { get; } = [];

    private List<InvestorProfitDistributionRow> _allRows = [];
    public ObservableCollection<InvestorProfitDistributionRow> Rows { get; } = [];

    public InvestorProfitDistributionsReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "توزيعات أرباح المستثمرين";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "SupervisoryReports");
        foreach (var x in await _unitOfWork.Investors.GetAllAsync()) Investors.Add(x);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetInvestorProfitDistributionsReportAsync(DateFrom, DateTo, SelectedInvestorId);

            TotalDistributed = FormatCurrency(result.TotalDistributed);
            TotalProfit = FormatCurrency(result.TotalProfit);
            DistributionCount = result.DistributionCount.ToString("N0");
            InvestorCount = result.InvestorCount.ToString("N0");
            if (result.ByInvestorChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.ByInvestorChart);
            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DailyChart.Select(d => d.Amount).ToArray(), "القيمة", 2)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            _allRows = result.Rows;

            _details = result.Details;
            if (IsDetailsVisible)
            {
                DetailRows.Clear();
                foreach (var d in _details) DetailRows.Add(d);
            }

            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ToggleDetails()
    {
        IsDetailsVisible = !IsDetailsVisible;
        if (IsDetailsVisible)
        {
            DetailRows.Clear();
            foreach (var d in _details) DetailRows.Add(d);
        }
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "توزيعات_أرباح_المستثمرين.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "التاريخ", "إجمالي الربح", "الموزع", "التفاصيل" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.TotalProfit, r.DistributedAmount, r.DetailCount }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "توزيعات أرباح المستثمرين", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "التاريخ", "إجمالي الربح", "الموزع", "التفاصيل" };
        var rows = _allRows.Select(r => new object[] { r.Date.ToString("yyyy/MM/dd"), r.TotalProfit, r.DistributedAmount, r.DetailCount }).ToList();
        _exportService.PrintTable("توزيعات أرباح المستثمرين", cols, rows);
    }
}
