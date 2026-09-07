using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels;

public partial class ProfitAndLossReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _totalSales = "0";
    [ObservableProperty] private string _grossProfit = "0";
    [ObservableProperty] private string _operatingProfit = "0";
    [ObservableProperty] private string _netProfit = "0";



    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<ProfitAndLossLineRow> _allRows = [];
    private ProfitAndLossReportResult? _lastResult;
    public ObservableCollection<ProfitAndLossLineRow> Rows { get; } = [];

    public ProfitAndLossReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "أرباح وخسائر";
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
            var result = await _reportService.GetProfitAndLossReportAsync(DateFrom, DateTo);
            _lastResult = result;

            TotalSales = FormatCurrency(result.TotalSales);
            GrossProfit = FormatCurrency(result.GrossProfit);
            OperatingProfit = FormatCurrency(result.OperatingProfit);
            NetProfit = FormatCurrency(result.NetProfit);
            if (result.CompositionChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.CompositionChart);
            if (result.MonthlyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.MonthlyChart.Select(d => d.Amount).ToArray(), "القيمة", 2)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.MonthlyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
            }
            _allRows = result.Lines;

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
        AmountBreakdownDialog.Show(key switch
        {
            "sales" => new AmountBreakdownModel
            {
                Title = "تفاصيل المبيعات",
                Formula = "المبيعات = Σ صافي فواتير البيع والأقساط",
                ResultLabel = "المبيعات",
                ResultAmount = r.TotalSales,
                Lines = [new AmountBreakdownLine { Operator = "Σ", Label = "فواتير المبيعات", Amount = r.TotalSales }]
            },
            "gross" => new AmountBreakdownModel
            {
                Title = "تفاصيل إجمالي الربح",
                Formula = "إجمالي الربح = المبيعات − تكلفة البضاعة",
                ResultLabel = "إجمالي الربح",
                ResultAmount = r.GrossProfit,
                Lines =
                [
                    new AmountBreakdownLine { Operator = "+", Label = "المبيعات", Amount = r.TotalSales },
                    new AmountBreakdownLine { Operator = "−", Label = "تكلفة البضاعة", Amount = r.CostOfGoodsSold },
                    new AmountBreakdownLine { Operator = "=", Label = "إجمالي الربح", Amount = r.GrossProfit, IsResult = true }
                ],
                Note = r.GrossProfit < 0 ? "سالب لأن التكلفة أكبر من المبيعات." : null
            },
            "operating" => new AmountBreakdownModel
            {
                Title = "تفاصيل الربح التشغيلي",
                Formula = "تشغيلي = إجمالي الربح − المصاريف − الرسوم البنكية",
                ResultLabel = "تشغيلي",
                ResultAmount = r.OperatingProfit,
                Lines =
                [
                    new AmountBreakdownLine { Operator = "+", Label = "إجمالي الربح", Amount = r.GrossProfit },
                    new AmountBreakdownLine { Operator = "−", Label = "المصاريف", Amount = r.TotalExpenses },
                    new AmountBreakdownLine { Operator = "−", Label = "رسوم بنكية", Amount = r.TotalBankFees },
                    new AmountBreakdownLine { Operator = "=", Label = "تشغيلي", Amount = r.OperatingProfit, IsResult = true }
                ],
                Note = r.OperatingProfit < 0 ? "سالب لأن المصاريف والرسوم تجاوزت إجمالي الربح." : null
            },
            "net" => new AmountBreakdownModel
            {
                Title = "تفاصيل صافي الربح",
                Formula = "صافي الربح = تشغيلي − توزيعات الأرباح",
                ResultLabel = "صافي الربح",
                ResultAmount = r.NetProfit,
                Lines =
                [
                    new AmountBreakdownLine { Operator = "+", Label = "المبيعات", Amount = r.TotalSales },
                    new AmountBreakdownLine { Operator = "−", Label = "تكلفة البضاعة", Amount = r.CostOfGoodsSold },
                    new AmountBreakdownLine { Operator = "−", Label = "المصاريف", Amount = r.TotalExpenses },
                    new AmountBreakdownLine { Operator = "−", Label = "رسوم بنكية", Amount = r.TotalBankFees },
                    new AmountBreakdownLine { Operator = "−", Label = "توزيعات الأرباح", Amount = r.DistributedProfits },
                    new AmountBreakdownLine { Operator = "=", Label = "صافي الربح", Amount = r.NetProfit, IsResult = true }
                ],
                Note = r.NetProfit < 0
                    ? "الرقم السالب يعني خسارة: الخصومات تجاوزت المبيعات في الفترة."
                    : null
            },
            _ => new AmountBreakdownModel { Title = "تفاصيل", ResultAmount = 0 }
        });
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "أرباح_وخسائر.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "البند", "المبلغ" };
        var rows = _allRows.Select(r => new object[] { r.LineName, r.Amount }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "أرباح وخسائر", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "البند", "المبلغ" };
        var rows = _allRows.Select(r => new object[] { r.LineName, r.Amount }).ToList();
        _exportService.PrintTable("أرباح وخسائر", cols, rows);
    }
}
