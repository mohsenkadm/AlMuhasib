using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Hotel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class HotelRoomTypesViewModel : PagedViewModelBase
{
    private readonly IHotelMasterDataService _masterDataService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly IUserPreferencesService _userPreferences;
    private readonly IExportService _exportService;
    private System.Timers.Timer? _debounceTimer;

    public ObservableCollection<RoomType> RoomTypes { get; } = [];
    public ObservableCollection<HotelListStatItem> Stats { get; } = [];

    [ObservableProperty] private RoomType? _selectedRoomType;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _dialogTitle = string.Empty;
    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editDescription = string.Empty;
    [ObservableProperty] private int _editCapacity = 2;
    [ObservableProperty] private decimal _editBasePrice;
    [ObservableProperty] private int _editSortOrder;

    private int? _editingId;

    public HotelRoomTypesViewModel(
        IHotelMasterDataService masterDataService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast,
        IUserPreferencesService userPreferences,
        IExportService exportService)
    {
        _masterDataService = masterDataService;
        _currentUserService = currentUserService;
        _toast = toast;
        _userPreferences = userPreferences;
        _exportService = exportService;
        PageTitle = "أنواع الغرف";
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.HotelRoomTypes);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.RoomTypes);
        await LoadAsync();
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
                await LoadAsync();
            });
        };
        _debounceTimer.Start();
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.HotelRoomTypes, value);

    protected override Task OnPageChangedAsync() => LoadAsync();

    protected override void OnColumnFiltersChanged() => _ = ReloadFromFirstPageAsync();

    private async Task ReloadFromFirstPageAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
            var items = (await _masterDataService.GetRoomTypesAsync())
                .Where(x => search is null
                            || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || x.Description.Contains(search, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToList();

            if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
                items = ColumnFilterEngine.Apply(items, ColumnFilters).ToList();

            MasterDataColumnFilterHelper.ApplyClientPagination(
                items, RoomTypes, CurrentPage, PageSize,
                out var totalCount, out _, out _);

            ApplyPaginationStats(totalCount);
            RebuildStats(items);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RebuildStats(IReadOnlyCollection<RoomType> allItems)
    {
        Stats.Clear();
        var total = allItems.Count;
        var avgPrice = total == 0 ? 0m : allItems.Average(x => x.BasePrice);
        var avgCapacity = total == 0 ? 0 : (int)Math.Round(allItems.Average(x => x.Capacity));

        Stats.Add(new HotelListStatItem { Label = "إجمالي الأنواع", Value = total.ToString("N0"), AccentColor = "#1565C0" });
        Stats.Add(new HotelListStatItem { Label = "متوسط السعر", Value = avgPrice.ToString("N0"), AccentColor = "#2E7D32" });
        Stats.Add(new HotelListStatItem { Label = "متوسط السعة", Value = avgCapacity.ToString("N0"), AccentColor = "#6A1B9A" });
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        if (!CanAdd) return;
        DialogTitle = "إضافة نوع غرفة";
        _editingId = null;
        IsEditMode = false;
        EditName = string.Empty;
        EditDescription = string.Empty;
        EditCapacity = 2;
        EditBasePrice = 0;
        EditSortOrder = RoomTypes.Count + 1;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(RoomType? item)
    {
        item ??= SelectedRoomType;
        if (item is null || !CanEdit) return;
        DialogTitle = "تعديل نوع غرفة";
        _editingId = item.Id;
        IsEditMode = true;
        EditName = item.Name;
        EditDescription = item.Description;
        EditCapacity = item.Capacity;
        EditBasePrice = item.BasePrice;
        EditSortOrder = item.SortOrder;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDialog() => IsDialogOpen = false;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName))
        {
            _toast.ShowWarning("أدخل اسم النوع");
            return;
        }

        try
        {
            if (_editingId.HasValue)
            {
                var entity = await _masterDataService.GetRoomTypeByIdAsync(_editingId.Value)
                    ?? throw new InvalidOperationException("النوع غير موجود");
                entity.Name = EditName.Trim();
                entity.Description = EditDescription.Trim();
                entity.Capacity = EditCapacity;
                entity.BasePrice = EditBasePrice;
                entity.SortOrder = EditSortOrder;
                await _masterDataService.UpdateRoomTypeAsync(entity);
                _toast.ShowSuccess("تم التحديث");
            }
            else
            {
                await _masterDataService.CreateRoomTypeAsync(new RoomType
                {
                    Name = EditName.Trim(),
                    Description = EditDescription.Trim(),
                    Capacity = EditCapacity,
                    BasePrice = EditBasePrice,
                    SortOrder = EditSortOrder
                });
                _toast.ShowSuccess("تم الإضافة");
            }

            IsDialogOpen = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(RoomType? item)
    {
        item ??= SelectedRoomType;
        if (item is null || !CanDelete) return;
        if (!RequestSensitiveApproval($"حذف نوع الغرفة «{item.Name}»؟")) return;

        try
        {
            await _masterDataService.DeleteRoomTypeAsync(item.Id, _currentUserService.Username ?? "System");
            _toast.ShowSuccess("تم الحذف");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void ExportToExcel()
    {
        var headers = new[] { "الاسم", "السعة", "السعر الأساسي", "الترتيب" };
        var data = RoomTypes.Select(r => new object?[] { r.Name, r.Capacity, r.BasePrice, r.SortOrder }).ToList();
        ListTableExportHelper.ExportExcel(_exportService, _toast, CanExport, "RoomTypes", "أنواع الغرف", headers, data);
    }

    [RelayCommand]
    private void PrintTable()
    {
        var headers = new[] { "الاسم", "السعة", "السعر الأساسي", "الترتيب" };
        var data = RoomTypes.Select(r => new object?[] { r.Name, r.Capacity, r.BasePrice, r.SortOrder }).ToList();
        ListTableExportHelper.Print(_exportService, CanPrint, "قائمة أنواع الغرف", headers, data);
    }
}
