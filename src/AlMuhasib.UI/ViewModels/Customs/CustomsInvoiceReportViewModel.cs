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
using NameAmountPoint = AlMuhasib.Core.Interfaces.Services.NameAmountPoint;

namespace AlMuhasib.UI.ViewModels.Customs;

public sealed record CustomsStatusFilterOption(CustomsInvoiceStatus? Value, string Label);
public sealed record CustomsServiceFilterOption(CustomsServiceType? Value, string Label);
public sealed record CustomsRemainingFilterOption(bool? Value, string Label);

public partial class CustomsInvoiceReportViewModel : CustomsReportViewModelBase
{
    [ObservableProperty] private string _countText = "0";
    [ObservableProperty] private string _totalIqd = "0";
    [ObservableProperty] private string _totalUsd = "0";
    [ObservableProperty] private string _paidText = "0";
    [ObservableProperty] private string _remainingText = "0";
    [ObservableProperty] private string _averageText = "0";
    [ObservableProperty] private string _todayText = "0";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private CustomsReportCustomerOption? _selectedCustomer;
    [ObservableProperty] private CustomsStatusFilterOption? _selectedStatus;
    [ObservableProperty] private CustomsCurrencyFilterOption? _selectedCurrency;
    [ObservableProperty] private CustomsServiceFilterOption? _selectedServiceType;
    [ObservableProperty] private CustomsRemainingFilterOption? _selectedRemaining;
    [ObservableProperty] private ISeries[] _statusSeries = [];
    [ObservableProperty] private ISeries[] _serviceSeries = [];
    [ObservableProperty] private ISeries[] _dailySeries = [];
    [ObservableProperty] private Axis[] _dailyXAxes = [];
    [ObservableProperty] private Axis[] _dailyYAxes = [];
    [ObservableProperty] private bool _isStatusChartEmpty = true;
    [ObservableProperty] private bool _isServiceChartEmpty = true;
    [ObservableProperty] private bool _isDailyChartEmpty = true;

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

    public ObservableCollection<CustomsRemainingFilterOption> RemainingOptions { get; } =
    [
        new(null, "الكل"),
        new(true, "عليها متبقي"),
        new(false, "مسددة بالكامل")
    ];

    private List<CustomsInvoiceReportRow> _allRows = [];
    public ObservableCollection<CustomsInvoiceReportRow> Rows { get; } = [];

    public CustomsInvoiceReportViewModel(
        ICustomsReportService reportService,
        ICustomsPrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, printService, exportService, currentUserService)
    {
        PageTitle = "تقرير الفواتير — التخليص";
        SelectedStatus = StatusOptions[0];
        SelectedCurrency = CurrencyOptions[0];
        SelectedServiceType = ServiceTypeOptions[0];
        SelectedRemaining = RemainingOptions[0];
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
            var from = DateFrom ?? DateTime.Today.AddMonths(-1);
            var to = DateTo ?? DateTime.Today;
            var result = await ReportService.GetInvoiceReportAsync(
                from, to,
                SelectedCustomer is { Id: > 0 } ? SelectedCustomer.Id : null,
                SelectedStatus?.Value,
                SelectedCurrency?.Value,
                SelectedServiceType?.Value,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                SelectedRemaining?.Value);

            CountText = result.Count.ToString("N0");
            TotalIqd = FormatCurrency(result.TotalIqd);
            TotalUsd = FormatCurrency(result.TotalUsd, AccountingCurrency.USD);
            PaidText = $"{FormatCurrency(result.PaidIqd)} / {FormatCurrency(result.PaidUsd, AccountingCurrency.USD)}";
            RemainingText = $"{FormatCurrency(result.RemainingIqd)} / {FormatCurrency(result.RemainingUsd, AccountingCurrency.USD)}";
            AverageText = $"{FormatCurrency(result.AverageIqd)} / {FormatCurrency(result.AverageUsd, AccountingCurrency.USD)}";
            TodayText = $"{result.TodayCount:N0} — {FormatCurrency(result.TodayIqd)} / {FormatCurrency(result.TodayUsd, AccountingCurrency.USD)}";

            StatusSeries = ChartThemeConfig.PieFromNameAmount(
                result.StatusChart.Select(p => new NameAmountPoint { Name = p.Name, Amount = p.Amount }).ToList());
            IsStatusChartEmpty = result.StatusChart.Count == 0 || StatusSeries.Length == 0;

            ServiceSeries = ChartThemeConfig.PieFromNameAmount(
                result.ServiceTypeChart.Select(p => new NameAmountPoint { Name = p.Name, Amount = p.Amount }).ToList());
            IsServiceChartEmpty = result.ServiceTypeChart.Count == 0 || ServiceSeries.Length == 0;

            if (result.DailyChart.Count > 0)
            {
                DailySeries = [ChartThemeConfig.Line(result.DailyChart.Select(d => d.Amount).ToArray(), "الإجمالي", 0)];
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

    [RelayCommand]
    private async Task ApplyDatePresetAsync(string? preset)
    {
        switch (preset)
        {
            case "today": SetDateToday(); break;
            case "week": SetDateWeek(); break;
            case "month": SetDateMonth(); break;
            default: ResetDateFilters(); break;
        }
        await LoadDataAsync();
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
        return (cols, rows, [
            $"العدد: {CountText}",
            $"إجمالي د.ع: {TotalIqd}",
            $"إجمالي $: {TotalUsd}",
            $"المدفوع: {PaidText}",
            $"متبقي: {RemainingText}"
        ]);
    }

    [RelayCommand] private void ExportToExcel()
    {
        var (c, r, _) = BuildExport();
        ExportExcel($"تقرير_فواتير_تخليص_{DateTime.Now:yyyyMMdd}.xlsx", "فواتير", c, r);
    }

    [RelayCommand] private void Print()
    {
        var (c, r, s) = BuildExport();
        PrintStaffReport(
            CustomsPrintDocumentKind.InvoiceReport,
            "تقرير الفواتير — التخليص", c, r, s,
            "الفترة المحددة");
    }

    [RelayCommand] private void ExportToPdf()
    {
        var (c, r, s) = BuildExport();
        ExportStaffReportPdf(
            CustomsPrintDocumentKind.InvoiceReport,
            "تقرير الفواتير — التخليص", "الفترة المحددة", c, r, s,
            $"تقرير_فواتير_تخليص_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
