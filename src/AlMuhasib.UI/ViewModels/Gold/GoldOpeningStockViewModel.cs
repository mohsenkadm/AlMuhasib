using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldOpeningStockViewModel : ViewModelBase
{
    private readonly IGoldOpeningBalanceService _openingService;
    private readonly IGoldInventoryService _inventoryService;
    private readonly IGoldPricingService _pricingService;
    private readonly IGoldWarehouseService _warehouseService;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private int _karatValue = 21;
    [ObservableProperty] private decimal _gramsOnHand;
    [ObservableProperty] private decimal? _costPerGram;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _formError = string.Empty;
    [ObservableProperty] private string _currentBalanceText = "—";
    [ObservableProperty] private GoldWarehouse? _selectedWarehouse;

    public ObservableCollection<GoldKarat> Karats { get; } = [];
    public ObservableCollection<GoldWarehouse> Warehouses { get; } = [];

    public GoldOpeningStockViewModel(
        IGoldOpeningBalanceService openingService,
        IGoldInventoryService inventoryService,
        IGoldPricingService pricingService,
        IGoldWarehouseService warehouseService,
        ICurrentUserService currentUserService)
    {
        _openingService = openingService;
        _inventoryService = inventoryService;
        _pricingService = pricingService;
        _warehouseService = warehouseService;
        _currentUserService = currentUserService;
        PageTitle = "رصيد افتتاحي للمخزون";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.OpeningStock);

        Warehouses.Clear();
        foreach (var w in await _warehouseService.GetAllAsync(activeOnly: true))
            Warehouses.Add(w);
        SelectedWarehouse = Warehouses.FirstOrDefault(w => w.IsDefault) ?? Warehouses.FirstOrDefault();

        Karats.Clear();
        foreach (var karat in await _pricingService.GetKaratsAsync())
            Karats.Add(karat);

        if (Karats.Count > 0)
            KaratValue = Karats[0].KaratValue;

        await RefreshBalanceAsync();
    }

    partial void OnKaratValueChanged(int value) => _ = RefreshBalanceAsync();
    partial void OnSelectedWarehouseChanged(GoldWarehouse? value) => _ = RefreshBalanceAsync();

    private async Task RefreshBalanceAsync()
    {
        try
        {
            var balance = await _inventoryService.GetStockBalanceByKaratAsync(
                KaratValue, SelectedWarehouse?.Id);
            if (balance is null)
            {
                CurrentBalanceText = "لا يوجد رصيد مسجّل لهذا العيار";
                GramsOnHand = 0;
            }
            else
            {
                CurrentBalanceText =
                    $"الرصيد الحالي: {balance.GramsOnHand:N2} غ — متوسط التكلفة: {balance.AverageCostPerGram:N0}";
                GramsOnHand = balance.GramsOnHand;
                CostPerGram = balance.AverageCostPerGram > 0 ? balance.AverageCostPerGram : null;
            }
        }
        catch
        {
            CurrentBalanceText = "—";
        }
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanAdd && !CanEdit)
        {
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تعيين رصيد الافتتاح");
            return;
        }

        if (SelectedWarehouse is null)
        {
            FormError = "اختر المخزن";
            return;
        }

        if (KaratValue <= 0)
        {
            FormError = "اختر العيار";
            return;
        }

        if (GramsOnHand < 0)
        {
            FormError = "الرصيد لا يمكن أن يكون سالباً";
            return;
        }

        if (!BeautifulMessageDialog.ShowConfirm(
                $"تعيين رصيد افتتاحي لعيار {KaratValue} بمقدار {GramsOnHand:N2} غرام في «{SelectedWarehouse.Name}»؟",
                "رصيد الافتتاح"))
            return;

        try
        {
            IsBusy = true;
            FormError = string.Empty;
            await _openingService.SetOpeningStockAsync(new GoldOpeningStockRequest
            {
                WarehouseId = SelectedWarehouse.Id,
                KaratValue = KaratValue,
                GramsOnHand = GramsOnHand,
                CostPerGram = CostPerGram,
                Notes = string.IsNullOrWhiteSpace(Notes) ? "رصيد افتتاحي" : Notes.Trim()
            });

            BeautifulMessageDialog.ShowSuccess("تم تعيين رصيد الافتتاح");
            Notes = string.Empty;
            await RefreshBalanceAsync();
        }
        catch (Exception ex)
        {
            FormError = ex.Message;
            BeautifulMessageDialog.ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
