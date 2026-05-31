using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Charts;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class ProfitReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _totalPurchases = "0";
    [ObservableProperty] private string _grossProfit = "0";
    [ObservableProperty] private string _netProfit = "0";
    [ObservableProperty] private string _totalExpenses = "0";
    [ObservableProperty] private string _totalBankFees = "0";
    [ObservableProperty] private string _distributedProfits = "0";
    [ObservableProperty] private string _profitMargin = "0%";

    [ObservableProperty] private ISeries[] _monthlySeries = [];
    [ObservableProperty] private Axis[] _monthlyXAxes = [];
    [ObservableProperty] private Axis[] _monthlyYAxes = [];

    private List<MonthlyProfitRow> _allRows = [];
    public ObservableCollection<MonthlyProfitRow> Rows { get; } = [];

    public ProfitReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "تقرير الأرباح";
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
            var result = await _reportService.GetProfitReportAsync(DateFrom, DateTo);
            var monthly = await _reportService.GetMonthlyProfitAsync(DateFrom, DateTo);

            TotalSales = FormatCurrency(result.TotalSales);
            TotalPurchases = FormatCurrency(result.TotalPurchases);
            GrossProfit = FormatCurrency(result.GrossProfit);
            NetProfit = FormatCurrency(result.NetProfit);
            TotalExpenses = FormatCurrency(result.TotalExpenses);
            TotalBankFees = FormatCurrency(result.TotalBankFees);
            DistributedProfits = FormatCurrency(result.DistributedProfits);
            ProfitMargin = $"{result.ProfitMargin}%";

            if (monthly.Count > 0)
            {
                MonthlySeries = [
                    ChartThemeConfig.Column(monthly.Select(m => m.Sales).ToArray(), "المبيعات", 0),
                    ChartThemeConfig.Column(monthly.Select(m => m.Purchases).ToArray(), "المشتريات", 3),
                    ChartThemeConfig.Line(monthly.Select(m => m.NetProfit).ToArray(), "صافي الربح", 2)
                ];
                MonthlyXAxes = [ChartThemeConfig.CreateXAxis(monthly.Select(m => m.Month).ToArray(), -45)];
                MonthlyYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                MonthlySeries = [];
                MonthlyXAxes = [];
                MonthlyYAxes = [];
            }

            _allRows = monthly;
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
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "تقرير_الأرباح.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "الشهر", "المبيعات", "المشتريات", "إجمالي الربح", "المصروفات", "صافي الربح", "هامش الربح %" };
        var rows = _allRows.Select(r => new object[] { r.Month, r.Sales, r.Purchases, r.GrossProfit, r.Expenses, r.NetProfit, r.ProfitMargin }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "الأرباح", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "الشهر", "المبيعات", "المشتريات", "إجمالي الربح", "المصروفات", "صافي الربح", "هامش الربح %" };
        var rows = _allRows.Select(r => new object[] { r.Month, r.Sales, r.Purchases, r.GrossProfit, r.Expenses, r.NetProfit, r.ProfitMargin }).ToList();
        _exportService.PrintTable("تقرير الأرباح", cols, rows);
    }
}
