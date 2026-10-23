using System.Collections.ObjectModel;
using AlMuhasib.Core.Enums;
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
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];

    private List<CustomsMonthlyReportRow> _allRows = [];
    public ObservableCollection<CustomsMonthlyReportRow> Rows { get; } = [];

    public CustomsMonthlyReportViewModel(
        ICustomsReportService reportService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, exportService, currentUserService)
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
            }
            else
            {
                DailySeries = [];
                DailyXAxes = [];
                DailyYAxes = [];
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
        var cols = new[]
        {
            "التاريخ", "فواتير", "فواتير د.ع", "فواتير $", "قبض د.ع", "قبض $",
            "مصروف د.ع", "مصروف $", "ربح د.ع", "ربح $"
        };
        IList<object[]> rows = _allRows.Select(r => new object[]
        {
            r.Date.ToString("yyyy/MM/dd"), r.InvoiceCount,
            r.InvoicesIqd, r.InvoicesUsd, r.ReceiptsIqd, r.ReceiptsUsd,
            r.ExpensesIqd, r.ExpensesUsd, r.ProfitIqd, r.ProfitUsd
        }).ToList();
        var summary = new List<string>
        {
            $"فواتير د.ع: {InvoicesIqd}",
            $"فواتير $: {InvoicesUsd}",
            $"الربح: {ProfitIqd}"
        };
        return (cols, rows, summary);
    }

    [RelayCommand] private void ExportToExcel()
    {
        var (c, r, _) = BuildExport();
        ExportExcel($"تقرير_شهري_تخليص_{DateTime.Now:yyyyMMdd}.xlsx", "شهري", c, r);
    }

    [RelayCommand] private void Print()
    {
        var (c, r, s) = BuildExport();
        PrintTable("التقرير الشهري — التخليص", c, r, s);
    }

    [RelayCommand] private void ExportToPdf()
    {
        var (c, r, s) = BuildExport();
        ExportPdfDocument("التقرير الشهري — التخليص", "الفترة المحددة", c, r, s,
            $"تقرير_شهري_تخليص_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
