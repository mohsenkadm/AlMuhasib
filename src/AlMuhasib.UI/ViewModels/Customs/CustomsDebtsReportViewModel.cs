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
using NameAmountPoint = AlMuhasib.Core.Interfaces.Services.NameAmountPoint;

namespace AlMuhasib.UI.ViewModels.Customs;

public partial class CustomsDebtsReportViewModel : CustomsReportViewModelBase
{
    [ObservableProperty] private string _debtsIqd = "0";
    [ObservableProperty] private string _debtsUsd = "0";
    [ObservableProperty] private string _unpaidCount = "0";
    [ObservableProperty] private DateTime? _asOfDate = DateTime.Today;
    [ObservableProperty] private CustomsReportCustomerOption? _selectedCustomer;
    [ObservableProperty] private CustomsCurrencyFilterOption? _selectedCurrency;
    [ObservableProperty] private ISeries[] _agingSeries = [];
    [ObservableProperty] private bool _isAgingChartEmpty = true;

    public ObservableCollection<CustomsCurrencyFilterOption> CurrencyOptions { get; } =
    [
        new(null, "الكل"),
        new(AccountingCurrency.IQD, "دينار"),
        new(AccountingCurrency.USD, "دولار")
    ];

    private List<CustomsDebtsReportRow> _allRows = [];
    public ObservableCollection<CustomsDebtsReportRow> Rows { get; } = [];

    public CustomsDebtsReportViewModel(
        ICustomsReportService reportService,
        ICustomsPrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, printService, exportService, currentUserService)
    {
        PageTitle = "تقرير الديون — التخليص";
        SelectedCurrency = CurrencyOptions[0];
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, CustomsPermissionRegistry.Reports);
        PermissionScreen = CustomsPermissionRegistry.Reports;
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
            var asOf = AsOfDate ?? DateTime.Today;
            DateFrom = DateTo = asOf;
            var result = await ReportService.GetDebtsReportAsync(
                asOf,
                SelectedCustomer is { Id: > 0 } ? SelectedCustomer.Id : null,
                SelectedCurrency?.Value);

            DebtsIqd = FormatCurrency(result.DebtsIqd);
            DebtsUsd = FormatCurrency(result.DebtsUsd, AccountingCurrency.USD);
            UnpaidCount = result.UnpaidCount.ToString("N0");
            AgingSeries = ChartThemeConfig.PieFromNameAmount(
                result.AgingChart.Select(p => new NameAmountPoint { Name = p.Name, Amount = p.Amount }).ToList());
            IsAgingChartEmpty = result.AgingChart.Count == 0 || AgingSeries.Length == 0;

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
        var cols = new[] { "الفاتورة", "التاريخ", "العميل", "العملة", "المتبقي", "أيام التأخير", "الفئة" };
        IList<object[]> rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.InvoiceDate.ToString("yyyy/MM/dd"), r.CustomerName,
            r.CurrencyLabel, r.RemainingAmount, r.DaysOverdue, r.AgingBucket
        }).ToList();
        return (cols, rows, [$"ديون د.ع: {DebtsIqd}", $"ديون $: {DebtsUsd}", $"عدد: {UnpaidCount}"]);
    }

    [RelayCommand] private void ExportToExcel()
    {
        var (c, r, _) = BuildExport();
        ExportExcel($"تقرير_ديون_تخليص_{DateTime.Now:yyyyMMdd}.xlsx", "ديون", c, r);
    }

    [RelayCommand] private void Print()
    {
        var (c, r, s) = BuildExport();
        PrintTable("تقرير الديون — التخليص", c, r, s);
    }

    [RelayCommand] private void ExportToPdf()
    {
        var (c, r, s) = BuildExport();
        ExportPdfDocument("تقرير الديون — التخليص", SelectedCustomer?.Name ?? "الكل", c, r, s,
            $"تقرير_ديون_تخليص_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
