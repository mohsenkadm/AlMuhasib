using AlMuhasib.Core.Entities.Hotel.Restaurant;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Hotel;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AlMuhasib.UI.ViewModels.Hotel;

public partial class RestaurantMenuViewModel : ViewModelBase
{
    private readonly IRestaurantMenuService _menuService;
    private readonly IRestaurantInventoryService _inventoryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;

    public ObservableCollection<RestaurantMenuCategory> Categories { get; } = [];
    public ObservableCollection<RestaurantMenuItem> MenuItems { get; } = [];
    public ObservableCollection<RestaurantIngredient> Ingredients { get; } = [];
    public ObservableCollection<HotelListStatItem> Stats { get; } = [];

    [ObservableProperty] private RestaurantMenuCategory? _selectedCategory;
    [ObservableProperty] private RestaurantMenuItem? _selectedMenuItem;
    [ObservableProperty] private bool _isItemDialogOpen;
    [ObservableProperty] private bool _isCategoryDialogOpen;
    [ObservableProperty] private string _editItemName = string.Empty;
    [ObservableProperty] private decimal _editItemPrice;
    [ObservableProperty] private int? _editItemCategoryId;
    [ObservableProperty] private string _editCategoryName = string.Empty;
    [ObservableProperty] private string _editCategoryColor = "#00897B";
    [ObservableProperty] private string _searchText = string.Empty;

    private int? _editingItemId;
    private int? _editingCategoryId;
    private List<RestaurantMenuItem> _allMenuItems = [];

    public bool IsCategoryEditMode => _editingCategoryId.HasValue;
    public bool IsItemEditMode => _editingItemId.HasValue;

