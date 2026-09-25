using System.Collections.ObjectModel;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.ViewModels;

public partial class PurchaseInvoiceViewModel
{
    private IExchangeRateService? _exchangeRateService;
    private List<CashBox> _allCashBoxesForCurrency = [];

    [ObservableProperty] private bool _showMultiCurrency;
    [ObservableProperty] private bool _showFxRateInput;
    [ObservableProperty] private AccountingCurrency _selectedCurrency = AccountingCurrency.IQD;
    [ObservableProperty] private decimal _fxRate = 1m;
    [ObservableProperty] private CurrencyOption? _selectedCurrencyOption;

    public ObservableCollection<CurrencyOption> CurrencyOptions { get; } = new(CurrencyOption.All);

    public void ConfigureCurrencyServices(IExchangeRateService exchangeRateService) =>
        _exchangeRateService = exchangeRateService;

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

        ShowFxRateInput = ShowMultiCurrency && SelectedCurrency == AccountingCurrency.USD;
        ApplyCashBoxCurrencyFilter();
    }

    partial void OnSelectedCurrencyOptionChanged(CurrencyOption? value)
    {
        if (value is null) return;
        SelectedCurrency = value.Currency;
        ShowFxRateInput = ShowMultiCurrency && SelectedCurrency == AccountingCurrency.USD;
        _ = RefreshFxRateForSelectedCurrencyAsync();
        ApplyCashBoxCurrencyFilter();
    }

    private async Task RefreshFxRateForSelectedCurrencyAsync()
    {
        if (SelectedCurrency == AccountingCurrency.IQD)
        {
            FxRate = 1m;
            return;
        }

        if (_exchangeRateService is null) return;
        try
        {
            FxRate = await _exchangeRateService.GetUsdToIqdForDateOrLatestAsync(InvoiceDate);
            if (FxRate <= 0) FxRate = 0m;
        }
        catch
        {
            FxRate = 0m;
        }
    }

    private void ApplyCurrencyFromDocument(AccountingCurrency currency, decimal fxRate)
    {
        SelectedCurrency = currency;
        SelectedCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == currency);
        FxRate = currency == AccountingCurrency.IQD
            ? 1m
            : (fxRate > 0 ? fxRate : 0m);
        ShowFxRateInput = ShowMultiCurrency && currency == AccountingCurrency.USD;
        ApplyCashBoxCurrencyFilter();
    }

    private void RememberCashBoxesForCurrencyFilter(IEnumerable<CashBox> boxes)
    {
        _allCashBoxesForCurrency = boxes.ToList();
        ApplyCashBoxCurrencyFilter();
    }

    private void ApplyCashBoxCurrencyFilter()
    {
        if (_allCashBoxesForCurrency.Count == 0) return;
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
