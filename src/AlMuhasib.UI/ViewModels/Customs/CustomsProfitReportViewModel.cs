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

public partial class CustomsProfitReportViewModel : CustomsReportViewModelBase
{
    [ObservableProperty] private string _revenueIqd = "0";
    [ObservableProperty] private string _revenueUsd = "0";
    [ObservableProperty] private string _expensesText = "0";
    [ObservableProperty] private string _profitText = "0";
    [ObservableProperty] private CustomsReportCustomerOption? _selectedCustomer;
    [ObservableProperty] private CustomsCurrencyFilterOption? _selectedCurrency;
    [ObservableProperty] private ISeries[] _monthlySeries = [];
    [ObservableProperty] private Axis[] _monthlyXAxes = [];
    [ObservableProperty] private Axis[] _monthlyYAxes = [];
    [ObservableProperty] private bool _isMonthlyChartEmpty = true;

    public ObservableCollection<CustomsCurrencyFilterOption> CurrencyOptions { get; } =
    [
        new(null, "الكل"),
        new(AccountingCurrency.IQD, "دينار"),
        new(AccountingCurrency.USD, "دولار")
    ];

    private List<CustomsProfitReportRow> _allRows = [];
    public ObservableCollection<CustomsProfitReportRow> Rows { get; } = [];

    public CustomsProfitReportViewModel(
        ICustomsReportService reportService,
        ICustomsPrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, printService, exportService, currentUserService)
    {
        PageTitle = "تقرير الربح — التخليص";
        SelectedCurrency = CurrencyOptions[0];
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, CustomsPermissionRegistry.ProfitReport);
        PermissionScreen = CustomsPermissionRegistry.ProfitReport;
        await LoadCustomerOptionsAsync();
        SelectedCustomer = Customers.FirstOrDefault();
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
            var result = await ReportService.GetProfitReportAsync(
                from, to,
                SelectedCustomer is { Id: > 0 } ? SelectedCustomer.Id : null,
                SelectedCurrency?.Value);

            RevenueIqd = FormatCurrency(result.RevenueIqd);
            RevenueUsd = FormatCurrency(result.RevenueUsd, AccountingCurrency.USD);
            ExpensesText = $"{FormatCurrency(result.ExpensesIqd)} / {FormatCurrency(result.ExpensesUsd, AccountingCurrency.USD)}";
            ProfitText = $"{FormatCurrency(result.ProfitIqd)} / {FormatCurrency(result.ProfitUsd, AccountingCurrency.USD)}";

            if (result.MonthlyChart.Count > 0)
            {
                MonthlySeries = [ChartThemeConfig.Column(result.MonthlyChart.Select(d => d.Amount).ToArray(), "الربح", 2)];
                MonthlyXAxes = [ChartThemeConfig.CreateXAxis(result.MonthlyChart.Select(d => d.Name).ToArray())];
                MonthlyYAxes = [ChartThemeConfig.CreateYAxis(forceNonNegative: false)];
                IsMonthlyChartEmpty = false;
            }
            else
            {
                MonthlySeries = [];
                MonthlyXAxes = [];
                MonthlyYAxes = [];
                IsMonthlyChartEmpty = true;
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
        var cols = new[] { "الفاتورة", "التاريخ", "العميل", "العملة", "الإيراد", "المصروف", "الربح" };
        IList<object[]> rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.InvoiceDate.ToString("yyyy/MM/dd"), r.CustomerName,
            r.CurrencyLabel, r.Revenue, r.Expenses, r.Profit
        }).ToList();
        return (cols, rows,
        [
            $"إيراد د.ع: {RevenueIqd}",
            $"إيراد $: {RevenueUsd}",
            $"مصاريف: {ExpensesText}",
            $"الربح الداخلي: {ProfitText}"
        ]);
    }

    [RelayCommand] private void ExportToExcel()
    {
        var (c, r, _) = BuildExport();
        ExportExcel($"تقرير_ربح_تخليص_{DateTime.Now:yyyyMMdd}.xlsx", "ربح", c, r);
    }

    [RelayCommand] private void Print()
    {
        var (c, r, s) = BuildExport();
        PrintStaffReport(
            CustomsPrintDocumentKind.ProfitReport,
            "تقرير الربح — التخليص", c, r, s,
            SelectedCustomer?.Name ?? "الكل");
    }

    [RelayCommand] private void ExportToPdf()
    {
        var (c, r, s) = BuildExport();
        ExportStaffReportPdf(
            CustomsPrintDocumentKind.ProfitReport,
            "تقرير الربح — التخليص", SelectedCustomer?.Name ?? "الكل", c, r, s,
            $"تقرير_ربح_تخليص_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
