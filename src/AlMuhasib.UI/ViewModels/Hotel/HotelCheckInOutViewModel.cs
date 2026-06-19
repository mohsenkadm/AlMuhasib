using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelCheckInOutViewModel : ViewModelBase
{
    private readonly ICheckInOutService _checkInOutService;
    private readonly IHotelMasterDataService _masterDataService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;

    [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
    [ObservableProperty] private ReservationListItem? _selectedArrival;
    [ObservableProperty] private ReservationListItem? _selectedDeparture;
    [ObservableProperty] private int? _checkInRoomId;
    [ObservableProperty] private bool _isCheckInDialogOpen;

    public ObservableCollection<ReservationListItem> Arrivals { get; } = [];
    public ObservableCollection<ReservationListItem> Departures { get; } = [];
    public ObservableCollection<RoomOption> AvailableRooms { get; } = [];

    private List<ReservationListItem> _allArrivals = [];
    private List<ReservationListItem> _allDepartures = [];
    private readonly Dictionary<string, string> _arrivalColumnFilters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _departureColumnFilters = new(StringComparer.OrdinalIgnoreCase);

    [ObservableProperty] private bool _isArrivalColumnFilterPanelOpen;
    [ObservableProperty] private bool _isDepartureColumnFilterPanelOpen;
    [ObservableProperty] private int _arrivalActiveColumnFilterCount;
    [ObservableProperty] private int _departureActiveColumnFilterCount;

    private void ApplyArrivalFilters()
    {
        var items = MasterDataColumnFilterHelper.HasActiveColumnFilters(_arrivalColumnFilters)
            ? ColumnFilterEngine.Apply(_allArrivals, _arrivalColumnFilters).ToList()
            : _allArrivals;

        Arrivals.Clear();
        foreach (var item in items)
            Arrivals.Add(item);
    }

    private void ApplyDepartureFilters()
    {
        var items = MasterDataColumnFilterHelper.HasActiveColumnFilters(_departureColumnFilters)
            ? ColumnFilterEngine.Apply(_allDepartures, _departureColumnFilters).ToList()
            : _allDepartures;

        Departures.Clear();
        foreach (var item in items)
            Departures.Add(item);
    }

    [RelayCommand]
    private void ApplyArrivalColumnFilters(Dictionary<string, string>? filters)
    {
        _arrivalColumnFilters.Clear();
        if (filters is not null)
        {
            foreach (var kv in filters)
                _arrivalColumnFilters[kv.Key] = kv.Value;
        }

        ArrivalActiveColumnFilterCount = _arrivalColumnFilters.Count(kv => !string.IsNullOrWhiteSpace(kv.Value));
        ApplyArrivalFilters();
    }

    [RelayCommand]
    private void ClearArrivalColumnFilters()
    {
        _arrivalColumnFilters.Clear();
        ArrivalActiveColumnFilterCount = 0;
        ApplyArrivalFilters();
    }

    [RelayCommand]
    private void ApplyDepartureColumnFilters(Dictionary<string, string>? filters)
    {
        _departureColumnFilters.Clear();
        if (filters is not null)
        {
            foreach (var kv in filters)
                _departureColumnFilters[kv.Key] = kv.Value;
        }

        DepartureActiveColumnFilterCount = _departureColumnFilters.Count(kv => !string.IsNullOrWhiteSpace(kv.Value));
        ApplyDepartureFilters();
    }

    [RelayCommand]
    private void ClearDepartureColumnFilters()
    {
        _departureColumnFilters.Clear();
        DepartureActiveColumnFilterCount = 0;
        ApplyDepartureFilters();
    }

    public HotelCheckInOutViewModel(
        ICheckInOutService checkInOutService,
        IHotelMasterDataService masterDataService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _checkInOutService = checkInOutService;
        _masterDataService = masterDataService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "تسجيل دخول/خروج";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.CheckInOut);
        await LoadDataAsync();
    }

    partial void OnSelectedDateChanged(DateTime value) => _ = LoadDataAsync();

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var arrivals = await _checkInOutService.GetTodayArrivalsAsync(SelectedDate);
            _allArrivals = arrivals.ToList();

            var departures = await _checkInOutService.GetTodayDeparturesAsync(SelectedDate);
            _allDepartures = departures.ToList();

            ApplyArrivalFilters();
            ApplyDepartureFilters();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenCheckInDialogAsync(ReservationListItem? item)
    {
        item ??= SelectedArrival;
        if (item is null || !CanEdit)
            return;

        SelectedArrival = item;
        await LoadAvailableRoomsAsync();
        CheckInRoomId = AvailableRooms.FirstOrDefault()?.Id;
        IsCheckInDialogOpen = true;
    }

    [RelayCommand]
    private void CloseCheckInDialog() => IsCheckInDialogOpen = false;

    [RelayCommand]
    private async Task ConfirmCheckInAsync()
    {
        if (SelectedArrival is null)
            return;

        try
        {
            await _checkInOutService.CheckInAsync(
                SelectedArrival.Id,
                CheckInRoomId,
                DateTime.Now,
                _currentUserService.Username);
            IsCheckInDialogOpen = false;
            _toast.ShowSuccess("تم تسجيل الدخول");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task CheckOutAsync(ReservationListItem? item)
    {
        item ??= SelectedDeparture;
        if (item is null || !CanEdit)
            return;

        try
        {
            await _checkInOutService.CheckOutAsync(item.Id, DateTime.Now, _currentUserService.Username);
            _toast.ShowSuccess("تم تسجيل المغادرة");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    private async Task LoadAvailableRoomsAsync()
    {
        AvailableRooms.Clear();
        var rooms = await _masterDataService.GetRoomsAsync();
        foreach (var room in rooms.Where(r => r.Status == Core.Enums.RoomStatus.Available))
            AvailableRooms.Add(new RoomOption(room.Id, room.RoomNumber, room.FloorName, room.Status));
    }
}
