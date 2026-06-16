using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.Globalization;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelReservationsCalendarViewModel : ViewModelBase
{
    private readonly IReservationService _reservationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;
    private List<ReservationListItem> _monthReservations = [];

    public ObservableCollection<CalendarDayCell> CalendarDays { get; } = [];
    public ObservableCollection<ReservationListItem> SelectedDayReservations { get; } = [];

    [ObservableProperty] private DateTime _displayMonth;
    [ObservableProperty] private string _monthTitle = string.Empty;
    [ObservableProperty] private DateTime? _selectedDate;
    [ObservableProperty] private string _selectedDayTitle = string.Empty;
    [ObservableProperty] private bool _isLoaded;

    public HotelReservationsCalendarViewModel(
        IReservationService reservationService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _reservationService = reservationService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "تقويم الحجوزات";
        _displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.ReservationsCalendar);
        await LoadMonthAsync();
    }

    [RelayCommand]
    private async Task LoadMonthAsync()
    {
        IsLoaded = false;
        try
        {
            var monthStart = new DateTime(DisplayMonth.Year, DisplayMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            MonthTitle = monthStart.ToString("MMMM yyyy", new CultureInfo("ar-IQ"));

            var (items, _) = await _reservationService.SearchPagedAsync(new ReservationFilter
            {
                CheckInTo = monthEnd,
                CheckOutFrom = monthStart
            }, 1, 500);

            _monthReservations = items.ToList();
            BuildCalendarGrid(monthStart, monthEnd);

            if (SelectedDate.HasValue && SelectedDate.Value.Month == monthStart.Month)
                SelectDay(SelectedDate.Value);
            else
            {
                SelectedDate = DateTime.Today.Month == monthStart.Month ? DateTime.Today : monthStart;
                SelectDay(SelectedDate.Value);
            }
        }
        finally
        {
            IsLoaded = true;
        }
    }

    [RelayCommand]
    private async Task PreviousMonthAsync()
    {
        DisplayMonth = DisplayMonth.AddMonths(-1);
        await LoadMonthAsync();
    }

    [RelayCommand]
    private async Task NextMonthAsync()
    {
        DisplayMonth = DisplayMonth.AddMonths(1);
        await LoadMonthAsync();
    }

    [RelayCommand]
    private async Task GoToTodayAsync()
    {
        DisplayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        await LoadMonthAsync();
    }

    [RelayCommand]
    private void SelectDay(CalendarDayCell? cell)
    {
        if (cell is null || !cell.IsCurrentMonth)
            return;

        SelectDay(cell.Date);
    }

    [RelayCommand]
    private async Task OpenNewReservationAsync()
    {
        await _mainWindow.OpenTabAsync(
            typeof(HotelReservationFormViewModel),
            "حجز جديد",
            PackIconKind.CalendarPlus,
            activateIfExists: false);
    }

    [RelayCommand]
    private async Task OpenReservationsListAsync()
    {
        await _mainWindow.OpenTabAsync(
            typeof(HotelReservationsViewModel),
            "الحجوزات",
            PackIconKind.CalendarClock);
    }

    private List<ReservationListItem> _selectedDayAll = [];

    protected override void OnColumnFiltersChanged() => ApplyDayReservationFilters();

    private void ApplyDayReservationFilters()
    {
        var items = MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters)
            ? ColumnFilterEngine.Apply(_selectedDayAll, ColumnFilters).ToList()
            : _selectedDayAll;

        SelectedDayReservations.Clear();
        foreach (var r in items)
            SelectedDayReservations.Add(r);
    }

    private void SelectDay(DateTime date)
    {
        SelectedDate = date.Date;
        SelectedDayTitle = date.ToString("dddd dd MMMM", new CultureInfo("ar-IQ"));

        _selectedDayAll = _monthReservations
            .Where(r => r.CheckInDate.Date <= date && r.CheckOutDate.Date > date)
            .ToList();
        ApplyDayReservationFilters();

        foreach (var day in CalendarDays)
            day.IsSelected = day.Date == date.Date;
    }

    private void BuildCalendarGrid(DateTime monthStart, DateTime monthEnd)
    {
        CalendarDays.Clear();

        var firstCell = monthStart;
        while (firstCell.DayOfWeek != DayOfWeek.Saturday)
            firstCell = firstCell.AddDays(-1);

        var lastCell = monthEnd;
        while (lastCell.DayOfWeek != DayOfWeek.Friday)
            lastCell = lastCell.AddDays(1);

        for (var day = firstCell; day <= lastCell; day = day.AddDays(1))
        {
            var isCurrentMonth = day.Month == monthStart.Month;
            var count = isCurrentMonth
                ? _monthReservations.Count(r => r.CheckInDate.Date <= day && r.CheckOutDate.Date > day)
                : 0;

            CalendarDays.Add(new CalendarDayCell
            {
                Date = day,
                DayNumber = day.Day,
                IsCurrentMonth = isCurrentMonth,
                IsToday = day.Date == DateTime.Today,
                ReservationCount = count,
                HasReservations = count > 0
            });
        }
    }
}

public partial class CalendarDayCell : ObservableObject
{
    [ObservableProperty] private DateTime _date;
    [ObservableProperty] private int _dayNumber;
    [ObservableProperty] private bool _isCurrentMonth;
    [ObservableProperty] private bool _isToday;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private int _reservationCount;
    [ObservableProperty] private bool _hasReservations;
}
