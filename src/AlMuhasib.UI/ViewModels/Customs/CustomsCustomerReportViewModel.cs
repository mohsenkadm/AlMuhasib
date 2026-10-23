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

public partial class CustomsCustomerReportViewModel : CustomsReportViewModelBase
{
    [ObservableProperty] private string _customerCount = "0";
    [ObservableProperty] private string _balanceIqd = "0";
    [ObservableProperty] private string _balanceUsd = "0";
    [ObservableProperty] private string _invoiceCount = "0";
    [ObservableProperty] private CustomsReportCustomerOption? _selectedCustomer;
    [ObservableProperty] private ISeries[] _topSeries = [];

    private List<CustomsCustomerReportRow> _allRows = [];
    public ObservableCollection<CustomsCustomerReportRow> Rows { get; } = [];

    public CustomsCustomerReportViewModel(
        ICustomsReportService reportService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, exportService, currentUserService)
    {
        PageTitle = "تقرير العملاء — التخليص";
        RegisterThemeChartReload(LoadDataAsync);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(CurrentUserService, CustomsPermissionRegistry.Reports);
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
            var customerId = SelectedCustomer is { Id: > 0 } ? SelectedCustomer.Id : (int?)null;
            var result = await ReportService.GetCustomerReportAsync(from, to, customerId);

            CustomerCount = result.CustomerCount.ToString("N0");
            BalanceIqd = FormatCurrency(result.BalanceIqd);
            BalanceUsd = FormatCurrency(result.BalanceUsd, AccountingCurrency.USD);
            InvoiceCount = result.InvoiceCount.ToString("N0");
            TopSeries = ChartThemeConfig.PieFromNameAmount(
                result.TopCustomersChart.Select(p => new NameAmountPoint { Name = p.Name, Amount = p.Amount }).ToList());

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
            "العميل", "الهاتف", "رصيد د.ع", "رصيد $", "فواتير",
            "إيراد د.ع", "إيراد $", "مدفوع د.ع", "مدفوع $", "متبقي د.ع", "متبقي $"
        };
        IList<object[]> rows = _allRows.Select(r => new object[]
        {
            r.Name, r.Phone, r.BalanceIqd, r.BalanceUsd, r.InvoiceCount,
            r.RevenueIqd, r.RevenueUsd, r.PaidIqd, r.PaidUsd, r.RemainingIqd, r.RemainingUsd
        }).ToList();
        return (cols, rows, [$"عملاء: {CustomerCount}", $"رصيد د.ع: {BalanceIqd}", $"رصيد $: {BalanceUsd}"]);
    }

    [RelayCommand] private void ExportToExcel()
    {
        var (c, r, _) = BuildExport();
        ExportExcel($"تقرير_عملاء_تخليص_{DateTime.Now:yyyyMMdd}.xlsx", "عملاء", c, r);
    }

    [RelayCommand] private void Print()
    {
        var (c, r, s) = BuildExport();
        PrintTable("تقرير العملاء — التخليص", c, r, s);
    }

    [RelayCommand] private void ExportToPdf()
    {
        var (c, r, s) = BuildExport();
        ExportPdfDocument("تقرير العملاء — التخليص", SelectedCustomer?.Name ?? "الكل", c, r, s,
            $"تقرير_عملاء_تخليص_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
