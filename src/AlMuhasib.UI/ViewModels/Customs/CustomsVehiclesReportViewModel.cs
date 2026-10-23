using System.Collections.ObjectModel;
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

public sealed record CustomsVehicleStatusFilterOption(CustomsVehicleStatus? Value, string Label);

public partial class CustomsVehiclesReportViewModel : CustomsReportViewModelBase
{
    [ObservableProperty] private string _totalText = "0";
    [ObservableProperty] private string _inProgressText = "0";
    [ObservableProperty] private string _exitedText = "0";
    [ObservableProperty] private string _arrivedText = "0";
    [ObservableProperty] private CustomsReportCustomerOption? _selectedCustomer;
    [ObservableProperty] private CustomsVehicleStatusFilterOption? _selectedStatus;
    [ObservableProperty] private ISeries[] _statusSeries = [];

    public ObservableCollection<CustomsVehicleStatusFilterOption> StatusOptions { get; } =
    [
        new(null, "الكل"),
        new(CustomsVehicleStatus.InProgress, "قيد الإجراء"),
        new(CustomsVehicleStatus.Exited, "خارجة"),
        new(CustomsVehicleStatus.Arrived, "واصلة")
    ];

    private List<CustomsVehiclesReportRow> _allRows = [];
    public ObservableCollection<CustomsVehiclesReportRow> Rows { get; } = [];

    public CustomsVehiclesReportViewModel(
        ICustomsReportService reportService,
        ICustomsPrintService printService,
        IExportService exportService,
        ICurrentUserService currentUserService)
        : base(reportService, printService, exportService, currentUserService)
    {
        PageTitle = "تقرير السيارات — التخليص";
        SelectedStatus = StatusOptions[0];
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
            var result = await ReportService.GetVehiclesReportAsync(
                from, to,
                SelectedCustomer is { Id: > 0 } ? SelectedCustomer.Id : null,
                SelectedStatus?.Value);

            TotalText = result.Total.ToString("N0");
            InProgressText = result.InProgress.ToString("N0");
            ExitedText = result.Exited.ToString("N0");
            ArrivedText = result.Arrived.ToString("N0");
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
        var cols = new[]
        {
            "لوحة عراقية", "لوحة سورية", "الهيكل", "الحالة", "التحميل", "الخروج",
            "الفاتورة", "العميل", "العملة", "السعر"
        };
        IList<object[]> rows = _allRows.Select(r => new object[]
        {
            r.IraqiPlate, r.SyrianPlate, r.SerialNumber, r.StatusLabel,
            r.LoadingDate?.ToString("yyyy/MM/dd") ?? "",
            r.ExitDate?.ToString("yyyy/MM/dd") ?? "",
            r.InvoiceNumber, r.CustomerName, r.CurrencyLabel, r.VehiclePrice
        }).ToList();
        return (cols, rows, [$"الإجمالي: {TotalText}", $"قيد الإجراء: {InProgressText}", $"خارجة: {ExitedText}"]);
    }

    [RelayCommand] private void ExportToExcel()
    {
        var (c, r, _) = BuildExport();
        ExportExcel($"تقرير_سيارات_تخليص_{DateTime.Now:yyyyMMdd}.xlsx", "سيارات", c, r);
    }

    [RelayCommand] private void Print()
    {
        var (c, r, s) = BuildExport();
        PrintStaffReport(CustomsPrintDocumentKind.VehicleReport, "تقرير السيارات — التخليص", c, r, s);
    }

    [RelayCommand] private void ExportToPdf()
    {
        var (c, r, s) = BuildExport();
        ExportStaffReportPdf(
            CustomsPrintDocumentKind.VehicleReport,
            "تقرير السيارات — التخليص", "الفترة المحددة", c, r, s,
            $"تقرير_سيارات_تخليص_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