    public RestaurantMenuViewModel(
        IRestaurantMenuService menuService,
        IRestaurantInventoryService inventoryService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _menuService = menuService;
        _inventoryService = inventoryService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "قائمة المطعم";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.RestaurantMenu);
        await _menuService.EnsureSeedDataAsync();
        await LoadAllAsync();
    }

    partial void OnSelectedCategoryChanged(RestaurantMenuCategory? value) => _ = LoadMenuItemsAsync();
    partial void OnSearchTextChanged(string value) => ApplyMenuItemFilters();

    protected override void OnColumnFiltersChanged() => ApplyMenuItemFilters();

    [RelayCommand]
    private async Task LoadAllAsync()
    {
        Categories.Clear();
        foreach (var c in await _menuService.GetCategoriesAsync(activeOnly: false))
            Categories.Add(c);

        Ingredients.Clear();
        foreach (var i in await _inventoryService.GetIngredientsAsync())
            Ingredients.Add(i);

        await LoadMenuItemsAsync();
        UpdateStats();
    }

    [RelayCommand]
    private async Task LoadMenuItemsAsync()
    {
        _allMenuItems = (await _menuService.GetMenuItemsAsync(SelectedCategory?.Id, activeOnly: false)).ToList();
        ApplyMenuItemFilters();
    }

    private void ApplyMenuItemFilters()
    {
        var items = _allMenuItems.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            items = items.Where(m =>
                m.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || (m.Barcode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            items = ColumnFilterEngine.Apply(items, ColumnFilters);

        MenuItems.Clear();
        foreach (var m in items)
            MenuItems.Add(m);

        UpdateStats();
    }

    private void UpdateStats()
    {
        Stats.Clear();
        Stats.Add(new HotelListStatItem { Label = "الفئات", Value = Categories.Count.ToString("N0"), AccentColor = "#1565C0" });
        Stats.Add(new HotelListStatItem { Label = "الأصناف", Value = _allMenuItems.Count.ToString("N0"), AccentColor = "#00897B" });
        Stats.Add(new HotelListStatItem { Label = "نشط", Value = _allMenuItems.Count(i => i.IsActive).ToString("N0"), AccentColor = "#2E7D32" });
    }

    [RelayCommand]
    private void OpenAddCategoryDialog()
    {
        _editingCategoryId = null;
        EditCategoryName = string.Empty;
        EditCategoryColor = "#00897B";
        IsCategoryDialogOpen = true;
        OnPropertyChanged(nameof(IsCategoryEditMode));
    }

    [RelayCommand]
    private void OpenEditCategoryDialog(RestaurantMenuCategory? category)
    {
        if (category is null) return;
        _editingCategoryId = category.Id;
        EditCategoryName = category.Name;
        EditCategoryColor = category.ColorHex;
        IsCategoryDialogOpen = true;
        OnPropertyChanged(nameof(IsCategoryEditMode));
    }

    [RelayCommand]
    private void CloseCategoryDialog()
    {
        IsCategoryDialogOpen = false;
        _editingCategoryId = null;
        OnPropertyChanged(nameof(IsCategoryEditMode));
    }

    [RelayCommand]
    private async Task SaveCategoryAsync()
    {
        try
        {
            var category = new RestaurantMenuCategory
            {
                Id = _editingCategoryId ?? 0,
                Name = EditCategoryName,
                ColorHex = EditCategoryColor,
                SortOrder = Categories.Count + 1
            };
            await _menuService.SaveCategoryAsync(category);
            CloseCategoryDialog();
            await LoadAllAsync();
            _toast.ShowSuccess("تم حفظ الفئة");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(RestaurantMenuCategory? category)
    {
        if (category is null || !CanDelete) return;
        try
        {
            await _menuService.DeleteCategoryAsync(category.Id, _currentUserService.Username ?? "system");
            await LoadAllAsync();
            _toast.ShowSuccess("تم حذف الفئة");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenAddItemDialog()
    {
        _editingItemId = null;
        EditItemName = string.Empty;
        EditItemPrice = 0;
        EditItemCategoryId = SelectedCategory?.Id ?? Categories.FirstOrDefault()?.Id;
        IsItemDialogOpen = true;
        OnPropertyChanged(nameof(IsItemEditMode));
    }

    [RelayCommand]
    private void OpenEditItemDialog(RestaurantMenuItem? item)
    {
        if (item is null) return;
        _editingItemId = item.Id;
        EditItemName = item.Name;
        EditItemPrice = item.SalePrice;
        EditItemCategoryId = item.RestaurantMenuCategoryId;
        IsItemDialogOpen = true;
        OnPropertyChanged(nameof(IsItemEditMode));
    }

    [RelayCommand]
    private void CloseItemDialog()
    {
        IsItemDialogOpen = false;
        _editingItemId = null;
        OnPropertyChanged(nameof(IsItemEditMode));
    }

    [RelayCommand]
    private async Task SaveItemAsync()
    {
        if (!EditItemCategoryId.HasValue) return;
        try
        {
            var item = new RestaurantMenuItem
            {
                Id = _editingItemId ?? 0,
                Name = EditItemName,
                SalePrice = EditItemPrice,
                RestaurantMenuCategoryId = EditItemCategoryId.Value,
                IsActive = true
            };

            RestaurantRecipe? recipe = null;
            IReadOnlyList<RestaurantRecipeLine>? lines = null;
            if (_editingItemId is null && Ingredients.Count > 0)
            {
                recipe = new RestaurantRecipe { Name = EditItemName };
                lines = [new RestaurantRecipeLine { RestaurantIngredientId = Ingredients[0].Id, Quantity = 0.1m }];
            }

            await _menuService.SaveMenuItemAsync(item, recipe, lines);
            CloseItemDialog();
            await LoadMenuItemsAsync();
            _toast.ShowSuccess("تم حفظ الصنف");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteItemAsync(RestaurantMenuItem? item)
    {
        if (item is null || !CanDelete) return;
        try
        {
            await _menuService.DeleteMenuItemAsync(item.Id, _currentUserService.Username ?? "system");
            await LoadMenuItemsAsync();
            _toast.ShowSuccess("تم حذف الصنف");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }
}
