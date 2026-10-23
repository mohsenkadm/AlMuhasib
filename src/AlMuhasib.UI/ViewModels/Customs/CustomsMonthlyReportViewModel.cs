using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Customs;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Customs;
using AlMuhasib.Core.Models.Customs;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;

namespace AlMuhasib.UI.ViewModels.Customs;

public partial class CustomsMonthlyReportViewModel : CustomsReportViewModelBase
{
    [ObservableProperty] private string _invoicesIqd = "0";
    [ObservableProperty] private string _invoicesUsd = "0";
    [ObservableProperty] private string _receiptsIqd = "0";
    [ObservableProperty] private string _profitIqd = "0";
    [ObservableProperty] private bool _canViewExpenses;
    [ObservableProperty] private bool _canViewProfit;
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];
    [ObservableProperty] private bool _isDailyChartEmpty = true;

    private List<CustomsMonthlyReportRow> _allRows = [];
    public ObservableCollection<CustomsMonthlyReportRow> Rows { get; } = [];

    public CustomsMonthlyReportViewModel(
        ICustomsReportService reportService,
        ICustomsPrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, printService, exportService, currentUserService)
    {
        PageTitle = "التقرير الشهري — التخليص";
        var today = DateTime.Today;
        DateFrom = new DateTime(today.Year, today.Month, 1);
        DateTo = today;
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, CustomsPermissionRegistry.Reports);
        PermissionScreen = CustomsPermissionRegistry.Reports;
        CanViewExpenses = CurrentUserService.IsAdmin
            || CurrentUserService.CanView(CustomsPermissionRegistry.Expenses);
        CanViewProfit = CurrentUserService.IsAdmin
            || CurrentUserService.CanView(CustomsPermissionRegistry.ProfitReport);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;
            var from = DateFrom ?? DateTime.Today.AddMonths(-1);
            var to = DateTo ?? DateTime.Today;
            var result = await ReportService.GetMonthlyReportAsync(from, to);

            InvoicesIqd = FormatCurrency(result.InvoicesIqd);
            InvoicesUsd = FormatCurrency(result.InvoicesUsd, AccountingCurrency.USD);
            ReceiptsIqd = $"{FormatCurrency(result.ReceiptsIqd)} / {FormatCurrency(result.ReceiptsUsd, AccountingCurrency.USD)}";
            ProfitIqd = $"{FormatCurrency(result.ProfitIqd)} / {FormatCurrency(result.ProfitUsd, AccountingCurrency.USD)}";

            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Column(result.DailyChart.Select(d => d.Amount).ToArray(), "الفواتير", 0)];
                DailyXAxes = [ChartThemeConfig.CreateXAxis(result.DailyChart.Select(d => d.Name).ToArray(), 45)];
                DailyYAxes = [ChartThemeConfig.CreateYAxis()];
                IsDailyChartEmpty = false;
            }
            else
            {
                DailySeries = [];
                DailyXAxes = [];
                DailyYAxes = [];
                IsDailyChartEmpty = true;
            }

            _allRows = result.Rows;
            CurrentPage = 1;
            UpdatePaginationWithFilters(_allRows, Rows);
        }
        catch (Exception ex) { BeautifulMessageDialog.ShowError(ex.Message); }
        finally { IsBusy = false; }
    }

    protected override void OnPageChanged() => UpdatePaginationWithFilters(_allRows, Rows);

    private (string[] Cols, IList<object[]> Rows, List<string> Summary) BuildExport()
    {
        var colList = new List<string>
        {
            "التاريخ", "فواتير", "فواتير د.ع", "فواتير $", "قبض د.ع", "قبض $"
        };
        if (CanViewExpenses)
        {
            colList.Add("مصروف د.ع");
            colList.Add("مصروف $");
        }

        if (CanViewProfit && CanViewExpenses)
        {
            colList.Add("ربح د.ع");
            colList.Add("ربح $");
        }

        IList<object[]> rows = _allRows.Select(r =>
        {
            var cells = new List<object>
            {
                r.Date.ToString("yyyy/MM/dd"), r.InvoiceCount,
                r.InvoicesIqd, r.InvoicesUsd, r.ReceiptsIqd, r.ReceiptsUsd
            };
            if (CanViewExpenses)
            {
                cells.Add(r.ExpensesIqd);
                cells.Add(r.ExpensesUsd);
            }

            if (CanViewProfit && CanViewExpenses)
            {
                cells.Add(r.ProfitIqd);
                cells.Add(r.ProfitUsd);
            }

            return cells.ToArray();
        }).ToList();
        var summary = new List<string>
        {
            $"فواتير د.ع: {InvoicesIqd}",
            $"فواتير $: {InvoicesUsd}"
        };
        if (CanViewProfit && CanViewExpenses)
            summary.Add($"الربح: {ProfitIqd}");
        return (colList.ToArray(), rows, summary);
    }

    [RelayCommand] private void ExportToExcel()
    {
        var (c, r, _) = BuildExport();
        ExportExcel($"تقرير_شهري_تخليص_{DateTime.Now:yyyyMMdd}.xlsx", "شهري", c, r);
    }

    [RelayCommand] private void Print()
    {
        var (c, r, s) = BuildExport();
        // Staff report — full internal totals including profit are intentional here.
        PrintStaffReport(CustomsPrintDocumentKind.MonthlyReport, "التقرير الشهري — التخليص", c, r, s);
    }

    [RelayCommand] private void ExportToPdf()
    {
        var (c, r, s) = BuildExport();
        ExportStaffReportPdf(
            CustomsPrintDocumentKind.MonthlyReport,
            "التقرير الشهري — التخليص", "الفترة المحددة", c, r, s,
            $"تقرير_شهري_تخليص_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
