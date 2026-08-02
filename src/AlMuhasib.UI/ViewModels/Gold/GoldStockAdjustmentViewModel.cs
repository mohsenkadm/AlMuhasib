using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldStockAdjustmentViewModel : ViewModelBase
{
    private readonly IGoldInventoryService _inventoryService;
    private readonly IGoldPricingService _pricingService;
    private readonly IGoldWarehouseService _warehouseService;
    private readonly ICurrentUserService _currentUserService;

    [ObservableProperty] private int _karatValue = 21;
    [ObservableProperty] private decimal _gramsDelta;
    [ObservableProperty] private decimal? _costPerGram;
    [ObservableProperty] private string _notes = string.Empty;
    [ObservableProperty] private string _formError = string.Empty;
    [ObservableProperty] private string _currentBalanceText = "—";
    [ObservableProperty] private GoldWarehouse? _selectedWarehouse;

    public ObservableCollection<GoldKarat> Karats { get; } = [];
    public ObservableCollection<GoldWarehouse> Warehouses { get; } = [];

    public GoldStockAdjustmentViewModel(
        IGoldInventoryService inventoryService,
        IGoldPricingService pricingService,
        IGoldWarehouseService warehouseService,
        ICurrentUserService currentUserService)
    {
        _inventoryService = inventoryService;
        _pricingService = pricingService;
        _warehouseService = warehouseService;
        _currentUserService = currentUserService;
        PageTitle = "تسوية مخزون";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.StockAdjustment);

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
            CurrentBalanceText = balance is null
                ? "لا يوجد رصيد مسجّل لهذا العيار"
                : $"الرصيد الحالي: {balance.GramsOnHand:N2} غ — متوسط التكلفة: {balance.AverageCostPerGram:N0}";
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
            BeautifulMessageDialog.ShowWarning("ليس لديك صلاحية تسوية المخزون");
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

        if (GramsDelta == 0)
        {
            FormError = "أدخل فرق الوزن (+/-)";
            return;
        }

        if (!BeautifulMessageDialog.ShowConfirm(
                $"تأكيد تسوية مخزون عيار {KaratValue} بمقدار {GramsDelta:N2} غرام في «{SelectedWarehouse.Name}»؟",
                "تسوية المخزون"))
            return;

        try
        {
            IsBusy = true;
            FormError = string.Empty;
            await _inventoryService.AdjustStockAsync(
                KaratValue,
                GramsDelta,
                CostPerGram,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                SelectedWarehouse.Id);

            BeautifulMessageDialog.ShowSuccess("تم تنفيذ التسوية");
            GramsDelta = 0;
            CostPerGram = null;
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

    [RelayCommand]
    private void ResetForm()
    {
        GramsDelta = 0;
        CostPerGram = null;
        Notes = string.Empty;
        FormError = string.Empty;
    }
}
