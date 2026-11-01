using System.Collections.ObjectModel;
using System.Windows;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.ViewModels;

public partial class StockAdjustmentViewModel : ViewModelBase
{
    private const int DefaultPageSize = 50;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProductService _productService;

    /// <summary>التعديلات غير المحفوظة عبر الصفحات: ProductId → الكمية الجديدة.</summary>
    private readonly Dictionary<int, decimal> _pendingNewQuantities = new();

    private bool _initialized;
    private bool _suppressWarehouseReload;
    private bool _suppressPendingSync;
    private int _loadVersion;
    private System.Timers.Timer? _debounceTimer;

    public ObservableCollection<StockAdjustmentRow> Rows { get; } = [];
    public ObservableCollection<Warehouse> Warehouses { get; } = [];

    [ObservableProperty]
    private Warehouse? _selectedWarehouse;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private int _changedCount;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = DefaultPageSize;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _totalPages = 1;

    [ObservableProperty]
    private string _paginationText = string.Empty;

    public StockAdjustmentViewModel(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IProductService productService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _productService = productService;
        PageTitle = "تسوية مخزنية";
    }

    public override bool HasUnsavedChanges => _pendingNewQuantities.Count > 0;

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
            {
                _suppressWarehouseReload = true;
                SelectedWarehouse = Warehouses[0];
                _suppressWarehouseReload = false;
            }
        }
        finally { IsBusy = false; }

        if (SelectedWarehouse is not null)
            await LoadPageAsync();
    }

    partial void OnSelectedWarehouseChanged(Warehouse? value)
    {
        if (!_initialized || _suppressWarehouseReload || value is null)
            return;

        _pendingNewQuantities.Clear();
        UpdateChangedCount();
        CurrentPage = 1;
        _ = LoadPageAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (!_initialized) return;

        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(400);
        _debounceTimer.Elapsed += (_, _) =>
        {
            _debounceTimer?.Stop();
            CurrentPage = 1;
            _ = Application.Current.Dispatcher.InvokeAsync(async () => await LoadPageAsync());
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    private async Task LoadPageAsync()
    {
        if (SelectedWarehouse is null) return;

        var version = ++_loadVersion;
        IsBusy = true;
        try
        {
            var search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();
            var (products, totalCount) = await _productService.GetVisibleNamesPagedAsync(
                CurrentPage, PageSize, search);

            if (version != _loadVersion) return;

            TotalCount = totalCount;
            TotalPages = PaginationHelper.ComputeTotalPages(TotalCount, PageSize);
            var safePage = Math.Clamp(CurrentPage, 1, TotalPages);
            if (safePage != CurrentPage)
            {
                CurrentPage = safePage;
                (products, totalCount) = await _productService.GetVisibleNamesPagedAsync(
                    CurrentPage, PageSize, search);
                if (version != _loadVersion) return;
                TotalCount = totalCount;
                TotalPages = PaginationHelper.ComputeTotalPages(TotalCount, PageSize);
            }

            var productIds = products.Select(p => p.Id).ToList();
            var stockDict = new Dictionary<int, decimal>();
            if (productIds.Count > 0)
            {
                var warehouseId = SelectedWarehouse.Id;
                var stocks = await _unitOfWork.WarehouseStocks.FindAsync(
                    s => s.WarehouseId == warehouseId && productIds.Contains(s.ProductId));
                if (version != _loadVersion) return;
                stockDict = stocks.ToDictionary(s => s.ProductId, s => s.Quantity);
            }

            ClearRows();
            _suppressPendingSync = true;
            var startIndex = (CurrentPage - 1) * PageSize;
            for (var i = 0; i < products.Count; i++)
            {
                var (productId, productName) = products[i];
                stockDict.TryGetValue(productId, out var current);
                var newQty = _pendingNewQuantities.TryGetValue(productId, out var pending)
                    ? pending
                    : current;

                var row = new StockAdjustmentRow
                {
                    RowNumber = startIndex + i + 1,
                    ProductId = productId,
                    ProductName = productName,
                    CurrentQuantity = current,
                    NewQuantity = newQty
                };
                row.RecalculateDifference();
                row.PropertyChanged += OnRowPropertyChanged;
                Rows.Add(row);
            }
            _suppressPendingSync = false;

            PaginationText = PaginationHelper.BuildPaginationText(TotalCount, CurrentPage, PageSize);
            UpdateChangedCount();
            ErrorMessage = string.Empty;
        }
        finally
        {
            if (version == _loadVersion)
                IsBusy = false;
        }
    }

    private void ClearRows()
    {
        foreach (var row in Rows)
            row.PropertyChanged -= OnRowPropertyChanged;
        Rows.Clear();
    }

    private void OnRowPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_suppressPendingSync) return;
        if (sender is not StockAdjustmentRow row) return;
        if (e.PropertyName is not (nameof(StockAdjustmentRow.Difference)
            or nameof(StockAdjustmentRow.HasChange)
            or nameof(StockAdjustmentRow.NewQuantity)))
            return;

        if (row.HasChange)
            _pendingNewQuantities[row.ProductId] = row.NewQuantity;
        else
            _pendingNewQuantities.Remove(row.ProductId);

        UpdateChangedCount();
    }

    private void UpdateChangedCount()
    {
        ChangedCount = _pendingNewQuantities.Count;
    }

    [RelayCommand]
    private async Task FirstPage()
    {
        if (CurrentPage == 1) return;
        CurrentPage = 1;
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task PreviousPage()
    {
        if (CurrentPage <= 1) return;
        CurrentPage--;
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (CurrentPage >= TotalPages) return;
        CurrentPage++;
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task LastPage()
    {
        if (CurrentPage == TotalPages) return;
        CurrentPage = TotalPages;
        await LoadPageAsync();
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

        // مزامنة الصفحة الحالية قبل الحفظ
        foreach (var row in Rows)
        {
            if (row.HasChange)
                _pendingNewQuantities[row.ProductId] = row.NewQuantity;
            else
                _pendingNewQuantities.Remove(row.ProductId);
        }

        if (_pendingNewQuantities.Count == 0)
        {
            ErrorMessage = "لا توجد تسويات للحفظ";
            return;
        }

        if (_pendingNewQuantities.Values.Any(q => q < 0))
        {
            ErrorMessage = "الكمية الجديدة لا يمكن أن تكون سالبة";
            return;
        }

        IsBusy = true;
        try
        {
            var username = _currentUserService.Username;
            var warehouseId = SelectedWarehouse.Id;
            var productIds = _pendingNewQuantities.Keys.ToHashSet();
            var pendingSnapshot = _pendingNewQuantities.ToDictionary(kv => kv.Key, kv => kv.Value);

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var existingStocks = (await _unitOfWork.WarehouseStocks.FindAsync(
                        s => s.WarehouseId == warehouseId && productIds.Contains(s.ProductId)))
                    .ToDictionary(s => s.ProductId);

                foreach (var (productId, newQuantity) in pendingSnapshot)
                {
                    if (existingStocks.TryGetValue(productId, out var existing))
                    {
                        existing.Quantity = newQuantity;
                        existing.UpdatedBy = username;
                        existing.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.WarehouseStocks.Update(existing);
                    }
                    else if (newQuantity > 0)
                    {
                        await _unitOfWork.WarehouseStocks.AddAsync(new WarehouseStock
                        {
                            WarehouseId = warehouseId,
                            ProductId = productId,
                            Quantity = newQuantity,
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

            var savedCount = pendingSnapshot.Count;
            _pendingNewQuantities.Clear();

            _suppressPendingSync = true;
            foreach (var row in Rows)
            {
                if (pendingSnapshot.TryGetValue(row.ProductId, out var savedQty))
                {
                    row.CurrentQuantity = savedQty;
                    row.NewQuantity = savedQty;
                    row.RecalculateDifference();
                }
            }
            _suppressPendingSync = false;

            UpdateChangedCount();
            BeautifulMessageDialog.ShowSuccess($"تم حفظ {savedCount} تسوية مخزنية بنجاح");
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
        _pendingNewQuantities.Clear();

        _suppressPendingSync = true;
        foreach (var row in Rows)
        {
            row.NewQuantity = row.CurrentQuantity;
            row.RecalculateDifference();
        }
        _suppressPendingSync = false;

        UpdateChangedCount();
        ErrorMessage = string.Empty;
    }

}

public partial class StockAdjustmentRow : ObservableObject
{
    [ObservableProperty] private int _rowNumber;
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
