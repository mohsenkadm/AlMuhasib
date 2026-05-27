using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Infrastructure.Services;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public enum InvoicePickerMode
{
    Sale,
    Purchase,
    Installment
}

public partial class ProductPickerDisplayItem : ObservableObject
{
    public ProductPickerDisplayItem(Product product, string categoryName)
    {
        Product = product;
        CategoryName = categoryName;
    }

    public Product Product { get; }
    public int ProductId => Product.Id;
    public string Name => Product.Name;
    public string CategoryName { get; }
    public string? Barcode => Product.Barcode;

    [ObservableProperty]
    private decimal _quantity;

    [ObservableProperty]
    private string _stockLabel = string.Empty;

    public bool IsSelected => Quantity > 0;

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(IsSelected));
}

public partial class ProductPickerViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly Dictionary<int, decimal> _quantities = new();
    private List<Product> _allProducts = [];
    private List<Category> _categories = [];
    private Dictionary<int, string> _categoryNames = [];
    private Dictionary<int, decimal> _stockByProduct = [];
    private Dictionary<int, decimal> _suggestedPrices = [];
    private int? _warehouseId;

    public ObservableCollection<Category> Categories { get; } = [];
    public ObservableCollection<ProductPickerDisplayItem> DisplayProducts { get; } = [];

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _selectedProductCount;

    [ObservableProperty]
    private decimal _selectedTotalQuantity;

    [ObservableProperty]
    private bool _isAllCategoriesSelected = true;

    public event Action? Confirmed;
    public event Action? Cancelled;

    public ProductPickerViewModel(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task InitializeAsync(int? warehouseId, InvoicePickerMode mode)
    {
        _warehouseId = warehouseId;
        _quantities.Clear();
        _suggestedPrices.Clear();

        var products = (await _unitOfWork.Products.GetAllAsync()).ToList();
        _allProducts = products;

        _categories = (await _unitOfWork.Categories.GetAllAsync()).ToList();
        _categoryNames = _categories.ToDictionary(c => c.Id, c => c.Name);

        Categories.Clear();
        foreach (var c in _categories.OrderBy(c => c.Name))
            Categories.Add(c);

        await LoadStockAsync();
        await LoadSuggestedPricesAsync(mode);

        IsAllCategoriesSelected = true;
        SelectedCategory = null;
        SearchText = string.Empty;
        RefreshDisplayProducts();
        UpdateSummary();
    }

    public void SeedFromInvoiceItems(IEnumerable<InvoiceItemRow> rows)
    {
        foreach (var row in rows)
        {
            if (row.ProductId is not { } id || id <= 0)
                continue;

            _quantities[id] = _quantities.GetValueOrDefault(id) + Math.Max(0, row.Quantity);
        }

        RefreshDisplayProducts();
        UpdateSummary();
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        IsAllCategoriesSelected = value is null;
        RefreshDisplayProducts();
    }

    partial void OnSearchTextChanged(string value) => RefreshDisplayProducts();

    [RelayCommand]
    private void SelectAllCategories()
    {
        SelectedCategory = null;
        IsAllCategoriesSelected = true;
    }

    [RelayCommand]
    private void SelectCategory(Category? category)
    {
        SelectedCategory = category;
        IsAllCategoriesSelected = category is null;
    }

    [RelayCommand]
    private void AddProduct(ProductPickerDisplayItem? item)
    {
        if (item is null)
            return;

        _quantities[item.ProductId] = _quantities.GetValueOrDefault(item.ProductId) + 1;
        SyncItemQuantity(item.ProductId);
        UpdateSummary();
    }

    [RelayCommand]
    private void RemoveProduct(ProductPickerDisplayItem? item)
    {
        if (item is null)
            return;

        var current = _quantities.GetValueOrDefault(item.ProductId);
        if (current <= 1)
            _quantities.Remove(item.ProductId);
        else
            _quantities[item.ProductId] = current - 1;

        SyncItemQuantity(item.ProductId);
        UpdateSummary();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        _quantities.Clear();
        foreach (var item in DisplayProducts)
            item.Quantity = 0;
        UpdateSummary();
    }

    [RelayCommand]
    private void Confirm()
    {
        Confirmed?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke();

    public IReadOnlyList<ProductPickerResult> BuildResults()
    {
        var results = new List<ProductPickerResult>();
        foreach (var (productId, qty) in _quantities)
        {
            if (qty <= 0)
                continue;

            var product = _allProducts.FirstOrDefault(p => p.Id == productId);
            if (product is null)
                continue;

            results.Add(new ProductPickerResult
            {
                Product = product,
                Quantity = qty,
                SuggestedUnitPrice = _suggestedPrices.GetValueOrDefault(productId)
            });
        }

        return results;
    }

    private async Task LoadStockAsync()
    {
        _stockByProduct.Clear();
        var stocks = await _unitOfWork.WarehouseStocks.FindAsync(_ => true);
        foreach (var group in stocks.GroupBy(s => s.ProductId))
        {
            _stockByProduct[group.Key] = _warehouseId is int wid
                ? group.Where(s => s.WarehouseId == wid).Sum(s => s.Quantity)
                : group.Sum(s => s.Quantity);
        }
    }

    private async Task LoadSuggestedPricesAsync(InvoicePickerMode mode)
    {
        if (mode == InvoicePickerMode.Purchase)
        {
            var stocks = await _unitOfWork.WarehouseStocks.FindAsync(_ => true);
            var purchases = (await _unitOfWork.InvoiceItems.FindAsync(i => i.ProductId != null)).ToList();
            var purchaseInvoices = (await _unitOfWork.Invoices.FindAsync(i => i.InvoiceType == InvoiceType.Purchase))
                .Select(i => i.Id)
                .ToHashSet();
            var purchaseItems = purchases.Where(i => i.ProductId is not null && purchaseInvoices.Contains(i.InvoiceId)).ToList();

            foreach (var product in _allProducts)
            {
                _suggestedPrices[product.Id] = Math.Round(
                    ProductCostHelper.ComputeAverageUnitCostForProduct(purchaseItems, stocks, product.Id), 0);
            }

            return;
        }

        var saleItems = (await _unitOfWork.InvoiceItems.FindAsync(i => i.ProductId != null)).ToList();
        foreach (var product in _allProducts)
        {
            var lastSale = saleItems
                .Where(i => i.ProductId == product.Id && i.UnitPrice > 0)
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();
            _suggestedPrices[product.Id] = lastSale?.UnitPrice ?? 0;
        }
    }

    private void RefreshDisplayProducts()
    {
        var term = SearchText?.Trim() ?? string.Empty;
        IEnumerable<Product> query = _allProducts;

        if (SelectedCategory is not null)
            query = query.Where(p => p.CategoryId == SelectedCategory.Id);

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(p =>
                p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (p.Barcode?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var ordered = query.OrderBy(p => p.Name).ToList();
        DisplayProducts.Clear();

        foreach (var product in ordered)
        {
            var catName = _categoryNames.GetValueOrDefault(product.CategoryId, "—");
            var item = new ProductPickerDisplayItem(product, catName)
            {
                Quantity = _quantities.GetValueOrDefault(product.Id),
                StockLabel = FormatStock(product.Id)
            };
            DisplayProducts.Add(item);
        }
    }

    private void SyncItemQuantity(int productId)
    {
        var qty = _quantities.GetValueOrDefault(productId);
        var display = DisplayProducts.FirstOrDefault(p => p.ProductId == productId);
        if (display is not null)
        {
            display.Quantity = qty;
            return;
        }

        RefreshDisplayProducts();
    }

    private void UpdateSummary()
    {
        SelectedProductCount = _quantities.Count(kv => kv.Value > 0);
        SelectedTotalQuantity = _quantities.Values.Sum();
    }

    private string FormatStock(int productId)
    {
        if (!_stockByProduct.TryGetValue(productId, out var qty))
            return "رصيد: 0";

        return $"رصيد: {qty:N0}";
    }
}
