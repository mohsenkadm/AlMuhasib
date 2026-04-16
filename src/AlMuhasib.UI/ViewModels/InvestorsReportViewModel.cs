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

public partial class InvestorsReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalInvestments = "0";
    [ObservableProperty] private string _totalDistributed = "0";
    [ObservableProperty] private string _investorCount = "0";
    [ObservableProperty] private string _lastDistributionDate = "—";

    [ObservableProperty] private int? _selectedInvestorId;
    public ObservableCollection<Investor> Investors { get; } = [];

    [ObservableProperty] private ISeries[] _sharesSeries = [];
    [ObservableProperty] private ISeries[] _distributedSeries = [];

    private List<InvestorReportRow> _allRows = [];
    public ObservableCollection<InvestorReportRow> Rows { get; } = [];

    public InvestorsReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    { PageTitle = "تقرير المستثمرين"; }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, "Reports");
        foreach (var i in await _unitOfWork.Investors.GetAllAsync()) Investors.Add(i);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var result = await _reportService.GetInvestorsReportAsync(_selectedInvestorId, DateFrom, DateTo);

            TotalInvestments = FormatCurrency(result.TotalInvestments);
            TotalDistributed = FormatCurrency(result.TotalDistributed);
            InvestorCount = result.InvestorCount.ToString("N0");
            LastDistributionDate = result.LastDistributionDate?.ToString("yyyy/MM/dd") ?? "—";

            if (result.SharesChart.Count > 0)
                SharesSeries = ChartThemeConfig.PieFromNameAmount(result.SharesChart);
            if (result.DistributedChart.Count > 0)
                DistributedSeries = ChartThemeConfig.PieFromNameAmount(result.DistributedChart);

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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تقرير_المستثمرين.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "المستثمر", "إجمالي الإيداع", "الإيداع المؤهل", "نسبة الربح %", "الأرباح الموزعة", "آخر سحب" };
        var rows = _allRows.Select(r => new object[] { r.InvestorName, r.TotalDeposit, r.EligibleDeposit, r.ProfitPercentage, r.TotalDistributed, r.LastWithdrawal?.ToString("yyyy/MM/dd") ?? "—" }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "المستثمرين", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "المستثمر", "إجمالي الإيداع", "الإيداع المؤهل", "نسبة الربح %", "الأرباح الموزعة", "آخر سحب" };
        var rows = _allRows.Select(r => new object[] { r.InvestorName, r.TotalDeposit, r.EligibleDeposit, r.ProfitPercentage, r.TotalDistributed, r.LastWithdrawal?.ToString("yyyy/MM/dd") ?? "—" }).ToList();
        _exportService.PrintTable("تقرير المستثمرين", cols, rows);
    }
}
