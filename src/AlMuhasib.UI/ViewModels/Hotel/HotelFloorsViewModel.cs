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

public partial class HotelFloorsViewModel : PagedViewModelBase
{
    private readonly IHotelMasterDataService _masterDataService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;
    private readonly IUserPreferencesService _userPreferences;
    private readonly IExportService _exportService;
    private System.Timers.Timer? _debounceTimer;

    public ObservableCollection<Floor> Floors { get; } = [];
    public ObservableCollection<HotelListStatItem> Stats { get; } = [];

    [ObservableProperty] private Floor? _selectedFloor;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private bool _isCardView;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private int _editSortOrder;

    private int? _editingId;
    private List<Floor> _allFloors = [];

    public HotelFloorsViewModel(
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
        PageTitle = "الطوابق";
        IsCardView = ListViewModeHelper.LoadIsCardView(_userPreferences, ListViewModeKeys.HotelFloors);
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.Floors);
        await LoadAllAsync();
        await ReloadPageAsync();
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
                await ReloadPageAsync();
            });
        };
        _debounceTimer.Start();
    }

    partial void OnIsCardViewChanged(bool value) =>
        ListViewModeHelper.SaveIsCardView(_userPreferences, ListViewModeKeys.HotelFloors, value);

    protected override Task OnPageChangedAsync() => ReloadPageAsync();

    protected override void OnColumnFiltersChanged() => _ = ReloadFromFirstPageAsync();

    private async Task ReloadFromFirstPageAsync()
    {
        CurrentPage = 1;
        await ReloadPageAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await LoadAllAsync();
        await ReloadPageAsync();
    }

    private async Task LoadAllAsync()
    {
        IsBusy = true;
        try
        {
            _allFloors = (await _masterDataService.GetFloorsAsync())
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Name)
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
        var items = _allFloors
            .Where(f => search is null || f.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            items = ColumnFilterEngine.Apply(items, ColumnFilters).ToList();

        MasterDataColumnFilterHelper.ApplyClientPagination(
            items, Floors, CurrentPage, PageSize,
            out var totalCount, out _, out _);

        ApplyPaginationStats(totalCount);
        RebuildStats(items);
        return Task.CompletedTask;
    }

    private void RebuildStats(IReadOnlyCollection<Floor> allItems)
    {
        Stats.Clear();
        var total = allItems.Count;
        var withRooms = allItems.Count(f => f.Rooms?.Count > 0);

        Stats.Add(new HotelListStatItem { Label = "إجمالي الطوابق", Value = total.ToString("N0"), AccentColor = "#1565C0" });
        Stats.Add(new HotelListStatItem { Label = "طوابق بها غرف", Value = withRooms.ToString("N0"), AccentColor = "#2E7D32" });
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        if (!CanAdd) return;
        _editingId = null;
        IsEditMode = false;
        EditName = string.Empty;
        EditSortOrder = Floors.Count + 1;
        IsDialogOpen = true;
    }

    [RelayCommand]
    private void OpenEditDialog(Floor? item)
    {
        item ??= SelectedFloor;
        if (item is null || !CanEdit) return;
        _editingId = item.Id;
        IsEditMode = true;
        EditName = item.Name;
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
            _toast.ShowWarning("أدخل اسم الطابق");
            return;
        }

        try
        {
            if (_editingId.HasValue)
            {
                var entity = await _masterDataService.GetFloorByIdAsync(_editingId.Value)
                    ?? throw new InvalidOperationException("الطابق غير موجود");
                entity.Name = EditName.Trim();
                entity.SortOrder = EditSortOrder;
                await _masterDataService.UpdateFloorAsync(entity);
                _toast.ShowSuccess("تم التحديث");
            }
            else
            {
                await _masterDataService.CreateFloorAsync(new Floor
                {
                    Name = EditName.Trim(),
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
    private async Task DeleteAsync(Floor? item)
    {
        item ??= SelectedFloor;
        if (item is null || !CanDelete) return;
        if (!RequestSensitiveApproval($"حذف الطابق «{item.Name}»؟")) return;

        try
        {
            await _masterDataService.DeleteFloorAsync(item.Id, _currentUserService.Username ?? "System");
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
        if (!CanExport) return;
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"HotelFloors_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };
        if (dialog.ShowDialog() != true) return;
        var headers = new[] { "الاسم", "الترتيب" };
        var data = Floors.Select(f => new object?[] { f.Name, f.SortOrder }).ToList();
        _exportService.ExportToExcel(dialog.FileName, "الطوابق", headers, data);
        _toast.ShowSuccess("تم التصدير");
    }

    [RelayCommand]
    private void PrintTable()
    {
        if (!CanPrint) return;
        var headers = new[] { "الاسم", "الترتيب" };
        var data = Floors.Select(f => new object?[] { f.Name, f.SortOrder }).ToList();
        _exportService.PrintTable("قائمة الطوابق", headers, data);
    }
}
