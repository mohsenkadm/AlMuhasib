using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Services;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Services;

/// <summary>
/// كتالوج بحث سريع للمنتجات داخل خلايا الفاتورة.
/// يحمّل قائمة المنتجات فقط عند البدء، ويجلب الأرصدة والأسعار للنتائج المعروضة فقط.
/// </summary>
public sealed class ProductQuickSearchCatalog
{
    public const int DefaultPreviewCount = 25;
    public const int DefaultSearchCount = 40;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductPriceService? _productPriceService;
    private readonly Dictionary<int, List<WarehouseStockChip>> _stocksByProduct = new();
    private readonly Dictionary<int, decimal> _suggestedPrices = new();
    private readonly Dictionary<int, decimal> _catalogPrices = new();
    private List<Product> _products = [];
    private bool _pricingEnabled;
    private InvoicePickerMode _mode;
    private bool _stockLoaded;
    private readonly SemaphoreSlim _stockLock = new(1, 1);

    public ProductQuickSearchCatalog(IUnitOfWork unitOfWork, IProductPriceService? productPriceService = null)
    {
        _unitOfWork = unitOfWork;
        _productPriceService = productPriceService;
    }

    public Task LoadAsync(
        IEnumerable<Product> products,
        InvoicePickerMode mode,
        bool pricingEnabled)
    {
        _products = ProductSearchHelper.ActiveOnly(products).OrderBy(p => p.Name).ToList();
        _mode = mode;
        _pricingEnabled = pricingEnabled;
        _stocksByProduct.Clear();
        _suggestedPrices.Clear();
        _catalogPrices.Clear();
        _stockLoaded = false;
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<ProductSearchSuggestion>> SearchAsync(
        string? searchText,
        int maxResults = DefaultSearchCount)
    {
        var term = searchText?.Trim() ?? string.Empty;
        IEnumerable<Product> query = _products;

        if (string.IsNullOrWhiteSpace(term))
            query = query.Take(Math.Min(maxResults, DefaultPreviewCount));
        else
            query = query.Where(p => ProductSearchHelper.Matches(p, term)).Take(maxResults);

        var matched = query.OrderBy(p => p.Name).ToList();
        if (matched.Count == 0)
            return [];

        await EnsureStockLoadedAsync();
        await EnsureSuggestedPricesLoadedAsync(matched.Select(p => p.Id).ToList());

        var results = new List<ProductSearchSuggestion>(matched.Count);
        foreach (var product in matched)
        {
            var suggestion = new ProductSearchSuggestion
            {
                Product = product,
                SearchTerm = term
            };

            if (_stocksByProduct.TryGetValue(product.Id, out var chips))
            {
                foreach (var chip in chips)
                    suggestion.WarehouseStocks.Add(chip);
            }

            var price = ResolvePrice(product.Id);
            suggestion.Price = price;
            suggestion.HasPrice = price > 0;
            suggestion.PriceLabel = price > 0
                ? $"السعر: {price:N0}"
                : (_pricingEnabled ? "بدون سعر" : "لا يوجد سعر سابق");

            results.Add(suggestion);
        }

        return results;
    }

    /// <summary>سعر مقترح للتعبئة التلقائية عند اختيار المنتج.</summary>
    public bool TryGetSuggestedPrice(int productId, out decimal price)
    {
        price = ResolvePrice(productId);
        return price > 0;
    }

    public async Task EnsurePriceLoadedAsync(int productId)
    {
        if (_suggestedPrices.ContainsKey(productId)
            && (!_pricingEnabled || _catalogPrices.ContainsKey(productId) || _productPriceService is null))
            return;

        await EnsureSuggestedPricesLoadedAsync([productId]);
    }

    private decimal ResolvePrice(int productId)
    {
        if (_pricingEnabled && _catalogPrices.TryGetValue(productId, out var catalog) && catalog > 0)
            return catalog;
        return _suggestedPrices.GetValueOrDefault(productId);
    }

    private async Task EnsureStockLoadedAsync()
    {
        if (_stockLoaded)
            return;

        await _stockLock.WaitAsync();
        try
        {
            if (_stockLoaded)
                return;

            _stocksByProduct.Clear();
            var warehouses = (await _unitOfWork.Warehouses.GetAllAsync())
                .ToDictionary(w => w.Id, w => w.Name);
            var stocks = await _unitOfWork.WarehouseStocks.FindAsync(_ => true);

            foreach (var group in stocks.GroupBy(s => s.ProductId))
            {
                var chips = group
                    .Where(s => warehouses.ContainsKey(s.WarehouseId))
                    .OrderBy(s => warehouses[s.WarehouseId])
                    .Select(s => new WarehouseStockChip
                    {
                        WarehouseName = warehouses[s.WarehouseId],
                        Quantity = s.Quantity
                    })
                    .ToList();
                _stocksByProduct[group.Key] = chips;
            }

            _stockLoaded = true;
        }
        finally
        {
            _stockLock.Release();
        }
    }

    private async Task EnsureSuggestedPricesLoadedAsync(IReadOnlyCollection<int> productIds)
    {
        var missing = productIds.Where(id => !_suggestedPrices.ContainsKey(id)).ToList();
        if (missing.Count == 0)
        {
            if (_pricingEnabled && _productPriceService is not null)
                await EnsureCatalogPricesLoadedAsync(productIds);
            return;
        }

        var invoiceItems = (await _unitOfWork.InvoiceItems.FindAsync(i => i.ProductId != null)).ToList();

        if (_mode == InvoicePickerMode.Purchase)
        {
            var stocks = await _unitOfWork.WarehouseStocks.FindAsync(_ => true);
            var purchaseInvoiceIds = (await _unitOfWork.Invoices.FindAsync(i => i.InvoiceType == InvoiceType.Purchase))
                .Select(i => i.Id)
                .ToHashSet();
            var purchaseItems = invoiceItems
                .Where(i => i.ProductId is not null && purchaseInvoiceIds.Contains(i.InvoiceId))
                .ToList();

            foreach (var productId in missing)
            {
                var lastPurchase = purchaseItems
                    .Where(i => i.ProductId == productId && i.UnitPrice > 0)
                    .OrderByDescending(i => i.Id)
                    .FirstOrDefault();

                _suggestedPrices[productId] = lastPurchase?.UnitPrice ?? Math.Round(
                    ProductCostHelper.ComputeAverageUnitCostForProduct(purchaseItems, stocks, productId), 0);
            }
        }
        else
        {
            var saleInvoiceIds = (await _unitOfWork.Invoices.FindAsync(i =>
                    i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment))
                .Select(i => i.Id)
                .ToHashSet();
            var saleItems = invoiceItems
                .Where(i => i.ProductId is not null && saleInvoiceIds.Contains(i.InvoiceId))
                .ToList();

            foreach (var productId in missing)
            {
                var lastSale = saleItems
                    .Where(i => i.ProductId == productId && i.UnitPrice > 0)
                    .OrderByDescending(i => i.Id)
                    .FirstOrDefault();
                _suggestedPrices[productId] = lastSale?.UnitPrice ?? 0;
            }
        }

        if (_pricingEnabled && _productPriceService is not null)
            await EnsureCatalogPricesLoadedAsync(productIds);
    }

    private async Task EnsureCatalogPricesLoadedAsync(IReadOnlyCollection<int> productIds)
    {
        var missing = productIds.Where(id => !_catalogPrices.ContainsKey(id)).ToList();
        if (missing.Count == 0 || _productPriceService is null)
            return;

        var prices = await _productPriceService.GetByProductIdsAsync(missing);
        foreach (var group in prices.GroupBy(p => p.ProductId))
        {
            var preferred = group.FirstOrDefault(p => p.PricingType?.IsDefault == true) ?? group.First();
            var price = _mode == InvoicePickerMode.Purchase ? preferred.PurchasePrice : preferred.SalePrice;
            _catalogPrices[group.Key] = price;
        }

        foreach (var productId in missing.Where(id => !_catalogPrices.ContainsKey(id)))
            _catalogPrices[productId] = 0;
    }
}
