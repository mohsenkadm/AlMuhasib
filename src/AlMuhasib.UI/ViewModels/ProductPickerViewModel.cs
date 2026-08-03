using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Services;
using AlMuhasib.UI.Helpers;
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
    public string? ScientificName => Product.ScientificName;
    public bool HasScientificName => !string.IsNullOrWhiteSpace(ScientificName);

    [ObservableProperty]
    private decimal _quantity;

    [ObservableProperty]
    private string _stockLabel = string.Empty;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private bool _pricingEnabled;

    public ObservableCollection<WarehouseStockChip> WarehouseStocks { get; } = [];

    public ObservableCollection<ProductPricingOption> PricingOptions { get; } = [];

    [ObservableProperty]
    private ProductPricingOption? _selectedPricingOption;

    public bool IsSelected => Quantity > 0;

    public string SelectedPriceLabel =>
        SelectedPricingOption is null ? string.Empty : $"السعر: {SelectedPricingOption.Price:N0}";

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(IsSelected));

    partial void OnSelectedPricingOptionChanged(ProductPricingOption? value)
    {
        OnPropertyChanged(nameof(SelectedPriceLabel));
        PricingSelectionChanged?.Invoke(this);
    }

    public event Action<ProductPickerDisplayItem>? PricingSelectionChanged;
}

