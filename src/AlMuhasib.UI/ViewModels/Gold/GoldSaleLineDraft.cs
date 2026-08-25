using AlMuhasib.Core.Enums.Gold;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels.Gold;

/// <summary>Editable draft line for gold sale/purchase invoices.</summary>
public partial class GoldSaleLineDraft : ObservableObject
{
    private readonly Action<GoldSaleLineDraft>? _onQuoteNeeded;
    private readonly Action<GoldSaleLineDraft>? _onItemLookupChanged;
    private bool _suppressItemLookupCallback;

    public GoldSaleLineDraft(
        Action<GoldSaleLineDraft>? onQuoteNeeded = null,
        Action<GoldSaleLineDraft>? onItemLookupChanged = null)
    {
        _onQuoteNeeded = onQuoteNeeded;
        _onItemLookupChanged = onItemLookupChanged;
    }

    [ObservableProperty] private int? _itemId;
    /// <summary>رقم القطعة أو الباركود — يُحلّ عند مغادرة الخلية.</summary>
    [ObservableProperty] private string _itemLookup = string.Empty;
    [ObservableProperty] private int _karatValue = 21;
    [ObservableProperty] private decimal _weightGrams;
    [ObservableProperty] private decimal _mithqalPrice;
    [ObservableProperty] private decimal _makingCharge;
    [ObservableProperty] private GoldMakingChargeMode _makingChargeMode = GoldMakingChargeMode.Fixed;
    [ObservableProperty] private decimal _makingChargeRate;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private bool _weightFromScale;
    /// <summary>When true, the weight cell is locked (e.g. after scale read with AllowManualWeightEdit=false).</summary>
    [ObservableProperty] private bool _isWeightReadOnly;
    [ObservableProperty] private decimal _goldValue;
    [ObservableProperty] private decimal _lineTotal;
    [ObservableProperty] private decimal? _lineTotalIqd;
    [ObservableProperty] private decimal? _lineTotalUsd;
    [ObservableProperty] private bool _isQuoting;

    partial void OnKaratValueChanged(int value) => RequestQuote();
    partial void OnWeightGramsChanged(decimal value) => RequestQuote();
    partial void OnMakingChargeChanged(decimal value) => RequestQuote();
    partial void OnMakingChargeModeChanged(GoldMakingChargeMode value) => RequestQuote();
    partial void OnMakingChargeRateChanged(decimal value) => RequestQuote();

    partial void OnItemLookupChanged(string value)
    {
        if (_suppressItemLookupCallback)
            return;
        _onItemLookupChanged?.Invoke(this);
    }

    public void SetItemLookupSilently(string value)
    {
        _suppressItemLookupCallback = true;
        ItemLookup = value;
        _suppressItemLookupCallback = false;
    }

    public void RequestQuote()
    {
        if (IsQuoting)
            return;
        _onQuoteNeeded?.Invoke(this);
    }

    public void ApplyQuote(Core.Models.Gold.GoldPricingQuote quote)
    {
        MithqalPrice = quote.MithqalPrice;
        GoldValue = quote.GoldValue;
        MakingCharge = quote.MakingCharge;
        MakingChargeMode = quote.MakingChargeMode;
        MakingChargeRate = quote.MakingChargeRate;
        LineTotal = quote.LineTotal;
        LineTotalIqd = quote.LineTotalIqd;
        LineTotalUsd = quote.LineTotalUsd;
        if (string.IsNullOrWhiteSpace(Description))
            Description = $"عيار {quote.KaratValue}";
    }
}

public sealed record GoldMakingChargeModeOption(GoldMakingChargeMode Value, string Label)
{
    public override string ToString() => Label;
}

public sealed record GoldCurrencyOption(GoldCurrency Value, string Label)
{
    public override string ToString() => Label;
}

public sealed record GoldPaymentMethodOption(GoldPaymentMethod Value, string Label)
{
    public override string ToString() => Label;
}

public sealed record GoldStatusFilterOption(GoldInvoiceStatus? Value, string Label)
{
    public override string ToString() => Label;
}

public sealed record GoldVoucherTypeFilterOption(GoldVoucherType? Value, string Label)
{
    public override string ToString() => Label;
}

public sealed record GoldVoucherTypeOption(GoldVoucherType Value, string Label)
{
    public override string ToString() => Label;
}
