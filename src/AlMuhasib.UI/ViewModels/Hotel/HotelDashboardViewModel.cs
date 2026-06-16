using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Charts;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelDashboardViewModel : ViewModelBase
{
    private readonly IHotelDashboardService _dashboardService;
    private readonly IHotelSmartAlertService _alertService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;

    [ObservableProperty] private bool _isLoaded;
    [ObservableProperty] private int _totalRooms;
    [ObservableProperty] private int _availableRooms;
    [ObservableProperty] private int _occupiedRooms;
    [ObservableProperty] private int _todayArrivalCount;
    [ObservableProperty] private int _todayDepartureCount;
    [ObservableProperty] private int _inHouseGuests;
    [ObservableProperty] private decimal _occupancyRate;
    [ObservableProperty] private decimal _todayRevenue;
    [ObservableProperty] private decimal _monthRevenue;
    [ObservableProperty] private decimal _outstandingBalances;
    [ObservableProperty] private ISeries[] _revenueSeries = [];
    [ObservableProperty] private ISeries[] _roomStatusSeries = [];
    [ObservableProperty] private ISeries[] _revenueByTypeSeries = [];
    [ObservableProperty] private Axis[] _revenueXAxes = [];
    [ObservableProperty] private Axis[] _revenueYAxes = [];

    public ObservableCollection<ReservationListItem> RecentReservations { get; } = [];
    public ObservableCollection<ReservationListItem> TodayArrivalList { get; } = [];
    public ObservableCollection<SmartAlert> SmartAlerts { get; } = [];

    public HotelDashboardViewModel(
        IHotelDashboardService dashboardService,
        IHotelSmartAlertService alertService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _dashboardService = dashboardService;
        _alertService = alertService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "لوحة التحكم";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.Dashboard);
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoaded = false;
        try
        {
            var stats = await _dashboardService.GetDashboardStatsAsync();
            TotalRooms = stats.TotalRooms;
            AvailableRooms = stats.AvailableRooms;
            OccupiedRooms = stats.OccupiedRooms;
            TodayArrivalCount = stats.TodayArrivals;
            TodayDepartureCount = stats.TodayDepartures;
            InHouseGuests = stats.InHouseGuests;
            OccupancyRate = stats.OccupancyRate;
            TodayRevenue = stats.TodayRevenue;
            MonthRevenue = stats.MonthRevenue;
            OutstandingBalances = stats.OutstandingBalances;

            RecentReservations.Clear();
            foreach (var item in stats.RecentReservations)
                RecentReservations.Add(item);

            TodayArrivalList.Clear();
            foreach (var item in stats.TodayArrivalList)
                TodayArrivalList.Add(item);

            var trend = stats.RevenueTrend;
            RevenueSeries = [ChartThemeConfig.Column(trend.Select(t => t.Amount).ToArray(), "الإيراد", 0)];
            RevenueXAxes = [ChartThemeConfig.CreateXAxis(trend.Select(t => t.Date.ToString("MM/dd")).ToArray(), -45)];
            RevenueYAxes = [ChartThemeConfig.CreateYAxis()];

            RoomStatusSeries = stats.RoomStatusChart
                .Select((p, i) => (ISeries)ChartThemeConfig.Pie(p.Count, p.Name, i))
                .ToArray();

            RevenueByTypeSeries = stats.RevenueByRoomType
                .Select((p, i) => (ISeries)ChartThemeConfig.Pie(p.Amount, p.Name, i))
                .ToArray();

            SmartAlerts.Clear();
            foreach (var alert in await _alertService.GetAlertsAsync())
                SmartAlerts.Add(alert);
        }
        finally
        {
            IsLoaded = true;
        }
    }

    [RelayCommand]
    private async Task OpenReservationsAsync() =>
        await _mainWindow.OpenTabAsync(typeof(HotelReservationsViewModel), "الحجوزات", PackIconKind.CalendarClock);

    [RelayCommand]
    private async Task OpenNewReservationAsync() =>
        await _mainWindow.OpenTabAsync(typeof(HotelReservationFormViewModel), "حجز جديد", PackIconKind.CalendarPlus, activateIfExists: false);

    [RelayCommand]
    private async Task OpenCheckInOutAsync() =>
        await _mainWindow.OpenTabAsync(typeof(HotelCheckInOutViewModel), "تسجيل دخول/خروج", PackIconKind.Login);
}
