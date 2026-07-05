using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelRoomsViewModel : HotelListPreviewViewModelBase
{
    private readonly IHotelMasterDataService _masterDataService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly IUserPreferencesService _userPreferences;
    private readonly HotelEntityNavigationHelper _navigation;
    private System.Timers.Timer? _debounceTimer;

    public ObservableCollection<HotelRoomListDisplay> Rooms { get; } = [];
    public ObservableCollection<HotelListStatItem> Stats { get; } = [];
    public ObservableCollection<FloorOption> Floors { get; } = [];
    public ObservableCollection<RoomTypeOption> RoomTypes { get; } = [];

    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private bool _isGridView;
    [ObservableProperty] private HotelRoomListDisplay? _selectedRoom;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _editRoomNumber = string.Empty;
    [ObservableProperty] private int? _editFloorId;
    [ObservableProperty] private int? _editRoomTypeId;
    [ObservableProperty] private RoomStatus _editStatus = RoomStatus.Available;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private string _statusFilter = "الكل";
    [ObservableProperty] private HotelRoomDetailDisplay? _detailRoom;

    private int? _editingRoomId;
    private List<HotelRoomListDisplay> _allRooms = [];

    public IReadOnlyList<string> StatusFilterOptions { get; } =
        ["الكل", "متاحة", "مشغولة", "تحتاج تنظيف", "صيانة", "خارج الخدمة"];

    public IReadOnlyList<RoomStatusOption> StatusOptions { get; } =
        Enum.GetValues<RoomStatus>()
            .Select(s => new RoomStatusOption(s, HotelDisplayHelper.GetRoomStatusLabel(s)))
            .ToList();

    public HotelRoomsViewModel(
        IHotelMasterDataService masterDataService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        IUserPreferencesService userPreferences,
        MainWindowViewModel mainWindow)
    {
        _masterDataService = masterDataService;
        _currentUserService = currentUserService;
        _toast = toast;
        _userPreferences = userPreferences;
        _navigation = new HotelEntityNavigationHelper(mainWindow);
        PageTitle = "الغرف";
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.HotelRooms);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.Rooms);
        await LoadLookupsAsync();
        await LoadAllRoomsAsync();
        await ReloadPageAsync();
        await ApplyPendingSelectionAsync();
    }

    private async Task ApplyPendingSelectionAsync()
    {
        if (HotelNavigationBridge.PendingRoomId is not int pendingId)
            return;

        HotelNavigationBridge.PendingRoomId = null;
        var item = Rooms.FirstOrDefault(r => r.Id == pendingId)
                   ?? _allRooms.FirstOrDefault(r => r.Id == pendingId);
        if (item is null)
            return;

        SelectedRoom = item;
        await LoadPreviewAsync(item);
    }

    protected override void OnPreviewClosed()
    {
        SelectedRoom = null;
        DetailRoom = null;
    }

    partial void OnSelectedRoomChanged(HotelRoomListDisplay? value)
    {
        if (value is null)
        {
            ClosePreview();
            return;
        }

        _ = LoadPreviewAsync(value);
    }

    private async Task LoadPreviewAsync(HotelRoomListDisplay item)
    {
        var room = await _masterDataService.GetRoomByIdAsync(item.Id);
        if (room is null)
        {
            _toast.ShowError("الغرفة غير موجودة");
            return;
        }

        var listItem = _allRooms.FirstOrDefault(r => r.Id == item.Id);
        DetailRoom = HotelRoomDetailDisplay.FromRoom(room, new RoomListItem
        {
            Id = item.Id,
            CurrentGuestName = listItem?.CurrentGuestName,
            CurrentGuestId = listItem?.CurrentGuestId,
            CurrentReservationId = listItem?.CurrentReservationId
        });
        SetPreviewHeader(room.RoomNumber, room.RoomType?.Name ?? "—", PackIconKind.Door);
    }

    partial void OnStatusFilterChanged(string value) => _ = ReloadFromFirstPageAsync();

    partial void OnSearchTextChanged(string value)
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(350) { AutoReset = false };
        _debounceTimer.Elapsed += async (_, _) =>
        {
            await App.Current.Dispatcher.InvokeAsync(async () =>
            {
                CurrentPage = 1;
                await ReloadPageAsync();
            });
        };
        _debounceTimer.Start();
    }

    public bool IsTableView => !IsCardView && !IsGridView;

    partial void OnIsCardViewChanged(bool value)
    {
        if (value)
            IsGridView = false;
        OnPropertyChanged(nameof(IsTableView));
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.HotelRooms, value);
    }

    partial void OnIsGridViewChanged(bool value)
    {
        if (value)
            IsCardView = false;
        OnPropertyChanged(nameof(IsTableView));
    }

    protected override Task OnPageChangedAsync() => ReloadPageAsync();

    protected override void OnColumnFiltersChanged() => _ = ReloadFromFirstPageAsync();

    private async Task ReloadFromFirstPageAsync()
    {
        CurrentPage = 1;
        await ReloadPageAsync();
    }

    [RelayCommand]
    private async Task LoadRoomsAsync()
    {
        await LoadAllRoomsAsync();
        await ReloadPageAsync();
    }

    private async Task LoadAllRoomsAsync()
    {
        IsBusy = true;
        try
        {
            _allRooms = (await _masterDataService.GetRoomsAsync())
                .OrderBy(r => r.RoomNumber)
                .Select(room => new HotelRoomListDisplay
                {
                    Id = room.Id,
                    RoomNumber = room.RoomNumber,
                    FloorName = room.FloorName,
                    RoomTypeName = room.RoomTypeName,
                    Status = room.Status,
                    PendingStatus = room.Status,
                    StatusLabel = HotelDisplayHelper.GetRoomStatusLabel(room.Status),
                    StatusColor = HotelDisplayHelper.GetRoomStatusColor(room.Status),
                    CurrentGuestName = room.CurrentGuestName ?? "—",
                    CurrentGuestId = room.CurrentGuestId,
                    CurrentReservationId = room.CurrentReservationId
                })
                .ToList();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task ReloadPageAsync()
    {
        var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

        var items = _allRooms
            .Where(r =>
            {
                if (search is not null
                    && !r.RoomNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                    && !r.FloorName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    && !r.RoomTypeName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    && !r.CurrentGuestName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.IsNullOrWhiteSpace(StatusFilter)
                    && StatusFilter != "الكل"
                    && !r.StatusLabel.Contains(StatusFilter, StringComparison.OrdinalIgnoreCase))
                    return false;

                return true;
            })
            .ToList();

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            items = ColumnFilterEngine.Apply(items, ColumnFilters).ToList();

        MasterDataColumnFilterHelper.ApplyClientPagination(
            items, Rooms, CurrentPage, PageSize,
            out var totalCount, out _, out _);

        ApplyPaginationStats(totalCount);
        RebuildStats(items);
        return Task.CompletedTask;
    }

    private void RebuildStats(IReadOnlyCollection<HotelRoomListDisplay> allItems)
    {
        Stats.Clear();
        Stats.Add(new HotelListStatItem { Label = "إجمالي الغرف", Value = allItems.Count.ToString("N0"), AccentColor = "#1565C0" });
        Stats.Add(new HotelListStatItem { Label = "متاحة", Value = allItems.Count(r => r.Status == RoomStatus.Available).ToString("N0"), AccentColor = "#2E7D32" });
        Stats.Add(new HotelListStatItem { Label = "مشغولة", Value = allItems.Count(r => r.Status == RoomStatus.Occupied).ToString("N0"), AccentColor = "#1565C0" });
        Stats.Add(new HotelListStatItem { Label = "تحتاج تنظيف", Value = allItems.Count(r => r.Status == RoomStatus.Dirty).ToString("N0"), AccentColor = "#F57C00" });
        Stats.Add(new HotelListStatItem { Label = "صيانة", Value = allItems.Count(r => r.Status is RoomStatus.Maintenance or RoomStatus.OutOfOrder).ToString("N0"), AccentColor = "#6A1B9A" });
    }

    [RelayCommand]
    private void ToggleGridView() => IsGridView = !IsGridView;

    private async Task LoadLookupsAsync()
    {
        Floors.Clear();
        RoomTypes.Clear();
        foreach (var f in await _masterDataService.GetFloorsAsync())
            Floors.Add(new FloorOption(f.Id, f.Name));
        foreach (var rt in await _masterDataService.GetRoomTypesAsync())
            RoomTypes.Add(new RoomTypeOption(rt.Id, rt.Name, rt.Capacity, rt.BasePrice));
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        if (!CanAdd)
            return;

        _editingRoomId = null;
        IsEditMode = false;
        EditRoomNumber = string.Empty;
        EditFloorId = Floors.FirstOrDefault()?.Id;
        EditRoomTypeId = RoomTypes.FirstOrDefault()?.Id;
        EditStatus = RoomStatus.Available;
        EditNotes = string.Empty;
        DialogTitle = "إضافة غرفة";
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task OpenEditDialogAsync(HotelRoomListDisplay? item)
    {
        item ??= SelectedRoom;
        if (item is null || !CanEdit)
            return;

        var room = await _masterDataService.GetRoomByIdAsync(item.Id);
        if (room is null)
            return;

        _editingRoomId = room.Id;
        IsEditMode = true;
        EditRoomNumber = room.RoomNumber;
        EditFloorId = room.FloorId;
        EditRoomTypeId = room.RoomTypeId;
        EditStatus = room.Status;
        EditNotes = room.Notes;
        DialogTitle = "تعديل غرفة";
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditRoomNumber) || !EditFloorId.HasValue || !EditRoomTypeId.HasValue)
        {
            _toast.ShowWarning("أكمل بيانات الغرفة");
            return;
        }

        try
        {
            if (_editingRoomId.HasValue)
            {
                var room = await _masterDataService.GetRoomByIdAsync(_editingRoomId.Value)
                    ?? throw new InvalidOperationException("الغرفة غير موجودة");
                room.RoomNumber = EditRoomNumber.Trim();
                room.FloorId = EditFloorId.Value;
                room.RoomTypeId = EditRoomTypeId.Value;
                room.Status = EditStatus;
                room.Notes = EditNotes;
                await _masterDataService.UpdateRoomAsync(room);
                _toast.ShowSuccess("تم تحديث الغرفة");
            }
            else
            {
                await _masterDataService.CreateRoomAsync(new Room
                {
                    RoomNumber = EditRoomNumber.Trim(),
                    FloorId = EditFloorId.Value,
                    RoomTypeId = EditRoomTypeId.Value,
                    Status = EditStatus,
                    Notes = EditNotes
                });
                _toast.ShowSuccess("تم إضافة الغرفة");
            }

            IsDialogOpen = false;
            await LoadRoomsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(HotelRoomListDisplay? item)
    {
        item ??= SelectedRoom;
        if (item is null || !CanDelete)
            return;

        if (!RequestSensitiveApproval($"حذف الغرفة {item.RoomNumber}؟"))
            return;

        try
        {
            await _masterDataService.DeleteRoomAsync(item.Id, _currentUserService.Username ?? "System");
            _toast.ShowSuccess("تم الحذف");
            await LoadRoomsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ChangeRoomStatusAsync(HotelRoomListDisplay? row)
    {
        row ??= SelectedRoom;
        if (row is null || !CanEdit)
            return;

        if (row.PendingStatus == row.Status)
            return;

        try
        {
            var room = await _masterDataService.GetRoomByIdAsync(row.Id)
                ?? throw new InvalidOperationException("الغرفة غير موجودة");

            room.Status = row.PendingStatus;
            await _masterDataService.UpdateRoomAsync(room);
            _toast.ShowSuccess("تم تحديث حالة الغرفة");
            await LoadRoomsAsync();
        }
        catch (Exception ex)
        {
            row.PendingStatus = row.Status;
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task SelectRoomAsync(HotelRoomListDisplay? room)
    {
        if (room is null)
            return;

        SelectedRoom = room;
        await LoadPreviewAsync(room);
    }

    [RelayCommand]
    private async Task OpenGuestFromRoomAsync()
    {
        if (DetailRoom?.CurrentGuestId is int guestId)
            await _navigation.OpenGuestsAsync(guestId);
        else
            _toast.ShowWarning("لا يوجد نزيل في الغرفة");
    }

    [RelayCommand]
    private async Task OpenGuestFromRoomLinkAsync(HotelRoomListDisplay? room)
    {
        room ??= SelectedRoom;
        if (room?.CurrentGuestId is int guestId)
            await _navigation.OpenGuestsAsync(guestId);
        else
            _toast.ShowWarning("لا يوجد نزيل في الغرفة");
    }

    [RelayCommand]
    private async Task OpenReservationFromRoomAsync()
    {
        if (DetailRoom?.CurrentReservationId is int reservationId)
            await _navigation.OpenReservationsAsync(reservationId);
        else
            _toast.ShowWarning("لا يوجد حجز نشط");
    }
}

public sealed record FloorOption(int Id, string Name);
public sealed record RoomStatusOption(RoomStatus Value, string Label);
