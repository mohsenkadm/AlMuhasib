using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.UI.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class InstallmentInvoiceViewModel
{
    [ObservableProperty] private bool _showMultiCurrency;
    [ObservableProperty] private string _currencyAmountSuffix = AccountingCurrencyHelper.IqdLabel;

    public string InvoiceDiscountValueHint => InvoiceDiscountType switch
    {
        DiscountType.Percentage => "نسبة الخصم (%)",
        DiscountType.FixedAmount => $"مبلغ الخصم ({CurrencyAmountSuffix})",
        _ => "قيمة الخصم الكلي"
    };

    private AccountingCurrency DocumentCurrency =>
        ShowMultiCurrency
            ? (SelectedCashBox?.Currency ?? AccountingCurrency.IQD)
            : AccountingCurrency.IQD;

    private void RefreshMultiCurrencyFeatureVisibility()
    {
        ShowMultiCurrency = _featureFlags.MultiCurrency;
        RefreshCurrencyAmountSuffix();
        SyncDocumentCurrencyToHelpers();
    }

    private void RefreshCurrencyAmountSuffix()
    {
        CurrencyAmountSuffix = AccountingCurrencyHelper.GetLabel(DocumentCurrency);
        OnPropertyChanged(nameof(InvoiceDiscountValueHint));
    }

    partial void OnSelectedCashBoxChanged(CashBox? value)
    {
        if (!ShowMultiCurrency)
            return;

        var previous = CurrencyAmountSuffix;
        RefreshCurrencyAmountSuffix();
        SyncDocumentCurrencyToHelpers();
        if (!string.Equals(previous, CurrencyAmountSuffix, StringComparison.Ordinal))
            _ = RequoteLinesForCurrencyAsync();
    }

    private void SyncDocumentCurrencyToHelpers()
    {
        ProductPicker.DocumentCurrency = DocumentCurrency;
        QuickSearchCatalog.DocumentCurrency = DocumentCurrency;
    }

    private async Task RequoteLinesForCurrencyAsync()
    {
        if (!ShowMultiCurrency || _productPriceService is null)
            return;

        var currency = DocumentCurrency;
        foreach (var row in Items.ToList())
        {
            if (row.ProductId is not int productId || productId <= 0)
                continue;

            IReadOnlyList<ProductPrice> prices;
            try
            {
                prices = await _productPriceService.GetByProductIdAsync(productId);
            }
            catch
            {
                continue;
            }

            if (ShowProductPricing)
            {
                var options = InvoiceBulkPricingHelper.ToOptions(prices, usePurchasePrice: false, currency);
                row.AvailablePricingOptions.Clear();
                foreach (var option in options)
                    row.AvailablePricingOptions.Add(option);

                var preferred = InvoiceBulkPricingHelper.ResolvePreferredOption(
                    options,
                    row.PricingTypeId,
                    SelectedBulkPricingType?.Id);

                if (preferred is not null)
                {
                    row.SelectedPricingOption = preferred;
                    continue;
                }

                row.UnitPrice = 0m;
                row.SetSelectedPricingOptionWithoutPrice(null);
                row.PricingTypeId = null;
                row.PricingTypeName = string.Empty;
                continue;
            }

            if (prices.Count > 0)
            {
                var preferred = prices.FirstOrDefault(p => p.PricingType?.IsDefault == true) ?? prices[0];
                row.UnitPrice = ProductListPriceHelper.ResolveListPrice(preferred, currency, isPurchase: false);
            }
            else
            {
                row.UnitPrice = 0m;
            }
        }

        RecalculateTotals();
    }
}
