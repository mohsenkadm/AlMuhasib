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
using NameAmountPoint = AlMuhasib.Core.Interfaces.Services.NameAmountPoint;

namespace AlMuhasib.UI.ViewModels.Customs;

public sealed record CustomsStatusFilterOption(CustomsInvoiceStatus? Value, string Label);
public sealed record CustomsServiceFilterOption(CustomsServiceType? Value, string Label);

public partial class CustomsInvoiceReportViewModel : CustomsReportViewModelBase
{
    [ObservableProperty] private string _countText = "0";
    [ObservableProperty] private string _totalIqd = "0";
    [ObservableProperty] private string _totalUsd = "0";
    [ObservableProperty] private string _remainingText = "0";
    [ObservableProperty] private CustomsReportCustomerOption? _selectedCustomer;
    [ObservableProperty] private CustomsStatusFilterOption? _selectedStatus;
    [ObservableProperty] private CustomsCurrencyFilterOption? _selectedCurrency;
    [ObservableProperty] private CustomsServiceFilterOption? _selectedServiceType;
    [ObservableProperty] private ISeries[] _statusSeries = [];

    public ObservableCollection<CustomsStatusFilterOption> StatusOptions { get; } =
    [
        new(null, "الكل (غير الملغاة)"),
        new(CustomsInvoiceStatus.Draft, "مسودة"),
        new(CustomsInvoiceStatus.Approved, "معتمدة"),
        new(CustomsInvoiceStatus.Cancelled, "ملغاة")
    ];

    public ObservableCollection<CustomsCurrencyFilterOption> CurrencyOptions { get; } =
    [
        new(null, "الكل"),
        new(AccountingCurrency.IQD, "دينار"),
        new(AccountingCurrency.USD, "دولار")
    ];

    public ObservableCollection<CustomsServiceFilterOption> ServiceTypeOptions { get; } =
    [
        new(null, "الكل"),
        new(CustomsServiceType.Clearance, "تخليص"),
        new(CustomsServiceType.Transport, "نقل"),
        new(CustomsServiceType.ClearanceAndTransport, "تخليص ونقل")
    ];

    private List<CustomsInvoiceReportRow> _allRows = [];
    public ObservableCollection<CustomsInvoiceReportRow> Rows { get; } = [];

    public CustomsInvoiceReportViewModel(
        ICustomsReportService reportService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, exportService, currentUserService)
    {
        PageTitle = "تقرير الفواتير — التخليص";
        SelectedStatus = StatusOptions[0];
        SelectedCurrency = CurrencyOptions[0];
        SelectedServiceType = ServiceTypeOptions[0];
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
            var result = await ReportService.GetInvoiceReportAsync(
                from, to,
                SelectedCustomer is { Id: > 0 } ? SelectedCustomer.Id : null,
                SelectedStatus?.Value,
                SelectedCurrency?.Value,
                SelectedServiceType?.Value);

            CountText = result.Count.ToString("N0");
            TotalIqd = FormatCurrency(result.TotalIqd);
            TotalUsd = FormatCurrency(result.TotalUsd, AccountingCurrency.USD);
            RemainingText = $"{FormatCurrency(result.RemainingIqd)} / {FormatCurrency(result.RemainingUsd, AccountingCurrency.USD)}";
            StatusSeries = ChartThemeConfig.PieFromNameAmount(
                result.StatusChart.Select(p => new NameAmountPoint { Name = p.Name, Amount = p.Amount }).ToList());

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
        var cols = new[] { "الرقم", "التاريخ", "العميل", "الحالة", "الخدمة", "العملة", "الإجمالي", "المدفوع", "المتبقي" };
        IList<object[]> rows = _allRows.Select(r => new object[]
        {
            r.InvoiceNumber, r.InvoiceDate.ToString("yyyy/MM/dd"), r.CustomerName,
            r.StatusLabel, r.ServiceTypeLabel, r.CurrencyLabel,
            r.TotalAmount, r.PaidAmount, r.RemainingAmount
        }).ToList();
        return (cols, rows, [$"العدد: {CountText}", $"إجمالي د.ع: {TotalIqd}", $"متبقي: {RemainingText}"]);
    }

    [RelayCommand] private void ExportToExcel()
    {
        var (c, r, _) = BuildExport();
        ExportExcel($"تقرير_فواتير_تخليص_{DateTime.Now:yyyyMMdd}.xlsx", "فواتير", c, r);
    }

    [RelayCommand] private void Print()
    {
        var (c, r, s) = BuildExport();
        PrintTable("تقرير الفواتير — التخليص", c, r, s);
    }

    [RelayCommand] private void ExportToPdf()
    {
        var (c, r, s) = BuildExport();
        ExportPdfDocument("تقرير الفواتير — التخليص", "الفترة المحددة", c, r, s,
            $"تقرير_فواتير_تخليص_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
