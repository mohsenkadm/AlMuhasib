using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelReportsViewModel : ViewModelBase
{
    private readonly IHotelReportService _reportService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private DateTime? _dateFrom = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime? _dateTo = DateTime.Today;
    [ObservableProperty] private DateTime _auditDate = DateTime.Today;
    [ObservableProperty] private decimal _occupancyRate;
    [ObservableProperty] private int _soldRoomNights;
    [ObservableProperty] private decimal _totalRoomRevenue;
    [ObservableProperty] private decimal _totalPayments;
    [ObservableProperty] private decimal _outstandingBalance;
    [ObservableProperty] private ISeries[] _occupancyChartSeries = [];
    [ObservableProperty] private ISeries[] _revenueChartSeries = [];
    [ObservableProperty] private Axis[] _chartXAxes = [];
    [ObservableProperty] private Axis[] _chartYAxes = [];

    public ObservableCollection<OccupancyReportRow> OccupancyRows { get; } = [];
    public ObservableCollection<RevenueReportRow> RevenueRows { get; } = [];
    public ObservableCollection<NightAuditReservationRow> NightAuditInHouse { get; } = [];
    public ObservableCollection<NightAuditReservationRow> NightAuditArrivals { get; } = [];
    public ObservableCollection<NightAuditReservationRow> NightAuditDepartures { get; } = [];
    public ObservableCollection<NightAuditCashBoxRow> NightAuditCashBoxes { get; } = [];

    [ObservableProperty] private int _auditTotalRooms;
    [ObservableProperty] private int _auditOccupiedRooms;
    [ObservableProperty] private decimal _auditRoomRevenue;
    [ObservableProperty] private decimal _auditPaymentsCollected;

    public HotelReportsViewModel(
        IHotelReportService reportService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _reportService = reportService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "التقارير";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.HotelReports);
        await LoadCurrentTabAsync();
    }

    partial void OnSelectedTabIndexChanged(int value) => _ = LoadCurrentTabAsync();

    [RelayCommand]
    private async Task LoadCurrentTabAsync()
    {
        switch (SelectedTabIndex)
        {
            case 0: await LoadOccupancyAsync(); break;
            case 1: await LoadRevenueAsync(); break;
            case 2: await LoadNightAuditAsync(); break;
        }
    }

    [RelayCommand]
    private async Task LoadOccupancyAsync()
    {
        IsBusy = true;
        try
        {
            var data = await _reportService.GetOccupancyReportAsync(BuildFilter());
            OccupancyRate = data.AverageOccupancyRate;
            SoldRoomNights = data.SoldRoomNights;
            _allOccupancyRows = data.Rows.ToList();
            ApplyCurrentTabFilters();

            var chart = data.DailyOccupancyChart;
            OccupancyChartSeries = [ChartThemeConfig.Column(chart.Select(c => c.Amount).ToArray(), "الإشغال %", 0)];
            ChartXAxes = [ChartThemeConfig.CreateXAxis(chart.Select(c => c.Date.ToString("MM/dd")).ToArray(), -45)];
            ChartYAxes = [ChartThemeConfig.CreateYAxis()];
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadRevenueAsync()
    {
        IsBusy = true;
        try
        {
            var data = await _reportService.GetRevenueReportAsync(BuildFilter());
            TotalRoomRevenue = data.TotalRoomRevenue;
            TotalPayments = data.TotalPayments;
            OutstandingBalance = data.OutstandingBalance;
            _allRevenueRows = data.Rows.ToList();
            ApplyCurrentTabFilters();

            var chart = data.DailyRevenueChart;
            RevenueChartSeries = [ChartThemeConfig.Column(chart.Select(c => c.Amount).ToArray(), "الإيراد", 0)];
            ChartXAxes = [ChartThemeConfig.CreateXAxis(chart.Select(c => c.Date.ToString("MM/dd")).ToArray(), -45)];
            ChartYAxes = [ChartThemeConfig.CreateYAxis()];
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadNightAuditAsync()
    {
        IsBusy = true;
        try
        {
            var data = await _reportService.GetNightAuditReportAsync(AuditDate);
            AuditTotalRooms = data.TotalRooms;
            AuditOccupiedRooms = data.OccupiedRooms;
            AuditRoomRevenue = data.RoomRevenue;
            AuditPaymentsCollected = data.PaymentsCollected;

            _allNightAuditInHouse = data.InHouseGuests.ToList();
            ApplyCurrentTabFilters();
            NightAuditArrivals.Clear();
            foreach (var row in data.ExpectedArrivals) NightAuditArrivals.Add(row);
            NightAuditDepartures.Clear();
            foreach (var row in data.ExpectedDepartures) NightAuditDepartures.Add(row);
            NightAuditCashBoxes.Clear();
            foreach (var row in data.CashBoxBalances) NightAuditCashBoxes.Add(row);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        if (!CanExport)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"HotelReport_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true)
            return;

        switch (SelectedTabIndex)
        {
            case 0:
                _exportService.ExportToExcel(dialog.FileName, "الإشغال",
                    ["التاريخ", "الغرف", "المشغولة", "المتاحة", "نسبة الإشغال", "الوصول", "المغادرة"],
                    OccupancyRows.Select(r => new object?[]
                    {
                        r.Date.ToString("yyyy/MM/dd"), r.TotalRooms, r.OccupiedRooms, r.AvailableRooms,
                        r.OccupancyRate, r.Arrivals, r.Departures
                    }).ToList());
                break;
            case 1:
                _exportService.ExportToExcel(dialog.FileName, "الإيرادات",
                    ["التاريخ", "رقم الحجز", "النزيل", "الغرفة", "النوع", "الليالي", "إيراد الغرفة", "إضافات", "الإجمالي", "المدفوع", "المتبقي"],
                    RevenueRows.Select(r => new object?[]
                    {
                        r.Date.ToString("yyyy/MM/dd"), r.ReservationNumber, r.GuestName, r.RoomNumber,
                        r.RoomTypeName, r.Nights, r.RoomRevenue, r.ExtraCharges, r.TotalAmount,
                        r.AmountPaid, r.RemainingAmount
                    }).ToList());
                break;
            case 2:
                _exportService.ExportToExcel(dialog.FileName, "المراجعة الليلية",
                    ["رقم الحجز", "النزيل", "الغرفة", "الوصول", "المغادرة", "الحالة", "الإجمالي", "المتبقي"],
                    NightAuditInHouse.Select(r => new object?[]
                    {
                        r.ReservationNumber, r.GuestName, r.RoomNumber,
                        r.CheckInDate.ToString("yyyy/MM/dd"), r.CheckOutDate.ToString("yyyy/MM/dd"),
                        HotelDisplayHelper.GetReservationStatusLabel(r.Status), r.TotalAmount, r.RemainingAmount
                    }).ToList());
                break;
        }

        _toast.ShowSuccess("تم تصدير الملف");
        await Task.CompletedTask;
    }

    private List<OccupancyReportRow> _allOccupancyRows = [];
    private List<RevenueReportRow> _allRevenueRows = [];
    private List<NightAuditReservationRow> _allNightAuditInHouse = [];

    protected override void OnColumnFiltersChanged() => ApplyCurrentTabFilters();

    private void ApplyCurrentTabFilters()
    {
        switch (SelectedTabIndex)
        {
            case 0:
                ApplyFilteredRows(_allOccupancyRows, OccupancyRows);
                break;
            case 1:
                ApplyFilteredRows(_allRevenueRows, RevenueRows);
                break;
            case 2:
                ApplyFilteredRows(_allNightAuditInHouse, NightAuditInHouse);
                break;
        }
    }

    private void ApplyFilteredRows<T>(IReadOnlyList<T> source, ObservableCollection<T> target)
    {
        var items = MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters)
            ? ColumnFilterEngine.Apply(source, ColumnFilters).ToList()
            : source.ToList();

        target.Clear();
        foreach (var row in items)
            target.Add(row);
    }

    private HotelReportFilter BuildFilter() => new()
    {
        DateFrom = DateFrom,
        DateTo = DateTo
    };
}
