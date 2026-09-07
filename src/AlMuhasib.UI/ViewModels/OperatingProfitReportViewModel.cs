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

public partial class OperatingProfitReportViewModel : ReportViewModelBase
{
    [ObservableProperty] private string _grossProfit = "0";
    [ObservableProperty] private string _totalExpenses = "0";
    [ObservableProperty] private string _totalBankFees = "0";
    [ObservableProperty] private string _operatingProfit = "0";



    [ObservableProperty] private ISeries[] _pieSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<OperatingProfitLineRow> _allRows = [];
    private OperatingProfitReportResult? _lastResult;
    public ObservableCollection<OperatingProfitLineRow> Rows { get; } = [];

    public OperatingProfitReportViewModel(IReportService reportService, IUnitOfWork unitOfWork,
        IExportService exportService, ICurrentUserService currentUserService)
        : base(reportService, unitOfWork, exportService, currentUserService)
    {
        PageTitle = "صافي الربح التشغيلي";
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
            var result = await _reportService.GetOperatingProfitReportAsync(DateFrom, DateTo);
            _lastResult = result;

            GrossProfit = FormatCurrency(result.GrossProfit);
            TotalExpenses = FormatCurrency(result.TotalExpenses);
            TotalBankFees = FormatCurrency(result.TotalBankFees);
            OperatingProfit = FormatCurrency(result.OperatingProfit);
            if (result.CompositionChart.Count > 0)
                PieSeries = ChartThemeConfig.PieFromNameAmount(result.CompositionChart);
            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DailyChart.Select(d => d.Amount).ToArray(), "القيمة", 2)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Date.ToString("MM/dd")).ToArray())];
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
            "expenses" => new AmountBreakdownModel
            {
                Title = "تفاصيل المصاريف",
                Formula = "المصاريف = Σ مبالغ المصروفات",
                ResultLabel = "المصاريف",
                ResultAmount = r.TotalExpenses,
                Lines = [new AmountBreakdownLine { Operator = "Σ", Label = "المصروفات", Amount = r.TotalExpenses }]
            },
            "fees" => new AmountBreakdownModel
            {
                Title = "تفاصيل الرسوم البنكية",
                Formula = "الرسوم = Σ رسوم سندات القبض المصرفي",
                ResultLabel = "رسوم بنكية",
                ResultAmount = r.TotalBankFees,
                Lines = [new AmountBreakdownLine { Operator = "Σ", Label = "رسوم بنكية", Amount = r.TotalBankFees }]
            },
            "operating" => new AmountBreakdownModel
            {
                Title = "تفاصيل صافي الربح التشغيلي",
                Formula = "تشغيلي = إجمالي الربح − المصاريف − الرسوم البنكية",
                ResultLabel = "صافي تشغيلي",
                ResultAmount = r.OperatingProfit,
                Lines =
                [
                    new AmountBreakdownLine { Operator = "+", Label = "إجمالي الربح", Amount = r.GrossProfit },
                    new AmountBreakdownLine { Operator = "−", Label = "المصاريف", Amount = r.TotalExpenses },
                    new AmountBreakdownLine { Operator = "−", Label = "رسوم بنكية", Amount = r.TotalBankFees },
                    new AmountBreakdownLine { Operator = "=", Label = "صافي تشغيلي", Amount = r.OperatingProfit, IsResult = true }
                ],
                Note = r.OperatingProfit < 0 ? "سالب لأن المصاريف والرسوم تجاوزت إجمالي الربح." : null
            },
            _ => new AmountBreakdownModel { Title = "تفاصيل", ResultAmount = 0 }
        });
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    [RelayCommand]
    private void ExportToExcel()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx", FileName = "صافي_الربح_التشغيلي.xlsx" };
        if (dlg.ShowDialog() != true) return;
        var cols = new[] { "البند", "المبلغ" };
        var rows = _allRows.Select(r => new object[] { r.LineName, r.Amount }).ToList();
        _exportService.ExportToExcel(dlg.FileName, "صافي الربح التشغيلي", cols, rows);
        BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
    }

    [RelayCommand]
    private void Print()
    {
        var cols = new[] { "البند", "المبلغ" };
        var rows = _allRows.Select(r => new object[] { r.LineName, r.Amount }).ToList();
        _exportService.PrintTable("صافي الربح التشغيلي", cols, rows);
    }
}
