using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Services;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Services;

/// <summary>كتالوج بحث سريع للمنتجات داخل خلايا الفاتورة: أرصدة المخازن + الأسعار.</summary>
public sealed class ProductQuickSearchCatalog
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductPriceService? _productPriceService;
    private readonly Dictionary<int, List<WarehouseStockChip>> _stocksByProduct = new();
    private readonly Dictionary<int, decimal> _suggestedPrices = new();
    private readonly Dictionary<int, decimal> _catalogPrices = new();
    private List<Product> _products = [];
    private bool _pricingEnabled;
    private InvoicePickerMode _mode;

    public ProductQuickSearchCatalog(IUnitOfWork unitOfWork, IProductPriceService? productPriceService = null)
    {
        _unitOfWork = unitOfWork;
        _productPriceService = productPriceService;
    }

    public async Task LoadAsync(
        IEnumerable<Product> products,
        InvoicePickerMode mode,
        bool pricingEnabled)
    {
        _products = products.ToList();
        _mode = mode;
        _pricingEnabled = pricingEnabled;
        await LoadStockAsync();
        await LoadSuggestedPricesAsync();
        if (_pricingEnabled && _productPriceService is not null)
            await LoadCatalogPricesAsync();
        else
            _catalogPrices.Clear();
    }

    public IReadOnlyList<ProductSearchSuggestion> Search(string? searchText, int maxResults = 40)
    {
        var term = searchText?.Trim() ?? string.Empty;
        IEnumerable<Product> query = _products;
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(p => ProductSearchHelper.Matches(p, term));

        var results = new List<ProductSearchSuggestion>();
        foreach (var product in query.OrderBy(p => p.Name).Take(maxResults))
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

    private decimal ResolvePrice(int productId)
    {
        if (_pricingEnabled && _catalogPrices.TryGetValue(productId, out var catalog) && catalog > 0)
            return catalog;
        return _suggestedPrices.GetValueOrDefault(productId);
    }

    private async Task LoadStockAsync()
    {
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
    }

    private async Task LoadSuggestedPricesAsync()
    {
        _suggestedPrices.Clear();
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

            foreach (var product in _products)
            {
                var lastPurchase = purchaseItems
                    .Where(i => i.ProductId == product.Id && i.UnitPrice > 0)
                    .OrderByDescending(i => i.Id)
                    .FirstOrDefault();

                if (lastPurchase is not null)
                {
                    _suggestedPrices[product.Id] = lastPurchase.UnitPrice;
                    continue;
                }

                _suggestedPrices[product.Id] = Math.Round(
                    ProductCostHelper.ComputeAverageUnitCostForProduct(purchaseItems, stocks, product.Id), 0);
            }

            return;
        }

        var saleInvoiceIds = (await _unitOfWork.Invoices.FindAsync(i =>
                i.InvoiceType == InvoiceType.Sale || i.InvoiceType == InvoiceType.Installment))
            .Select(i => i.Id)
            .ToHashSet();
        var saleItems = invoiceItems
            .Where(i => i.ProductId is not null && saleInvoiceIds.Contains(i.InvoiceId))
            .ToList();

        foreach (var product in _products)
        {
            var lastSale = saleItems
                .Where(i => i.ProductId == product.Id && i.UnitPrice > 0)
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();
            _suggestedPrices[product.Id] = lastSale?.UnitPrice ?? 0;
        }
    }

    private async Task LoadCatalogPricesAsync()
    {
        _catalogPrices.Clear();
        if (_productPriceService is null || _products.Count == 0)
            return;

        var prices = await _productPriceService.GetByProductIdsAsync(_products.Select(p => p.Id));
        foreach (var group in prices.GroupBy(p => p.ProductId))
        {
            var preferred = group.FirstOrDefault(p => p.PricingType?.IsDefault == true) ?? group.First();
            var price = _mode == InvoicePickerMode.Purchase ? preferred.PurchasePrice : preferred.SalePrice;
            _catalogPrices[group.Key] = price;
        }
    }
}
