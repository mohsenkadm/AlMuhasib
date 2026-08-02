using AlMuhasib.Core.Enums.Gold;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels.Gold;

/// <summary>Editable draft line for gold sale/purchase invoices.</summary>
public partial class GoldSaleLineDraft : ObservableObject
{
    private readonly Action<GoldSaleLineDraft>? _onQuoteNeeded;

    public GoldSaleLineDraft(Action<GoldSaleLineDraft>? onQuoteNeeded = null)
    {
        _onQuoteNeeded = onQuoteNeeded;
    }

    [ObservableProperty] private int? _itemId;
    [ObservableProperty] private int _karatValue = 21;
    [ObservableProperty] private decimal _weightGrams;
    [ObservableProperty] private decimal _mithqalPrice;
    [ObservableProperty] private decimal _makingCharge;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private bool _weightFromScale;
    [ObservableProperty] private decimal _goldValue;
    [ObservableProperty] private decimal _lineTotal;
    [ObservableProperty] private decimal? _lineTotalIqd;
    [ObservableProperty] private decimal? _lineTotalUsd;
    [ObservableProperty] private bool _isQuoting;

    partial void OnKaratValueChanged(int value) => RequestQuote();
    partial void OnWeightGramsChanged(decimal value) => RequestQuote();
    partial void OnMakingChargeChanged(decimal value) => RequestQuote();

    public void RequestQuote() => _onQuoteNeeded?.Invoke(this);

    public void ApplyQuote(Core.Models.Gold.GoldPricingQuote quote)
    {
        MithqalPrice = quote.MithqalPrice;
        GoldValue = quote.GoldValue;
        LineTotal = quote.LineTotal;
        LineTotalIqd = quote.LineTotalIqd;
        LineTotalUsd = quote.LineTotalUsd;
        if (string.IsNullOrWhiteSpace(Description))
            Description = $"عيار {quote.KaratValue}";
    }
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
