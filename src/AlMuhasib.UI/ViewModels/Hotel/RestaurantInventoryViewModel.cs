using AlMuhasib.Core.Entities.Hotel;
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

public partial class RestaurantInventoryViewModel : ViewModelBase
{
    private readonly IRestaurantInventoryService _inventoryService;
    private readonly IHotelCashService _cashService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IToastNotificationService _toast;

    public ObservableCollection<RestaurantIngredient> Ingredients { get; } = [];
    public ObservableCollection<HotelCashBox> CashBoxes { get; } = [];
    public ObservableCollection<HotelListStatItem> Stats { get; } = [];

    [ObservableProperty] private RestaurantIngredient? _selectedIngredient;
    [ObservableProperty] private bool _isIngredientDialogOpen;
    [ObservableProperty] private bool _isPurchaseDialogOpen;
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private string _editUnit = "كغ";
    [ObservableProperty] private decimal _editMinQuantity;
    [ObservableProperty] private decimal _editAverageCost;
    [ObservableProperty] private decimal _editInitialQuantity;
    [ObservableProperty] private decimal _purchaseQuantity;
    [ObservableProperty] private decimal _purchaseUnitCost;
    [ObservableProperty] private int? _purchaseCashBoxId;
    [ObservableProperty] private string _purchaseNotes = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;

    private int? _editingId;
    private List<RestaurantIngredient> _allIngredients = [];

    public bool IsEditMode => _editingId.HasValue;
    public decimal PurchaseTotal => PurchaseQuantity * PurchaseUnitCost;

    public RestaurantInventoryViewModel(
        IRestaurantInventoryService inventoryService,
        IHotelCashService cashService,
        ICurrentUserService currentUserService,
        IToastNotificationService toast)
    {
        _inventoryService = inventoryService;
        _cashService = cashService;
        _currentUserService = currentUserService;
        _toast = toast;
        PageTitle = "مخزون المطبخ";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, HotelPermissionRegistry.RestaurantInventory);
        CashBoxes.Clear();
        foreach (var c in await _cashService.GetCashBoxesAsync())
            CashBoxes.Add(c);
        await LoadIngredientsAsync();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    protected override void OnColumnFiltersChanged() => ApplyFilters();
    partial void OnPurchaseQuantityChanged(decimal value) => OnPropertyChanged(nameof(PurchaseTotal));
    partial void OnPurchaseUnitCostChanged(decimal value) => OnPropertyChanged(nameof(PurchaseTotal));

    [RelayCommand]
    private async Task LoadIngredientsAsync()
    {
        _allIngredients = (await _inventoryService.GetIngredientsAsync(activeOnly: false)).ToList();
        UpdateStats(_allIngredients);
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var items = _allIngredients.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
            items = items.Where(i => i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        if (MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters))
            items = ColumnFilterEngine.Apply(items, ColumnFilters);

        Ingredients.Clear();
        foreach (var i in items)
            Ingredients.Add(i);
    }

    private void UpdateStats(IReadOnlyList<RestaurantIngredient> all)
    {
        Stats.Clear();
        var lowStock = all.Count(i => i.Stock?.Quantity <= i.MinQuantity);
        var totalValue = all.Sum(i => (i.Stock?.Quantity ?? 0) * i.AverageCost);
        Stats.Add(new HotelListStatItem { Label = "المكونات", Value = all.Count.ToString("N0"), AccentColor = "#1565C0" });
        Stats.Add(new HotelListStatItem { Label = "منخفض المخزون", Value = lowStock.ToString("N0"), AccentColor = "#F57C00" });
        Stats.Add(new HotelListStatItem { Label = "قيمة المخزون", Value = totalValue.ToString("N0"), AccentColor = "#00897B" });
    }

    [RelayCommand]
    private void OpenAddDialog()
    {
        _editingId = null;
        EditName = string.Empty;
        EditUnit = "كغ";
        EditMinQuantity = 1;
        EditAverageCost = 0;
        EditInitialQuantity = 0;
        IsIngredientDialogOpen = true;
        OnPropertyChanged(nameof(IsEditMode));
    }

    [RelayCommand]
    private void OpenEditDialog(RestaurantIngredient? ingredient)
    {
        if (ingredient is null) return;
        _editingId = ingredient.Id;
        EditName = ingredient.Name;
        EditUnit = ingredient.Unit;
        EditMinQuantity = ingredient.MinQuantity;
        EditAverageCost = ingredient.AverageCost;
        IsIngredientDialogOpen = true;
        OnPropertyChanged(nameof(IsEditMode));
    }

    [RelayCommand]
    private void CloseIngredientDialog()
    {
        IsIngredientDialogOpen = false;
        _editingId = null;
        OnPropertyChanged(nameof(IsEditMode));
    }

    [RelayCommand]
    private async Task SaveIngredientAsync()
    {
        try
        {
            if (_editingId is null)
            {
                await _inventoryService.CreateIngredientAsync(new RestaurantIngredient
                {
                    Name = EditName,
                    Unit = EditUnit,
                    MinQuantity = EditMinQuantity,
                    AverageCost = EditAverageCost
                }, EditInitialQuantity);
            }
            else
            {
                await _inventoryService.UpdateIngredientAsync(new RestaurantIngredient
                {
                    Id = _editingId.Value,
                    Name = EditName,
                    Unit = EditUnit,
                    MinQuantity = EditMinQuantity,
                    AverageCost = EditAverageCost,
                    IsActive = true
                });
            }

            CloseIngredientDialog();
            await LoadIngredientsAsync();
            _toast.ShowSuccess("تم الحفظ");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenPurchaseDialog(RestaurantIngredient? ingredient)
    {
        SelectedIngredient = ingredient;
        PurchaseQuantity = 1;
        PurchaseUnitCost = ingredient?.AverageCost ?? 0;
        PurchaseCashBoxId = CashBoxes.FirstOrDefault()?.Id;
        PurchaseNotes = string.Empty;
        IsPurchaseDialogOpen = true;
        OnPropertyChanged(nameof(PurchaseTotal));
    }

    [RelayCommand]
    private void ClosePurchaseDialog() => IsPurchaseDialogOpen = false;

    [RelayCommand]
    private async Task ConfirmPurchaseAsync()
    {
        if (SelectedIngredient is null) return;
        try
        {
            await _inventoryService.PurchaseStockAsync(
                SelectedIngredient.Id, PurchaseQuantity, PurchaseUnitCost, PurchaseCashBoxId, PurchaseNotes);
            ClosePurchaseDialog();
            await LoadIngredientsAsync();
            _toast.ShowSuccess("تمت عملية الشراء");
        }
        catch (Exception ex)
        {
            _toast.ShowError(ex.Message);
        }
    }
}
