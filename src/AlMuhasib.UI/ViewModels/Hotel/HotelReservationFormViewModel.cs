using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelReservationFormViewModel : ViewModelBase
{
    private readonly IReservationService _reservationService;
    private readonly IGuestService _guestService;
    private readonly IHotelMasterDataService _masterDataService;
    private readonly IRatePlanService _ratePlanService;
    private readonly IHotelInvoicePrintService _printService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly MainWindowViewModel _mainWindow;
    private int? _editId;

    [ObservableProperty] private string _reservationNumber = "يُنشأ تلقائياً";
    [ObservableProperty] private DateTime _checkInDate = DateTime.Today;
    [ObservableProperty] private DateTime _checkOutDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private int _guestCount = 1;
    [ObservableProperty] private int? _selectedGuestId;
    [ObservableProperty] private string _guestSearchText = string.Empty;
    [ObservableProperty] private int? _selectedRoomTypeId;
    [ObservableProperty] private int? _selectedRoomId;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _amountPaid;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private bool _isSaving;
    [ObservableProperty] private bool _isGuestPickerOpen;
    [ObservableProperty] private bool _isNewGuestDialogOpen;
    [ObservableProperty] private string _newGuestName = string.Empty;
    [ObservableProperty] private string _newGuestPhone = string.Empty;
    [ObservableProperty] private string _newGuestIdNumber = string.Empty;
    [ObservableProperty] private string _selectedGuestDisplay = string.Empty;

    public ObservableCollection<GuestListItem> GuestSearchResults { get; } = [];
    public ObservableCollection<RoomTypeOption> RoomTypes { get; } = [];
    public ObservableCollection<RoomOption> AvailableRooms { get; } = [];

    public bool IsEditMode => _editId.HasValue;
    public bool CanSave => IsEditMode ? CanEdit : CanAdd;

    public HotelReservationFormViewModel(
        IReservationService reservationService,
        IGuestService guestService,
        IHotelMasterDataService masterDataService,
        IRatePlanService ratePlanService,
        IHotelInvoicePrintService printService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        MainWindowViewModel mainWindow)
    {
        _reservationService = reservationService;
        _guestService = guestService;
        _masterDataService = masterDataService;
        _ratePlanService = ratePlanService;
        _printService = printService;
        _currentUserService = currentUserService;
        _toast = toast;
        _mainWindow = mainWindow;
        PageTitle = "حجز جديد";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.ReservationForm);
        await LoadLookupsAsync();

        if (HotelReservationNavigationBridge.PendingEditReservationId is int editId)
        {
            HotelReservationNavigationBridge.PendingEditReservationId = null;
            await LoadReservationAsync(editId);
        }
        else
        {
            ResetForNew();
        }
    }

    partial void OnCheckInDateChanged(DateTime value) => _ = RecalculateAmountAsync();
    partial void OnCheckOutDateChanged(DateTime value) => _ = RecalculateAmountAsync();
    partial void OnSelectedRoomTypeIdChanged(int? value) => _ = OnRoomTypeChangedAsync();
    partial void OnSelectedRoomIdChanged(int? value) => _ = RecalculateAmountAsync();

    partial void OnGuestSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            GuestSearchResults.Clear();
            return;
        }

        _ = SearchGuestsAsync();
    }

    private async Task LoadLookupsAsync()
    {
        RoomTypes.Clear();
        var types = await _masterDataService.GetRoomTypesAsync();
        foreach (var rt in types.OrderBy(t => t.SortOrder).ThenBy(t => t.Name))
            RoomTypes.Add(new RoomTypeOption(rt.Id, rt.Name, rt.Capacity, rt.BasePrice));
    }

    private async Task OnRoomTypeChangedAsync()
    {
        await LoadAvailableRoomsAsync();
        await RecalculateAmountAsync();
    }

    private async Task LoadAvailableRoomsAsync()
    {
        AvailableRooms.Clear();
        if (!SelectedRoomTypeId.HasValue)
            return;

        var rooms = await _masterDataService.GetRoomsAsync();
        foreach (var room in rooms
                     .Where(r => r.RoomTypeName == RoomTypes.FirstOrDefault(t => t.Id == SelectedRoomTypeId)?.Name
                                 && r.Status is RoomStatus.Available or RoomStatus.Occupied))
        {
            AvailableRooms.Add(new RoomOption(room.Id, room.RoomNumber, room.FloorName, room.Status));
        }
    }

    private async Task RecalculateAmountAsync()
    {
        if (!SelectedRoomTypeId.HasValue || CheckOutDate <= CheckInDate)
        {
            TotalAmount = 0;
            return;
        }

        decimal total = 0;
        for (var d = CheckInDate.Date; d < CheckOutDate.Date; d = d.AddDays(1))
        {
            var price = await _ratePlanService.GetPriceForDateAsync(SelectedRoomTypeId.Value, d);
            total += price ?? RoomTypes.FirstOrDefault(t => t.Id == SelectedRoomTypeId)?.BasePrice ?? 0;
        }

        TotalAmount = total;
    }

    [RelayCommand]
    private async Task SearchGuestsAsync()
    {
        GuestSearchResults.Clear();
        var results = await _guestService.SearchAsync(GuestSearchText.Trim(), 20);
        foreach (var g in results)
            GuestSearchResults.Add(g);
    }

    [RelayCommand]
    private void OpenGuestPicker() => IsGuestPickerOpen = true;

    [RelayCommand]
    private void CloseGuestPicker() => IsGuestPickerOpen = false;

    [RelayCommand]
    private void SelectGuest(GuestListItem? guest)
    {
        if (guest is null)
            return;

        SelectedGuestId = guest.Id;
        SelectedGuestDisplay = $"{guest.FullName} — {guest.Phone}";
        IsGuestPickerOpen = false;
    }

    [RelayCommand]
    private void OpenNewGuestDialog()
    {
        NewGuestName = string.Empty;
        NewGuestPhone = string.Empty;
        NewGuestIdNumber = string.Empty;
        IsNewGuestDialogOpen = true;
    }

    [RelayCommand]
    private void CloseNewGuestDialog() => IsNewGuestDialogOpen = false;

    [RelayCommand]
    private async Task SaveNewGuestAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGuestName))
        {
            _toast.ShowWarning("أدخل اسم النزيل");
            return;
        }

        try
        {
            var guest = await _guestService.CreateAsync(new Guest
            {
                FullName = NewGuestName.Trim(),
                Phone = NewGuestPhone.Trim(),
                IdNumber = NewGuestIdNumber.Trim()
            });
            SelectedGuestId = guest.Id;
            SelectedGuestDisplay = $"{guest.FullName} — {guest.Phone}";
            IsNewGuestDialogOpen = false;
            IsGuestPickerOpen = false;
            _toast.ShowSuccess("تم إضافة النزيل");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!CanSave)
            return;

        if (!SelectedGuestId.HasValue)
        {
            _toast.ShowWarning("اختر النزيل");
            return;
        }

        if (CheckOutDate <= CheckInDate)
        {
            _toast.ShowWarning("تاريخ المغادرة يجب أن يكون بعد الوصول");
            return;
        }

        IsSaving = true;
        try
        {
            if (_editId.HasValue)
            {
                var existing = await _reservationService.GetByIdAsync(_editId.Value);
                if (existing is null)
                {
                    _toast.ShowError("الحجز غير موجود");
                    return;
                }

                existing.GuestId = SelectedGuestId.Value;
                existing.RoomId = SelectedRoomId;
                existing.CheckInDate = CheckInDate.Date;
                existing.CheckOutDate = CheckOutDate.Date;
                existing.GuestCount = GuestCount;
                existing.Notes = Notes;
                await _reservationService.UpdateAsync(existing);
                _toast.ShowSuccess("تم تحديث الحجز");
            }
            else
            {
                var reservation = new Reservation
                {
                    GuestId = SelectedGuestId.Value,
                    RoomId = SelectedRoomId,
                    CheckInDate = CheckInDate.Date,
                    CheckOutDate = CheckOutDate.Date,
                    GuestCount = GuestCount,
                    Status = ReservationStatus.Confirmed,
                    AmountPaid = AmountPaid,
                    Notes = Notes
                };
                await _reservationService.CreateAsync(reservation);
                _toast.ShowSuccess("تم إنشاء الحجز");
            }
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (!CanPrint || !_editId.HasValue)
            return;

        var reservation = await _reservationService.GetByIdAsync(_editId.Value);
        if (reservation is not null)
            _printService.PrintReservationInvoice(reservation);
    }

    private async Task LoadReservationAsync(int id)
    {
        var reservation = await _reservationService.GetByIdAsync(id);
        if (reservation is null)
        {
            _toast.ShowError("الحجز غير موجود");
            return;
        }

        _editId = reservation.Id;
        PageTitle = "تعديل حجز";
        ReservationNumber = reservation.ReservationNumber;
        CheckInDate = reservation.CheckInDate;
        CheckOutDate = reservation.CheckOutDate;
        GuestCount = reservation.GuestCount;
        SelectedGuestId = reservation.GuestId;
        SelectedGuestDisplay = reservation.Guest?.FullName ?? string.Empty;
        SelectedRoomId = reservation.RoomId;
        SelectedRoomTypeId = reservation.Room?.RoomTypeId;
        TotalAmount = reservation.TotalAmount;
        AmountPaid = reservation.AmountPaid;
        Notes = reservation.Notes;

        await LoadAvailableRoomsAsync();
    }

    private void ResetForNew()
    {
        _editId = null;
        PageTitle = "حجز جديد";
        ReservationNumber = "يُنشأ تلقائياً";
        CheckInDate = DateTime.Today;
        CheckOutDate = DateTime.Today.AddDays(1);
        GuestCount = 1;
        SelectedGuestId = null;
        SelectedGuestDisplay = string.Empty;
        SelectedRoomTypeId = null;
        SelectedRoomId = null;
        TotalAmount = 0;
        AmountPaid = 0;
        Notes = string.Empty;
    }
}

public sealed record RoomTypeOption(int Id, string Name, int Capacity, decimal BasePrice);
public sealed record RoomOption(int Id, string Number, string Floor, RoomStatus Status);
