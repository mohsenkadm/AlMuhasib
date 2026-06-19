using AlMuhasib.Core.Entities.Hotel.Restaurant;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class RestaurantTablesViewModel : ViewModelBase
{
    private readonly IRestaurantTableService _tableService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;

    public ObservableCollection<RestaurantTable> Tables { get; } = [];
    public ObservableCollection<HotelListStatItem> Stats { get; } = [];

    [ObservableProperty] private RestaurantTable? _selectedTable;
    [ObservableProperty] private bool _isDialogOpen;
    [ObservableProperty] private bool _isGridView = true;
    [ObservableProperty] private string _editTableNumber = string.Empty;
    [ObservableProperty] private int _editCapacity = 4;
    [ObservableProperty] private string _searchText = string.Empty;

    private int? _editingId;
    private List<RestaurantTable> _allTables = [];

    public bool IsTableView => !IsGridView;
    public bool IsEditMode => _editingId.HasValue;

    public RestaurantTablesViewModel(
        IRestaurantTableService tableService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _tableService = tableService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "طاولات الصالة";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.RestaurantTables);
        await LoadTablesAsync();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    protected override void OnColumnFiltersChanged() => ApplyFilters();
    partial void OnIsGridViewChanged(bool value) => OnPropertyChanged(nameof(IsTableView));

    [RelayCommand]
    private async Task LoadTablesAsync()
    {
        _allTables = (await _tableService.GetTablesAsync(activeOnly: false)).ToList();
        UpdateStats(_allTables);
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var items = _allTables.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
            items = items.Where(t => t.TableNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            items = ColumnFilterEngine.Apply(items, ColumnFilters);

        Tables.Clear();
        foreach (var t in items)
            Tables.Add(t);
    }

    private void UpdateStats(IReadOnlyList<RestaurantTable> all)
    {
        Stats.Clear();
        Stats.Add(new HotelListStatItem { Label = "إجمالي الطاولات", Value = all.Count.ToString("N0"), AccentColor = "#1565C0" });
        Stats.Add(new HotelListStatItem { Label = "متاحة", Value = all.Count(t => t.Status == RestaurantTableStatus.Available).ToString("N0"), AccentColor = "#2E7D32" });
        Stats.Add(new HotelListStatItem { Label = "مشغولة", Value = all.Count(t => t.Status == RestaurantTableStatus.Occupied).ToString("N0"), AccentColor = "#F57C00" });
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _editingId = null;
        EditTableNumber = (Tables.Count + 1).ToString();
        EditCapacity = 4;
        IsDialogOpen = true;
        OnPropertyChanged(nameof(IsEditMode));
    }

    [RelayCommand]
    private void OpenEditDialog(RestaurantTable? table)
    {
        if (table is null) return;
        _editingId = table.Id;
        EditTableNumber = table.TableNumber;
        EditCapacity = table.Capacity;
        IsDialogOpen = true;
        OnPropertyChanged(nameof(IsEditMode));
    }

    [RelayCommand]
    private void CloseDialog()
    {
        IsDialogOpen = false;
        _editingId = null;
        OnPropertyChanged(nameof(IsEditMode));
    }

    [RelayCommand]
    private async Task SaveTableAsync()
    {
        try
        {
            await _tableService.SaveTableAsync(new RestaurantTable
            {
                Id = _editingId ?? 0,
                TableNumber = EditTableNumber,
                Capacity = EditCapacity,
                SortOrder = Tables.Count + 1,
                IsActive = true
            });
            CloseDialog();
            await LoadTablesAsync();
            _toast.ShowSuccess("تم الحفظ");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteTableAsync(RestaurantTable? table)
    {
        if (table is null || !CanDelete) return;
        try
        {
            await _tableService.DeleteTableAsync(table.Id, _currentUserService.Username ?? "system");
            await LoadTablesAsync();
            _toast.ShowSuccess("تم حذف الطاولة");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task SetAvailableAsync(RestaurantTable? table)
    {
        if (table is null) return;
        await _tableService.SetTableStatusAsync(table.Id, RestaurantTableStatus.Available);
        await LoadTablesAsync();
        _toast.ShowSuccess("تم تحرير الطاولة");
    }
}
