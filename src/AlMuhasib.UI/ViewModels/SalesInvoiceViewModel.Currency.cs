using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class SalesInvoiceViewModel
{
    private IExchangeRateService? _exchangeRateService;
    private List<CashBox> _allCashBoxesForCurrency = [];
    private bool _suppressFxRateRefresh;

    [ObservableProperty] private bool _showMultiCurrency;
    [ObservableProperty] private bool _showFxRateInput;
    [ObservableProperty] private AccountingCurrency _selectedCurrency = AccountingCurrency.IQD;
    [ObservableProperty] private decimal _fxRate = 1m;
    [ObservableProperty] private string _currencyAmountSuffix = AccountingCurrencyHelper.IqdLabel;
    [ObservableProperty] private CurrencyOption? _selectedCurrencyOption;

    public string CreditPaidAmountHint => $"المبلغ المدفوع ({CurrencyAmountSuffix})";
    public string CreditRemainingAmountHint => $"المتبقي ({CurrencyAmountSuffix})";

    private AccountingCurrency DocumentRoundingCurrency =>
        ShowMultiCurrency ? SelectedCurrency : AccountingCurrency.IQD;

    public ObservableCollection<CurrencyOption> CurrencyOptions { get; } = new(CurrencyOption.All);

    public void ConfigureCurrencyServices(IExchangeRateService exchangeRateService)
    {
        _exchangeRateService = exchangeRateService;
    }

    private void RefreshMultiCurrencyFeatureVisibility()
    {
        ShowMultiCurrency = _featureFlags?.MultiCurrency == true;
        if (!ShowMultiCurrency)
        {
            SelectedCurrency = AccountingCurrency.IQD;
            FxRate = 1m;
            SelectedCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
        }
        else if (SelectedCurrencyOption is null)
        {
            SelectedCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
        }

        CurrencyAmountSuffix = AccountingCurrencyHelper.GetLabel(SelectedCurrency);
        ShowFxRateInput = ShowMultiCurrency && SelectedCurrency == AccountingCurrency.USD;
        NotifyCurrencyHintsChanged();
        OnPropertyChanged(nameof(CustomerBalanceText));
        OnPropertyChanged(nameof(HasCustomerOutstandingBalance));
        SyncDocumentCurrencyToHelpers();
        ApplyCashBoxCurrencyFilter();
        if (SelectedCustomer is not null)
            _ = RefreshCustomerBalanceAsync();
    }

    partial void OnSelectedCurrencyOptionChanged(CurrencyOption? value)
    {
        if (value is null) return;
        SelectedCurrency = value.Currency;
        CurrencyAmountSuffix = AccountingCurrencyHelper.GetLabel(SelectedCurrency);
        ShowFxRateInput = ShowMultiCurrency && SelectedCurrency == AccountingCurrency.USD;
        NotifyCurrencyHintsChanged();
        SyncDocumentCurrencyToHelpers();
        ApplyCashBoxCurrencyFilter();
        if (!_suppressFxRateRefresh)
            _ = OnDocumentCurrencyChangedAsync();
    }

    partial void OnSelectedCurrencyChanged(AccountingCurrency value)
    {
        CurrencyAmountSuffix = AccountingCurrencyHelper.GetLabel(value);
        ShowFxRateInput = ShowMultiCurrency && value == AccountingCurrency.USD;
        NotifyCurrencyHintsChanged();
        SyncDocumentCurrencyToHelpers();
        RecalculateTotals();
        if (SelectedCurrencyOption?.Currency != value)
            SelectedCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == value);
    }

    partial void OnCurrencyAmountSuffixChanged(string value) => NotifyCurrencyHintsChanged();

    private void NotifyCurrencyHintsChanged()
    {
        OnPropertyChanged(nameof(CreditPaidAmountHint));
        OnPropertyChanged(nameof(CreditRemainingAmountHint));
        OnPropertyChanged(nameof(InvoiceDiscountValueHint));
    }

    private void SyncDocumentCurrencyToHelpers()
    {
        ProductPicker.DocumentCurrency = SelectedCurrency;
        QuickSearchCatalog.DocumentCurrency = SelectedCurrency;
    }

    private async Task OnDocumentCurrencyChangedAsync()
    {
        try
        {
            await RefreshFxRateForSelectedCurrencyAsync();
            await RequoteLinesForCurrencyAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SalesCurrency] {ex.Message}");
        }
    }

    private async Task RefreshFxRateForSelectedCurrencyAsync()
    {
        if (SelectedCurrency == AccountingCurrency.IQD)
        {
            FxRate = 1m;
            return;
        }

        if (_exchangeRateService is null)
            return;

        try
        {
            FxRate = await _exchangeRateService.GetUsdToIqdForDateOrLatestAsync(InvoiceDate);
            if (FxRate <= 0)
                FxRate = 0m;
        }
        catch
        {
            FxRate = 0m;
        }
    }

    private async Task RequoteLinesForCurrencyAsync()
    {
        if (!ShowMultiCurrency || _productPriceService is null)
            return;

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
                var options = InvoiceBulkPricingHelper.ToOptions(prices, usePurchasePrice: false, SelectedCurrency);
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
                row.UnitPrice = ProductListPriceHelper.ResolveListPrice(preferred, SelectedCurrency, isPurchase: false);
            }
            else
            {
                row.UnitPrice = 0m;
            }
        }

        RecalculateTotals();
    }

    private void ApplyCurrencyFromDocument(AccountingCurrency currency, decimal fxRate)
    {
        _suppressFxRateRefresh = true;
        try
        {
            SelectedCurrency = currency;
            SelectedCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == currency);
            FxRate = currency == AccountingCurrency.IQD
                ? 1m
                : (fxRate > 0 ? fxRate : 0m);
            CurrencyAmountSuffix = AccountingCurrencyHelper.GetLabel(currency);
            ShowFxRateInput = ShowMultiCurrency && currency == AccountingCurrency.USD;
            NotifyCurrencyHintsChanged();
            SyncDocumentCurrencyToHelpers();
            ApplyCashBoxCurrencyFilter();
        }
        finally
        {
            _suppressFxRateRefresh = false;
        }
    }

    private void RememberCashBoxesForCurrencyFilter(IEnumerable<CashBox> boxes)
    {
        _allCashBoxesForCurrency = boxes.ToList();
        ApplyCashBoxCurrencyFilter();
    }

    private void ApplyCashBoxCurrencyFilter()
    {
        if (_allCashBoxesForCurrency.Count == 0)
            return;

        var selectedId = SelectedCashBox?.Id;
        var filtered = ShowMultiCurrency
            ? _allCashBoxesForCurrency.Where(c => c.Currency == SelectedCurrency).ToList()
            : _allCashBoxesForCurrency;

        CashBoxes.Clear();
        foreach (var box in filtered)
            CashBoxes.Add(box);

        SelectedCashBox = selectedId.HasValue
            ? CashBoxes.FirstOrDefault(c => c.Id == selectedId.Value)
            : CashBoxes.FirstOrDefault();
    }
}