public partial class ProductPickerViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductPriceService? _productPriceService;
    private readonly bool _pricingEnabled;
    private readonly Dictionary<int, decimal> _quantities = new();
    private readonly Dictionary<int, int> _selectedPricingTypeIds = new();
    private List<Product> _allProducts = [];
    private List<Category> _categories = [];
    private Dictionary<int, string> _categoryNames = [];
    private Dictionary<int, decimal> _stockByProduct = [];
    private Dictionary<int, List<WarehouseStockChip>> _stocksByProduct = [];
    private Dictionary<int, decimal> _suggestedPrices = [];
    private Dictionary<int, List<ProductPricingOption>> _pricesByProduct = [];
    private int? _warehouseId;
    private InvoicePickerMode _mode;

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

    [ObservableProperty]
    private bool _isPricingMode;

    public event Action? Confirmed;
    public event Action? Cancelled;

    public ProductPickerViewModel(
        IUnitOfWork unitOfWork,
        IProductPriceService? productPriceService = null,
        bool pricingEnabled = false)
    {
        _unitOfWork = unitOfWork;
        _productPriceService = productPriceService;
        _pricingEnabled = pricingEnabled;
        IsPricingMode = pricingEnabled;
    }

    public async Task InitializeAsync(int? warehouseId, InvoicePickerMode mode)
    {
        _warehouseId = warehouseId;
        _mode = mode;
        _quantities.Clear();
        _selectedPricingTypeIds.Clear();
        SearchText = string.Empty;
        SelectedCategory = null;
        IsAllCategoriesSelected = true;

        _categories = (await _unitOfWork.Categories.GetAllAsync()).OrderBy(c => c.Name).ToList();
        _categoryNames = _categories.ToDictionary(c => c.Id, c => c.Name);
        Categories.Clear();
        foreach (var c in _categories)
            Categories.Add(c);

        _allProducts = (await _unitOfWork.Products.GetAllAsync()).OrderBy(p => p.Name).ToList();
        await LoadStockAsync();
        await LoadSuggestedPricesAsync(mode);
        if (_pricingEnabled && _productPriceService is not null)
            await LoadProductPricesAsync(mode);

        RefreshDisplayProducts();
        UpdateSummary();
    }

    /// <summary>يملأ كميات المنتقي من بنود الفاتورة الحالية.</summary>
    public void SeedFromInvoiceItems(IEnumerable<InvoiceItemRow> items)
    {
        foreach (var row in items.Where(i => i.ProductId is > 0 && i.Quantity != 0))
        {
            var id = row.ProductId!.Value;
            _quantities[id] = _quantities.GetValueOrDefault(id) + Math.Abs(row.Quantity);
            if (row.PricingTypeId is int pricingTypeId)
                _selectedPricingTypeIds[id] = pricingTypeId;
        }

        foreach (var productId in _quantities.Keys.ToList())
            SyncItemQuantity(productId);
        UpdateSummary();
    }

    partial void OnSearchTextChanged(string value) => RefreshDisplayProducts();
    partial void OnSelectedCategoryChanged(Category? value)
    {
        IsAllCategoriesSelected = value is null;
        RefreshDisplayProducts();
    }

    [RelayCommand]
    private void SelectAllCategories()
    {
        SelectedCategory = null;
        IsAllCategoriesSelected = true;
    }

    [RelayCommand]
    private void AddProduct(ProductPickerDisplayItem item)
    {
        if (item is null) return;
        EnsurePricingSelection(item);
        var current = _quantities.GetValueOrDefault(item.ProductId);
        _quantities[item.ProductId] = current + 1;
        if (item.SelectedPricingOption is not null)
            _selectedPricingTypeIds[item.ProductId] = item.SelectedPricingOption.PricingTypeId;
        SyncItemQuantity(item.ProductId);
        UpdateSummary();
    }

    [RelayCommand]
    private void RemoveProduct(ProductPickerDisplayItem item)
    {
        if (item is null) return;
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
    private void Confirm() => Confirmed?.Invoke();

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

            int? pricingTypeId = null;
            string? pricingTypeName = null;
            decimal suggested = _suggestedPrices.GetValueOrDefault(productId);

            if (_pricingEnabled &&
                _pricesByProduct.TryGetValue(productId, out var options) &&
                options.Count > 0)
            {
                var selectedTypeId = _selectedPricingTypeIds.GetValueOrDefault(productId);
                var option = options.FirstOrDefault(o => o.PricingTypeId == selectedTypeId)
                             ?? options.First();
                pricingTypeId = option.PricingTypeId;
                pricingTypeName = option.Name;
                suggested = option.Price;
            }

            results.Add(new ProductPickerResult
            {
                Product = product,
                Quantity = qty,
                SuggestedUnitPrice = suggested,
                PricingTypeId = pricingTypeId,
                PricingTypeName = pricingTypeName
            });
        }

        return results;
    }

    private void EnsurePricingSelection(ProductPickerDisplayItem item)
    {
        if (!_pricingEnabled || item.SelectedPricingOption is not null)
            return;
        if (item.PricingOptions.Count > 0)
            item.SelectedPricingOption = item.PricingOptions[0];
    }

    private async Task LoadStockAsync()
    {
        _stockByProduct.Clear();
        _stocksByProduct.Clear();
        var warehouses = (await _unitOfWork.Warehouses.GetAllAsync())
            .ToDictionary(w => w.Id, w => w.Name);
        var stocks = await _unitOfWork.WarehouseStocks.FindAsync(_ => true);
        foreach (var group in stocks.GroupBy(s => s.ProductId))
        {
            _stockByProduct[group.Key] = _warehouseId is int wid
                ? group.Where(s => s.WarehouseId == wid).Sum(s => s.Quantity)
                : group.Sum(s => s.Quantity);

            _stocksByProduct[group.Key] = group
                .OrderBy(s => warehouses.GetValueOrDefault(s.WarehouseId, string.Empty))
                .Select(s => new WarehouseStockChip
                {
                    WarehouseName = warehouses.GetValueOrDefault(s.WarehouseId, $"مخزن {s.WarehouseId}"),
                    Quantity = s.Quantity
                })
                .ToList();
        }
    }

    private async Task LoadSuggestedPricesAsync(InvoicePickerMode mode)
    {
        _suggestedPrices.Clear();
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

    private async Task LoadProductPricesAsync(InvoicePickerMode mode)
    {
        _pricesByProduct.Clear();
        if (_productPriceService is null)
            return;

        var productIds = _allProducts.Select(p => p.Id);
        var prices = await _productPriceService.GetByProductIdsAsync(productIds);
        foreach (var group in prices.GroupBy(p => p.ProductId))
        {
            var options = group.Select(p => new ProductPricingOption
            {
                PricingTypeId = p.PricingTypeId,
                Name = p.PricingType?.Name ?? $"نوع {p.PricingTypeId}",
                Price = mode == InvoicePickerMode.Purchase ? p.PurchasePrice : p.SalePrice,
                IsDefault = p.PricingType?.IsDefault == true
            }).ToList();
            _pricesByProduct[group.Key] = options;
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
            query = query.Where(p => ProductSearchHelper.Matches(p, term));
        }

        var ordered = query.OrderBy(p => p.Name).ToList();
        DisplayProducts.Clear();

        foreach (var product in ordered)
        {
            var catName = _categoryNames.GetValueOrDefault(product.CategoryId, "—");
            var item = new ProductPickerDisplayItem(product, catName)
            {
                Quantity = _quantities.GetValueOrDefault(product.Id),
                StockLabel = FormatStock(product.Id),
                PricingEnabled = _pricingEnabled,
                SearchTerm = term
            };

            if (_stocksByProduct.TryGetValue(product.Id, out var chips))
            {
                foreach (var chip in chips)
                    item.WarehouseStocks.Add(chip);
            }

            if (_pricesByProduct.TryGetValue(product.Id, out var options))
            {
                foreach (var option in options)
                    item.PricingOptions.Add(option);

                var selectedId = _selectedPricingTypeIds.GetValueOrDefault(product.Id);
                item.SelectedPricingOption = options.FirstOrDefault(o => o.PricingTypeId == selectedId)
                                            ?? options.FirstOrDefault();
            }

            item.PricingSelectionChanged += OnPricingSelectionChanged;
            DisplayProducts.Add(item);
        }
    }

    private void OnPricingSelectionChanged(ProductPickerDisplayItem item)
    {
        if (item.SelectedPricingOption is null)
            return;
        _selectedPricingTypeIds[item.ProductId] = item.SelectedPricingOption.PricingTypeId;
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
            return "رصيد المخزن: 0";

        return $"رصيد المخزن: {qty:N0}";
    }
}
