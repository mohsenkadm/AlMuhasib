using System.Collections.ObjectModel;
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
using NameAmountPoint = AlMuhasib.Core.Interfaces.Services.NameAmountPoint;

namespace AlMuhasib.UI.ViewModels.Customs;

public partial class CustomsDailyReportViewModel : CustomsReportViewModelBase
{
    [ObservableProperty] private string _invoiceCount = "0";
    [ObservableProperty] private string _invoicesIqd = "0";
    [ObservableProperty] private string _receiptsIqd = "0";
    [ObservableProperty] private string _expensesIqd = "0";
    [ObservableProperty] private string _vehicleCount = "0";
    [ObservableProperty] private bool _canViewExpenses;
    [ObservableProperty] private CustomsReportCustomerOption? _selectedCustomer;
    [ObservableProperty] private ISeries[] _typeSeries = [];

    private List<CustomsDailyReportRow> _allRows = [];
    public ObservableCollection<CustomsDailyReportRow> Rows { get; } = [];

    public CustomsDailyReportViewModel(
        ICustomsReportService reportService,
        ICustomsPrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, printService, exportService, currentUserService)
    {
        PageTitle = "التقرير اليومي — التخليص";
        DateFrom = DateTo = DateTime.Today;
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, CustomsPermissionRegistry.Reports);
        PermissionScreen = CustomsPermissionRegistry.Reports;
        CanViewExpenses = CurrentUserService.IsAdmin
            || CurrentUserService.CanView(CustomsPermissionRegistry.Expenses);
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
            var day = DateFrom ?? DateTime.Today;
            DateTo = day;
            var customerId = SelectedCustomer is { Id: > 0 } ? SelectedCustomer.Id : (int?)null;
            var result = await ReportService.GetDailyReportAsync(day, customerId);

            InvoiceCount = result.InvoiceCount.ToString("N0");
            InvoicesIqd = $"{FormatCurrency(result.InvoicesIqd)} / {FormatCurrency(result.InvoicesUsd, Core.Enums.AccountingCurrency.USD)}";
            ReceiptsIqd = $"{FormatCurrency(result.ReceiptsIqd)} / {FormatCurrency(result.ReceiptsUsd, Core.Enums.AccountingCurrency.USD)}";
            ExpensesIqd = $"{FormatCurrency(result.ExpensesIqd)} / {FormatCurrency(result.ExpensesUsd, Core.Enums.AccountingCurrency.USD)}";
            VehicleCount = result.VehicleCount.ToString("N0");

            TypeSeries = ChartThemeConfig.PieFromNameAmount(
                result.ByTypeChart.Select(p => new NameAmountPoint { Name = p.Name, Amount = p.Amount }).ToList());

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
        var cols = new[] { "التاريخ", "النوع", "المرجع", "العميل", "العملة", "المبلغ", "البيان" };
        IList<object[]> rows = _allRows.Select(r => new object[]
        {
            r.Date.ToString("yyyy/MM/dd"), r.Kind, r.Reference, r.CustomerName,
            r.CurrencyLabel, r.Amount, r.Description
        }).ToList();
        var summary = new List<string>
        {
            $"فواتير: {InvoiceCount}",
            $"مقبوضات: {ReceiptsIqd}",
            $"مصاريف: {ExpensesIqd}",
            $"سيارات: {VehicleCount}"
        };
        return (cols, rows, summary);
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var (cols, rows, _) = BuildExport();
        ExportExcel($"تقرير_يومي_تخليص_{(DateFrom ?? DateTime.Today):yyyyMMdd}.xlsx", "اليومي", cols, rows);
    }

    [RelayCommand]
    private void Print()
    {
        var (cols, rows, summary) = BuildExport();
        PrintTable("التقرير اليومي — التخليص", cols, rows, summary);
    }

    [RelayCommand]
    private void ExportToPdf()
    {
        var (cols, rows, summary) = BuildExport();
        ExportPdfDocument(
            "التقرير اليومي — التخليص",
            SelectedCustomer?.Name ?? "الكل",
            cols, rows, summary,
            $"تقرير_يومي_تخليص_{(DateFrom ?? DateTime.Today):yyyyMMdd}.pdf");
    }
}
