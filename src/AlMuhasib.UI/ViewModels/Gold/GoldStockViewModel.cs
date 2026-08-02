using System.Collections.ObjectModel;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldStockViewModel : ViewModelBase
{
    private readonly IGoldInventoryService _inventoryService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;

    [ObservableProperty] private GoldStockRow? _selectedRow;
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private decimal _totalGrams;
    [ObservableProperty] private decimal _totalValue;

    public ObservableCollection<GoldStockRow> StockRows { get; } = [];

    public GoldStockViewModel(
        IGoldInventoryService inventoryService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _inventoryService = inventoryService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "المخزون";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Stock);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            StockRows.Clear();
            var rows = await _inventoryService.GetStockBalancesAsync();
            foreach (var row in rows)
                StockRows.Add(row);

            LowStockCount = rows.Count(r => r.IsLowStock);
            TotalGrams = rows.Sum(r => r.GramsOnHand);
            TotalValue = rows.Sum(r => r.StockValue);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"تعذر تحميل المخزون:\n{ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task OpenAdjustmentAsync() =>
        await _mainWindow.OpenTabAsync(
            typeof(GoldStockAdjustmentViewModel),
            "تسوية مخزون",
            MaterialDesignThemes.Wpf.PackIconKind.TuneVerticalVariant);
}
