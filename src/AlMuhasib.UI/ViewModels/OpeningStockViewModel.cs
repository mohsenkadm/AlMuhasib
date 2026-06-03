using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class OpeningStockViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private bool _initialized;

    public ObservableCollection<OpeningStockRow> Rows { get; } = [];
    public ObservableCollection<Warehouse> Warehouses { get; } = [];
    public ObservableCollection<Product> Products { get; } = [];

    [ObservableProperty]
    private Warehouse? _selectedWarehouse;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isSaved;

    public OpeningStockViewModel(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        PageTitle = "الأرصدة الافتتاحية للمنتجات";
    }

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "OpeningStock");

            var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
            Warehouses.Clear();
            foreach (var w in warehouses)
                Warehouses.Add(w);

            var products = await _unitOfWork.Products.GetAllAsync();
            Products.Clear();
            foreach (var p in products)
                Products.Add(p);

            _initialized = true;

            // Now select warehouse which triggers LoadExistingStockAsync
            if (Warehouses.Count > 0)
                SelectedWarehouse = Warehouses[0];
        }
        finally { IsBusy = false; }
    }

    partial void OnSelectedWarehouseChanged(Warehouse? value)
    {
        if (value is not null && _initialized)
            _ = LoadExistingStockAsync();
    }

    private async Task LoadExistingStockAsync()
    {
        if (SelectedWarehouse is null || Products.Count == 0) return;
        IsBusy = true;
        try
        {
            var stocks = await _unitOfWork.WarehouseStocks.FindAsync(
                s => s.WarehouseId == SelectedWarehouse.Id);
            var stockDict = stocks.ToDictionary(s => s.ProductId, s => s.Quantity);

            Rows.Clear();
            var stockDetails = stocks.ToDictionary(s => s.ProductId);

            foreach (var product in Products)
            {
                stockDetails.TryGetValue(product.Id, out var stock);
                Rows.Add(new OpeningStockRow
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = stock?.Quantity ?? 0,
                    UnitCost = stock?.UnitCost ?? 0
                });
            }

            IsSaved = false;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void AddRow()
    {
        Rows.Add(new OpeningStockRow());
    }

    [RelayCommand]
    private void RemoveRow(OpeningStockRow? row)
    {
        if (row is not null)
            Rows.Remove(row);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;

        if (SelectedWarehouse is null)
        {
            ErrorMessage = "يرجى اختيار المخزن";
            return;
        }

        var validRows = Rows.Where(r => r.ProductId > 0 && r.Quantity > 0).ToList();
        if (validRows.Count == 0)
        {
            ErrorMessage = "لا توجد بيانات للحفظ";
            return;
        }

        if (validRows.Any(r => r.Quantity > 0 && r.UnitCost <= 0 && r.TotalCost <= 0))
        {
            ErrorMessage = "يرجى إدخال سعر الوحدة أو الإجمالي الكلي لكل منتج له رصيد افتتاحي";
            return;
        }

        foreach (var row in validRows.Where(r => r.UnitCost <= 0 && r.TotalCost > 0 && r.Quantity > 0))
            row.UnitCost = row.TotalCost / row.Quantity;

        if (validRows.Any(r => r.UnitCost <= 0))
        {
            ErrorMessage = "يرجى إدخال سعر الوحدة أو الإجمالي الكلي لكل منتج له رصيد افتتاحي";
            return;
        }

        IsBusy = true;
        try
        {
            var username = _currentUserService.Username;

            foreach (var row in validRows)
            {
                var existing = (await _unitOfWork.WarehouseStocks.FindAsync(
                    s => s.WarehouseId == SelectedWarehouse.Id && s.ProductId == row.ProductId))
                    .FirstOrDefault();

                if (existing is not null)
                {
                    existing.Quantity = row.Quantity;
                    existing.OpeningQuantity = row.Quantity;
                    existing.UnitCost = row.UnitCost;
                    existing.UpdatedBy = username;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.WarehouseStocks.Update(existing);
                }
                else
                {
                    await _unitOfWork.WarehouseStocks.AddAsync(new WarehouseStock
                    {
                        WarehouseId = SelectedWarehouse.Id,
                        ProductId = row.ProductId,
                        Quantity = row.Quantity,
                        OpeningQuantity = row.Quantity,
                        UnitCost = row.UnitCost,
                        CreatedBy = username,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
            IsSaved = true;
            BeautifulMessageDialog.ShowSuccess("تم حفظ الأرصدة الافتتاحية بنجاح");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
        }
        finally { IsBusy = false; }
    }
}

public partial class OpeningStockRow : ObservableObject
{
    [ObservableProperty] private int _productId;
    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private decimal _quantity;
    [ObservableProperty] private decimal _unitCost;
    [ObservableProperty] private decimal _totalCost;

    private bool _isManualTotal;
    private bool _isRecalculating;

    partial void OnQuantityChanged(decimal value)
    {
        if (_isRecalculating) return;
        if (_isManualTotal)
            RecalcUnitCost();
        else
            RecalcTotal();
    }

    partial void OnUnitCostChanged(decimal value)
    {
        if (_isRecalculating) return;
        _isManualTotal = false;
        RecalcTotal();
    }

    partial void OnTotalCostChanged(decimal oldValue, decimal newValue)
    {
        if (_isRecalculating) return;
        _isManualTotal = true;
        RecalcUnitCost();
    }

    private void RecalcTotal()
    {
        if (_isManualTotal) return;
        _isRecalculating = true;
        TotalCost = Quantity * UnitCost;
        _isRecalculating = false;
    }

    private void RecalcUnitCost()
    {
        if (Quantity <= 0) return;
        _isRecalculating = true;
        UnitCost = TotalCost / Quantity;
        _isRecalculating = false;
    }
}
