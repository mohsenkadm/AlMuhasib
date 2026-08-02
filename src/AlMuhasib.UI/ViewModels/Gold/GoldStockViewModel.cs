using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.Core.Models.Gold;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace AlMuhasib.UI.ViewModels.Gold;

public partial class GoldStockViewModel : ViewModelBase
{
    private readonly IGoldInventoryService _inventoryService;
    private readonly IGoldWarehouseService _warehouseService;
    private readonly IExportService _exportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly MainWindowViewModel _mainWindow;
    private List<GoldStockRow> _allRows = [];
    private bool _isLoadingWarehouses;

    [ObservableProperty] private GoldStockRow? _selectedRow;
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private decimal _totalGrams;
    [ObservableProperty] private decimal _totalValue;
    [ObservableProperty] private GoldWarehouse? _selectedWarehouse;

    public ObservableCollection<GoldStockRow> StockRows { get; } = [];
    public ObservableCollection<GoldWarehouse> Warehouses { get; } = [];

    public GoldStockViewModel(
        IGoldInventoryService inventoryService,
        IGoldWarehouseService warehouseService,
        IExportService exportService,
        ICurrentUserService currentUserService,
        MainWindowViewModel mainWindow)
    {
        _inventoryService = inventoryService;
        _warehouseService = warehouseService;
        _exportService = exportService;
        _currentUserService = currentUserService;
        _mainWindow = mainWindow;
        PageTitle = "المخزون";
    }

    public override async Task InitializeAsync()
    {
        LoadPermissions(_currentUserService, GoldShopPermissionRegistry.Stock);
        await LoadWarehousesAsync();
        await LoadAsync();
    }

    private async Task LoadWarehousesAsync()
    {
        _isLoadingWarehouses = true;
        try
        {
            Warehouses.Clear();
            Warehouses.Add(new GoldWarehouse { Id = 0, Name = "كل المخازن" });
            foreach (var w in await _warehouseService.GetAllAsync(activeOnly: true))
                Warehouses.Add(w);

            SelectedWarehouse = Warehouses.FirstOrDefault();
        }
        finally
        {
            _isLoadingWarehouses = false;
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            int? warehouseId = SelectedWarehouse is null || SelectedWarehouse.Id == 0
                ? null
                : SelectedWarehouse.Id;

            _allRows = (await _inventoryService.GetStockBalancesAsync(warehouseId)).ToList();
            LowStockCount = _allRows.Count(r => r.IsLowStock);
            TotalGrams = _allRows.Sum(r => r.GramsOnHand);
            TotalValue = _allRows.Sum(r => r.StockValue);
            ApplyFilters();
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

    partial void OnSelectedWarehouseChanged(GoldWarehouse? value)
    {
        if (_isLoadingWarehouses) return;
        _ = LoadAsync();
    }

    private void ApplyFilters()
    {
        var filtered = MasterDataColumnFilterHelper.HasActiveColumnFilters(ColumnFilters)
            ? ColumnFilterEngine.Apply(_allRows, ColumnFilters)
            : _allRows.ToList();

        StockRows.Clear();
        foreach (var row in filtered)
            StockRows.Add(row);
    }

    protected override void OnColumnFiltersChanged() => ApplyFilters();

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task OpenAdjustmentAsync() =>
        await _mainWindow.OpenTabAsync(
            typeof(GoldStockAdjustmentViewModel),
            "تسوية مخزون",
            MaterialDesignThemes.Wpf.PackIconKind.TuneVerticalVariant);

    [RelayCommand]
    private async Task ExportToExcel()
    {
        try
        {
            if (_allRows.Count == 0)
                await LoadAsync();

            var exportData = _allRows.Select(r => new
            {
                المخزن = r.WarehouseName,
                العيار = r.KaratName,
                القيمة = r.KaratValue,
                الوزن = r.GramsOnHand,
                متوسط_التكلفة = r.AverageCostPerGram,
                قيمة_المخزون = r.StockValue,
                عدد_القطع = r.PieceCount,
                منخفض = r.IsLowStock ? "نعم" : "لا"
            });

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"مخزون_الذهب_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
                DefaultExt = ".xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportService.ExportToExcelFileAsync(exportData, dialog.FileName, "المخزون");
                BeautifulMessageDialog.ShowSuccess("تم التصدير بنجاح");
            }
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء التصدير: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintTable()
    {
        try
        {
            if (_allRows.Count == 0)
                await LoadAsync();

            var columns = new[] { "المخزن", "العيار", "القيمة", "الوزن (غ)", "متوسط التكلفة/غ", "قيمة المخزون", "عدد القطع", "تنبيه" };
            IList<object[]> rows = _allRows.Select(r => new object[]
            {
                r.WarehouseName,
                r.KaratName,
                r.KaratValue,
                r.GramsOnHand.ToString("N2"),
                r.AverageCostPerGram.ToString("N0"),
                r.StockValue.ToString("N0"),
                r.PieceCount,
                r.IsLowStock ? "منخفض" : ""
            }).ToList();
            _exportService.PrintTable("مخزون الذهب", columns, rows);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError($"حدث خطأ أثناء الطباعة: {ex.Message}");
        }
    }
}
