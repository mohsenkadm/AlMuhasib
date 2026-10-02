using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;

namespace AlMuhasib.UI.ViewModels;

public partial class StockAdjustmentViewModel : ViewModelBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private bool _initialized;

    public ObservableCollection<StockAdjustmentRow> Rows { get; } = [];
    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    [ObservableProperty]
    private Warehouse? _selectedWarehouse;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private int _changedCount;

    public StockAdjustmentViewModel(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        PageTitle = "تسوية مخزنية";
    }

    public override bool HasUnsavedChanges => Rows.Any(r => r.HasChange);

    public override async Task InitializeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            LoadPermissions(_currentUserService, "StockAdjustment");

            var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
            Warehouses.Clear();
            foreach (var w in warehouses)
                Warehouses.Add(w);

            _initialized = true;

            if (Warehouses.Count > 0)
                SelectedWarehouse = Warehouses[0];
        }
        finally { IsBusy = false; }
    }

    partial void OnSelectedWarehouseChanged(Warehouse? value)
    {
        if (value is not null && _initialized)
            _ = LoadStockAsync();
    }

    private async Task LoadStockAsync()
    {
        if (SelectedWarehouse is null) return;
        IsBusy = true;
        try
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            var stocks = await _unitOfWork.WarehouseStocks.FindAsync(
                s => s.WarehouseId == SelectedWarehouse.Id);
            var stockDict = stocks.ToDictionary(s => s.ProductId);

            Rows.Clear();
            foreach (var product in products)
            {
                stockDict.TryGetValue(product.Id, out var stock);
                var current = stock?.Quantity ?? 0;
                var row = new StockAdjustmentRow
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    CurrentQuantity = current,
                    NewQuantity = current
                };
                row.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName is nameof(StockAdjustmentRow.Difference)
                        or nameof(StockAdjustmentRow.HasChange))
                        UpdateChangedCount();
                };
                Rows.Add(row);
            }

            UpdateChangedCount();
            ErrorMessage = string.Empty;
        }
        finally { IsBusy = false; }
    }

    private void UpdateChangedCount()
    {
        ChangedCount = Rows.Count(r => r.HasChange);
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

        var changedRows = Rows.Where(r => r.HasChange).ToList();
        if (changedRows.Count == 0)
        {
            ErrorMessage = "لا توجد تسويات للحفظ";
            return;
        }

        if (changedRows.Any(r => r.NewQuantity < 0))
        {
            ErrorMessage = "الكمية الجديدة لا يمكن أن تكون سالبة";
            return;
        }

        IsBusy = true;
        try
        {
            var username = _currentUserService.Username;
            var warehouseId = SelectedWarehouse.Id;
            var productIds = changedRows.Select(r => r.ProductId).ToHashSet();

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingStocks = (await _unitOfWork.WarehouseStocks.FindAsync(
                        s => s.WarehouseId == warehouseId && productIds.Contains(s.ProductId)))
                    .ToDictionary(s => s.ProductId);

                foreach (var row in changedRows)
                {
                    if (existingStocks.TryGetValue(row.ProductId, out var existing))
                    {
                        existing.Quantity = row.NewQuantity;
                        existing.UpdatedBy = username;
                        existing.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.WarehouseStocks.Update(existing);
                    }
                    else if (row.NewQuantity > 0)
                    {
                        await _unitOfWork.WarehouseStocks.AddAsync(new WarehouseStock
                        {
                            WarehouseId = warehouseId,
                            ProductId = row.ProductId,
                            Quantity = row.NewQuantity,
                            OpeningQuantity = 0,
                            UnitCost = 0,
                            CreatedBy = username,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            foreach (var row in changedRows)
            {
                row.CurrentQuantity = row.NewQuantity;
                row.RecalculateDifference();
            }

            UpdateChangedCount();
            BeautifulMessageDialog.ShowSuccess($"تم حفظ {changedRows.Count} تسوية مخزنية بنجاح");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void ResetChanges()
    {
        foreach (var row in Rows)
            row.NewQuantity = row.CurrentQuantity;

        UpdateChangedCount();
        ErrorMessage = string.Empty;
    }

}

public partial class StockAdjustmentRow : ObservableObject
{
    [ObservableProperty] private int _productId;
    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private decimal _currentQuantity;
    [ObservableProperty] private decimal _newQuantity;
    [ObservableProperty] private decimal _difference;

    public bool HasChange => Difference != 0;

    public string DifferenceDisplay => Difference > 0
        ? $"+{Difference:N0}"
        : Difference < 0
            ? $"{Difference:N0}"
            : "0";

    partial void OnNewQuantityChanged(decimal value)
    {
        RecalculateDifference();
    }

    public bool IsPositiveDifference => Difference > 0;
    public bool IsNegativeDifference => Difference < 0;

    public void RecalculateDifference()
    {
        Difference = NewQuantity - CurrentQuantity;
        OnPropertyChanged(nameof(HasChange));
        OnPropertyChanged(nameof(DifferenceDisplay));
        OnPropertyChanged(nameof(IsPositiveDifference));
        OnPropertyChanged(nameof(IsNegativeDifference));
    }
}
