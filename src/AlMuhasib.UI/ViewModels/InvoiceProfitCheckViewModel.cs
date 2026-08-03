using System.Collections.ObjectModel;
using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class InvoiceProfitLine : ObservableObject
{
    public required string ItemName { get; init; }
    public decimal Quantity { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal SalePrice { get; init; }
    public decimal LineDiscount { get; init; }
    public decimal LineProfit { get; init; }
    public bool IsLoss => LineProfit < 0;
    public bool IsProfit => LineProfit > 0;
    public bool IsBreakEven => LineProfit == 0;
    public string ProfitLabel => $"{LineProfit:N0} د.ع";
    public string PurchasePriceLabel => PurchasePrice > 0 ? $"{PurchasePrice:N0}" : "—";
    public string SalePriceLabel => $"{SalePrice:N0}";
    public string QuantityLabel => Quantity.ToString("N0");
}

public partial class InvoiceProfitCheckViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductPriceService? _productPriceService;
    private readonly bool _pricingEnabled;
    private readonly bool _discountEnabled;
    private decimal _grossLineProfit;

    public ObservableCollection<InvoiceProfitLine> Lines { get; } = [];

    public IReadOnlyList<DiscountTypeOption> DiscountTypeOptions { get; } =
    [
        new(DiscountType.None, "بدون خصم كلي"),
        new(DiscountType.Percentage, "نسبة مئوية (%)"),
        new(DiscountType.FixedAmount, "قيمة ثابتة (د.ع)")
    ];

    [ObservableProperty]
    private DiscountTypeOption? _selectedDiscountOption;

    [ObservableProperty]
    private DiscountType _discountType = DiscountType.None;

    [ObservableProperty]
    private decimal _discountValue;

    [ObservableProperty]
    private decimal _invoiceDiscountAmount;

    [ObservableProperty]
    private decimal _totalProfit;

    [ObservableProperty]
    private int _lossCount;

    [ObservableProperty]
    private int _profitCount;

    [ObservableProperty]
    private bool _showDiscountControls;

    [ObservableProperty]
    private string _summaryLabel = string.Empty;

    [ObservableProperty]
    private bool _isOverallLoss;

    [ObservableProperty]
    private bool _isOverallProfit;

    public bool Applied { get; private set; }

    public InvoiceProfitCheckViewModel(
        IUnitOfWork unitOfWork,
        IProductPriceService? productPriceService,
        bool pricingEnabled,
        bool discountEnabled)
    {
        _unitOfWork = unitOfWork;
        _productPriceService = productPriceService;
        _pricingEnabled = pricingEnabled;
        _discountEnabled = discountEnabled;
        ShowDiscountControls = discountEnabled;
        SelectedDiscountOption = DiscountTypeOptions[0];
    }

    public async Task LoadAsync(
        IEnumerable<InvoiceItemRow> items,
        DiscountType currentDiscountType,
        decimal currentDiscountValue)
    {
        var rows = items
            .Where(i => i.ProductId is > 0 && !string.IsNullOrWhiteSpace(i.ItemName) && i.Quantity != 0)
            .ToList();

        DiscountType = currentDiscountType;
        DiscountValue = currentDiscountValue;
        SelectedDiscountOption = DiscountTypeOptions.FirstOrDefault(o => o.Type == currentDiscountType)
                                 ?? DiscountTypeOptions[0];

        var productIds = rows.Select(r => r.ProductId!.Value).Distinct().ToList();
        var costs = await ResolveCostsAsync(productIds);

        Lines.Clear();
        foreach (var row in rows)
        {
            var cost = costs.GetValueOrDefault(row.ProductId!.Value);
            var lineDiscount = _discountEnabled ? row.DiscountAmount : 0m;
            var profit = Math.Round((row.UnitPrice - cost) * row.Quantity - lineDiscount, 0);
            Lines.Add(new InvoiceProfitLine
            {
                ItemName = row.ItemName,
                Quantity = row.Quantity,
                PurchasePrice = cost,
                SalePrice = row.UnitPrice,
                LineDiscount = lineDiscount,
                LineProfit = profit
            });
        }

        _grossLineProfit = Lines.Sum(l => l.LineProfit);
        RecalculateTotals();
    }

    partial void OnSelectedDiscountOptionChanged(DiscountTypeOption? value)
    {
        if (value is not null && DiscountType != value.Type)
            DiscountType = value.Type;
    }

    partial void OnDiscountTypeChanged(DiscountType value)
    {
        var match = DiscountTypeOptions.FirstOrDefault(o => o.Type == value);
        if (!Equals(SelectedDiscountOption, match))
            SelectedDiscountOption = match;
        RecalculateTotals();
    }

    partial void OnDiscountValueChanged(decimal value) => RecalculateTotals();

    private void RecalculateTotals()
    {
        var subtotal = Lines.Sum(l => l.SalePrice * l.Quantity);
        InvoiceDiscountAmount = _discountEnabled
            ? ProductDiscountHelper.CalculateInvoiceDiscount(DiscountType, DiscountValue, subtotal)
            : 0m;

        TotalProfit = Math.Round(_grossLineProfit - InvoiceDiscountAmount, 0);
        LossCount = Lines.Count(l => l.IsLoss);
        ProfitCount = Lines.Count(l => l.IsProfit);
        IsOverallLoss = TotalProfit < 0;
        IsOverallProfit = TotalProfit > 0;
        SummaryLabel = LossCount > 0
            ? $"يوجد {LossCount} مادة بخسارة — راجع الأسعار قبل الحفظ"
            : "جميع المواد رابحة أو متعادلة";
    }

    [RelayCommand]
    private void ApplyDiscount()
    {
        Applied = true;
    }

    private async Task<Dictionary<int, decimal>> ResolveCostsAsync(IReadOnlyList<int> productIds)
    {
        var result = new Dictionary<int, decimal>();
        if (productIds.Count == 0)
            return result;

        var stocks = (await _unitOfWork.WarehouseStocks.FindAsync(s => productIds.Contains(s.ProductId))).ToList();
        var allItems = (await _unitOfWork.InvoiceItems.FindAsync(i =>
            i.ProductId != null && productIds.Contains(i.ProductId.Value))).ToList();
        var purchaseInvoiceIds = (await _unitOfWork.Invoices.FindAsync(i => i.InvoiceType == InvoiceType.Purchase))
            .Select(i => i.Id)
            .ToHashSet();
        var purchaseItems = allItems
            .Where(i => i.ProductId is not null && purchaseInvoiceIds.Contains(i.InvoiceId))
            .ToList();

        Dictionary<int, decimal>? catalogPurchase = null;
        if (_pricingEnabled && _productPriceService is not null)
        {
            var prices = await _productPriceService.GetByProductIdsAsync(productIds);
            catalogPurchase = prices
                .GroupBy(p => p.ProductId)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var preferred = g.FirstOrDefault(p => p.PricingType?.IsDefault == true) ?? g.First();
                        return preferred.PurchasePrice;
                    });
        }

        foreach (var productId in productIds)
        {
            if (catalogPurchase is not null
                && catalogPurchase.TryGetValue(productId, out var catalogCost)
                && catalogCost > 0)
            {
                result[productId] = catalogCost;
                continue;
            }

            var lastPurchase = purchaseItems
                .Where(i => i.ProductId == productId && i.UnitPrice > 0)
                .OrderByDescending(i => i.Id)
                .FirstOrDefault();
            if (lastPurchase is not null)
            {
                result[productId] = lastPurchase.UnitPrice;
                continue;
            }

            result[productId] = Math.Round(
                ProductCostHelper.ComputeAverageUnitCostForProduct(purchaseItems, stocks, productId), 0);
        }

        return result;
    }
}
