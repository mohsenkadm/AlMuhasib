using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.Core.Models.Hotel;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelGuestsViewModel : PagedViewModelBase
{
    private readonly IGuestService _guestService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly IUserPreferencesService _userPreferences;
    private System.Timers.Timer? _debounceTimer;

    public ObservableCollection<GuestListItem> Guests { get; } = [];
    public ObservableCollection<HotelListStatItem> Stats { get; } = [];

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private GuestListItem? _selectedGuest;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _editFullName = string.Empty;
    [ObservableProperty] private string _editPhone = string.Empty;
    [ObservableProperty] private string _editIdNumber = string.Empty;
    [ObservableProperty] private string _editEmail = string.Empty;
    [ObservableProperty] private string _editNotes = string.Empty;
    [ObservableProperty] private bool _isDeleteDialogOpen;
    [ObservableProperty] private GuestListItem? _guestToDelete;
    [ObservableProperty] private string _dialogTitle = string.Empty;

    private int? _editingId;

    public HotelGuestsViewModel(
        IGuestService guestService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        IUserPreferencesService userPreferences)
    {
        _guestService = guestService;
        _currentUserService = currentUserService;
        _toast = toast;
        _userPreferences = userPreferences;
        PageTitle = "النزلاء";
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.HotelGuests);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.Guests);
        await LoadGuestsAsync();
    }

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
                await LoadGuestsAsync();
            });
        };
        _debounceTimer.Start();
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.HotelGuests, value);

    protected override void OnColumnFiltersChanged() => _ = ReloadFromFirstPageAsync();

    private async Task ReloadFromFirstPageAsync()
    {
        CurrentPage = 1;
        await LoadGuestsAsync();
    }

    protected override Task OnPageChangedAsync() => LoadGuestsAsync();

    [RelayCommand]
    private async Task LoadGuestsAsync()
    {
        IsBusy = true;
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            {
                var (allItems, _) = await _guestService.GetPagedAsync(1, int.MaxValue, search);
                var filtered = ColumnFilterEngine.Apply(allItems, ColumnFilters).ToList();
                MasterDataColumnFilterHelper.ApplyClientPagination(
                    filtered, Guests, CurrentPage, PageSize,
                    out var filteredTotal, out _, out _);
                ApplyPaginationStats(filteredTotal);
                RebuildStats(filtered);
                return;
            }

            var (items, total) = await _guestService.GetPagedAsync(CurrentPage, PageSize, search);
            Guests.Clear();
            foreach (var item in items)
                Guests.Add(item);
            ApplyPaginationStats(total);
            RebuildStats(Guests);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RebuildStats(IEnumerable<GuestListItem> items)
    {
        var list = items.ToList();
        var total = list.Count;
        var withReservations = list.Count(x => x.ReservationCount > 0);
        var totalReservations = list.Sum(x => x.ReservationCount);

        Stats.Clear();
        Stats.Add(new HotelListStatItem { Label = "إجمالي النزلاء", Value = total.ToString("N0"), AccentColor = "#1565C0" });
        Stats.Add(new HotelListStatItem { Label = "نزلاء لديهم حجوزات", Value = withReservations.ToString("N0"), AccentColor = "#2E7D32" });
        Stats.Add(new HotelListStatItem { Label = "إجمالي الحجوزات", Value = totalReservations.ToString("N0"), AccentColor = "#6A1B9A" });
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        if (!CanAdd) return;
        _editingId = null;
        IsEditMode = false;
        DialogTitle = "إضافة نزيل";
        EditFullName = EditPhone = EditIdNumber = EditEmail = EditNotes = string.Empty;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private async Task OpenEditDialogAsync(GuestListItem? item)
    {
        item ??= SelectedGuest;
        if (item is null || !CanEdit) return;

        var guest = await _guestService.GetByIdAsync(item.Id);
        if (guest is null) return;

        _editingId = guest.Id;
        IsEditMode = true;
        DialogTitle = "تعديل نزيل";
        EditFullName = guest.FullName;
        EditPhone = guest.Phone;
        EditIdNumber = guest.IdNumber;
        EditEmail = guest.Email;
        EditNotes = guest.Notes;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditFullName))
        {
            _toast.ShowWarning("أدخل اسم النزيل");
            return;
        }

        try
        {
            if (_editingId.HasValue)
            {
                var guest = await _guestService.GetByIdAsync(_editingId.Value)
                    ?? throw new InvalidOperationException("النزيل غير موجود");
                guest.FullName = EditFullName.Trim();
                guest.Phone = EditPhone.Trim();
                guest.IdNumber = EditIdNumber.Trim();
                guest.Email = EditEmail.Trim();
                guest.Notes = EditNotes.Trim();
                await _guestService.UpdateAsync(guest);
                _toast.ShowSuccess("تم التحديث");
            }
            else
            {
                await _guestService.CreateAsync(new Guest
                {
                    FullName = EditFullName.Trim(),
                    Phone = EditPhone.Trim(),
                    IdNumber = EditIdNumber.Trim(),
                    Email = EditEmail.Trim(),
                    Notes = EditNotes.Trim()
                });
                _toast.ShowSuccess("تم الإضافة");
            }

            IsDialogOpen = false;
            await LoadGuestsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void ConfirmDelete(GuestListItem? item)
    {
        if (item is null || !CanDelete) return;
        GuestToDelete = item;
        IsDeleteDialogOpen = true;
    }

    [RelayCommand]
    private void CancelDelete() => IsDeleteDialogOpen = false;

    [RelayCommand]
    private async Task DeleteConfirmedAsync()
    {
        if (GuestToDelete is null) return;
        try
        {
            await _guestService.DeleteAsync(GuestToDelete.Id, _currentUserService.Username ?? "System");
            IsDeleteDialogOpen = false;
            GuestToDelete = null;
            _toast.ShowSuccess("تم الحذف");
            await LoadGuestsAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }
}
