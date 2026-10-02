using System.Collections.ObjectModel;
using AlMuhasib.Core;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
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
    public AccountingCurrency Currency { get; init; } = AccountingCurrency.IQD;
    public bool IsLoss => LineProfit < 0;
    public bool IsProfit => LineProfit > 0;
    public bool IsBreakEven => LineProfit == 0;
    public string ProfitLabel => AccountingCurrencyHelper.Format(LineProfit, Currency);
    public string PurchasePriceLabel => PurchasePrice > 0
        ? AccountingCurrencyHelper.Format(PurchasePrice, Currency)
        : "—";
    public string SalePriceLabel => AccountingCurrencyHelper.Format(SalePrice, Currency);
    public string QuantityLabel => Quantity.ToString("N0");
}

public partial class InvoiceProfitCheckViewModel : ObservableObject
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductPriceService? _productPriceService;
    private readonly bool _pricingEnabled;
    private readonly bool _discountEnabled;
    private readonly AccountingCurrency _currency;
    private decimal _grossLineProfit;

    public ObservableCollection<InvoiceProfitLine> Lines { get; } = [];

    public IReadOnlyList<DiscountTypeOption> DiscountTypeOptions { get; }

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
    private string _totalProfitLabel = string.Empty;

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

    public string CurrencyLabel => AccountingCurrencyHelper.GetLabel(_currency);

    public bool Applied { get; private set; }

    public InvoiceProfitCheckViewModel(
        IUnitOfWork unitOfWork,
        IProductPriceService? productPriceService,
        bool pricingEnabled,
        bool discountEnabled,
        AccountingCurrency currency = AccountingCurrency.IQD)
    {
        _unitOfWork = unitOfWork;
        _productPriceService = productPriceService;
        _pricingEnabled = pricingEnabled;
        _discountEnabled = discountEnabled;
        _currency = currency;
        ShowDiscountControls = discountEnabled;
        DiscountTypeOptions =
        [
            new(DiscountType.None, "بدون خصم كلي"),
            new(DiscountType.Percentage, "نسبة مئوية (%)"),
            new(DiscountType.FixedAmount, $"قيمة ثابتة ({CurrencyLabel})")
        ];
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
        var pricingByProduct = rows
            .GroupBy(r => r.ProductId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(r => r.PricingTypeId).FirstOrDefault(id => id is > 0));
        var costs = await ResolveCostsAsync(productIds, pricingByProduct);

        Lines.Clear();
        foreach (var row in rows)
        {
            var cost = costs.GetValueOrDefault(row.ProductId!.Value);
            var lineDiscount = _discountEnabled ? row.DiscountAmount : 0m;
            var profit = AccountingCurrencyHelper.NormalizeAmount(
                (row.UnitPrice - cost) * row.Quantity - lineDiscount,
                _currency);
            Lines.Add(new InvoiceProfitLine
            {
                ItemName = row.ItemName,
                Quantity = row.Quantity,
                PurchasePrice = cost,
                SalePrice = row.UnitPrice,
                LineDiscount = lineDiscount,
                LineProfit = profit,
                Currency = _currency
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

        TotalProfit = AccountingCurrencyHelper.NormalizeAmount(
            _grossLineProfit - InvoiceDiscountAmount,
            _currency);
        TotalProfitLabel = AccountingCurrencyHelper.Format(TotalProfit, _currency);
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

    private async Task<Dictionary<int, decimal>> ResolveCostsAsync(
        IReadOnlyList<int> productIds,
        IReadOnlyDictionary<int, int?>? pricingTypeByProduct = null)
    {
        return await InvoiceDocumentCostResolver.ResolveProductCostsAsync(
            _unitOfWork,
            _productPriceService,
            _pricingEnabled,
            productIds,
            _currency,
            pricingTypeByProduct);
    }
}
