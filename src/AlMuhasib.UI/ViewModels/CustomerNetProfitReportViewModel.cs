using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels;

public partial class CustomerNetProfitReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalNetProfit = "0";
    [ObservableProperty] private string _totalOutstanding = "0";
    [ObservableProperty] private string _customerCount = "0";
    [ObservableProperty] private string _averageMarginPercent = "0%";
    [ObservableProperty] private int _topCount = 30;
    [ObservableProperty] private ISeries[] _chartSeries = [];
    [ObservableProperty] private Axis[] _chartXAxes = [];
    [ObservableProperty] private Axis[] _chartYAxes = [];

    public ObservableCollection<int> TopCountOptions { get; } = [10, 20, 30, 50];

    /// <summary>true = العملاء الأقل ربحاً.</summary>
    public virtual bool IsLeastProfitableMode => false;

    public string ReportTitle => IsLeastProfitableMode ? "العملاء الأقل ربحاً" : "صافي أرباح العملاء";
    public string ChartTitle => IsLeastProfitableMode ? "أقل 10 عملاء ربحاً" : "أعلى 10 عملاء ربحاً";
    public Visibility TopCountVisibility => IsLeastProfitableMode ? Visibility.Visible : Visibility.Collapsed;

    private List<CustomerNetProfitRow> _allRows = [];
    private CustomerNetProfitReportResult? _lastResult;
    public ObservableCollection<CustomerNetProfitRow> Rows { get; } = [];

    public CustomerNetProfitReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = ReportTitle;
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
            int? topN = IsLeastProfitableMode ? TopCount : null;
            var result = await _reportService.GetCustomerNetProfitReportAsync(
                DateFrom, DateTo, IsLeastProfitableMode, topN);
            _lastResult = result;

            TotalNetProfit = FormatCurrency(result.TotalNetProfit);
            TotalOutstanding = FormatCurrency(result.TotalOutstanding);
            CustomerCount = result.CustomerCount.ToString("N0");
            AverageMarginPercent = $"{result.AverageMarginPercent}%";

            if (result.Chart.Count > 0)
            {
                var colorIndex = IsLeastProfitableMode ? 3 : 0;
                ChartSeries = [ChartThemeConfig.Column(result.Chart.Select(c => c.Amount).ToArray(), "صافي الربح", colorIndex)];
                ChartXAxes = [ChartThemeConfig.CreateXAxis(result.Chart.Select(c => c.Name).ToArray(), -45)];
                ChartYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            else
            {
                ChartSeries = [];
            }

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void ShowAmountDetails(string? key)
    {
        if (_lastResult is null || string.IsNullOrWhiteSpace(key)) return;
        var r = _lastResult;
        var totalRevenue = _allRows.Sum(x => x.SalesAmount);
        var totalCost = _allRows.Sum(x => x.Cost);
        AmountBreakdownDialog.Show(key switch
        {
            "net" => new AmountBreakdownModel
            {
                Title = "تفاصيل صافي أرباح العملاء",
                Formula = "صافي الأرباح = Σ (مبيعات العميل − تكلفتها)",
                ResultLabel = "صافي الأرباح",
                ResultAmount = r.TotalNetProfit,
                Lines =
                [
                    new AmountBreakdownLine { Operator = "+", Label = "إجمالي مبيعات العملاء", Amount = totalRevenue },
                    new AmountBreakdownLine { Operator = "−", Label = "إجمالي التكلفة", Amount = totalCost },
                    new AmountBreakdownLine { Operator = "=", Label = "صافي الأرباح", Amount = r.TotalNetProfit, IsResult = true }
                ],
                Note = r.TotalNetProfit < 0
                    ? "سالب لأن تكلفة مبيعات بعض العملاء أعلى من إيرادهم."
                    : $"مجموع صافي ربح {r.CustomerCount} عميل في التقرير."
            },
            "outstanding" => new AmountBreakdownModel
            {
                Title = "تفاصيل إجمالي الديون",
                Formula = "الديون = Σ المتبقي على العملاء الظاهرين",
                ResultLabel = "إجمالي الديون",
                ResultAmount = r.TotalOutstanding,
                Lines = [new AmountBreakdownLine { Operator = "Σ", Label = "ذمم العملاء", Amount = r.TotalOutstanding }]
            },
            _ => new AmountBreakdownModel { Title = "تفاصيل", ResultAmount = 0 }
        });
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = IsLeastProfitableMode ? "العملاء_الأقل_ربحا.xlsx" : "صافي_أرباح_العملاء.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        var cols = new[] { "#", "العميل", "الهاتف", "عدد الفواتير", "المبيعات", "التكلفة", "صافي الربح", "الهامش %", "الدين المستحق" };
        var rows = _allRows.Select(r => new object[]
        {
            r.Rank, r.CustomerName, r.Phone, r.InvoiceCount,
            r.SalesAmount.ToString("N0"), r.Cost.ToString("N0"), r.NetProfit.ToString("N0"),
            r.MarginPercent, r.OutstandingBalance.ToString("N0")
        }).ToList();
        _exportService.ExportToExcel(dlg.FileName, ReportTitle, cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "#", "العميل", "الهاتف", "فواتير", "المبيعات", "التكلفة", "صافي الربح", "الهامش %", "الدين" };
        var rows = _allRows.Select(r => new object[]
        {
            r.Rank, r.CustomerName, r.Phone, r.InvoiceCount,
            r.SalesAmount.ToString("N0"), r.Cost.ToString("N0"), r.NetProfit.ToString("N0"),
            r.MarginPercent, r.OutstandingBalance.ToString("N0")
        }).ToList();
        _exportService.PrintTable(ReportTitle, cols, rows);
    }
}

public partial class LeastProfitCustomersReportViewModel : CustomerNetProfitReportViewModel
{
    public override bool IsLeastProfitableMode => true;

    public LeastProfitCustomersReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "العملاء الأقل ربحاً";
    }
}
