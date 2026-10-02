using System.Collections.ObjectModel;
using System.Globalization;
using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlMuhasib.UI.ViewModels;

public partial class VouchersViewModel
{
    private readonly List<CashBox> _allCashBoxes = [];
    private bool _suppressAmountTextSync;
    private bool _suppressSettlementAutoPick;
    private CancellationTokenSource? _employeeBalanceCts;

    // ── Dialog ─────────────────────────────────────────────
    [ObservableProperty]
    private bool _isCreateDialogOpen;

    // ── Currency / FX ──────────────────────────────────────
    [ObservableProperty]
    private bool _showMultiCurrency;

    [ObservableProperty]
    private AccountingCurrency _selectedCurrency = AccountingCurrency.IQD;

    [ObservableProperty]
    private CurrencyOption? _selectedCurrencyOption;

    [ObservableProperty]
    private decimal _fxRate = 1m;

    [ObservableProperty]
    private bool _showFxRateInput;

    [ObservableProperty]
    private string _currencyAmountSuffix = AccountingCurrencyHelper.IqdLabel;

    public ObservableCollection<CurrencyOption> CurrencyOptions { get; } = new(CurrencyOption.All);
    public ObservableCollection<CashBox> FilteredCashBoxes { get; } = [];

    // ── Amount with thousand separators ────────────────────
    [ObservableProperty]
    private string _amountText = string.Empty;

    [ObservableProperty]
    private string _bankFeesText = string.Empty;

    // ── Dual party balances ────────────────────────────────
    [ObservableProperty]
    private decimal? _partyBalanceIqd;

    [ObservableProperty]
    private decimal? _partyBalanceUsd;

    [ObservableProperty]
    private bool _isPartyBalanceLoading;

    [ObservableProperty]
    private bool _showPartyBalancePanel;

    [ObservableProperty]
    private string _partyBalanceTitle = "الرصيد الحالي";

    [ObservableProperty]
    private AccountingCurrency _settlementCurrency = AccountingCurrency.IQD;

    [ObservableProperty]
    private CurrencyOption? _selectedSettlementCurrencyOption;

    [ObservableProperty]
    private bool _showSettlementCurrencyPicker;

    [ObservableProperty]
    private bool _isCrossCurrencySettlement;

    [ObservableProperty]
    private string _crossCurrencyHint = string.Empty;

    [ObservableProperty]
    private string _convertedAmountText = string.Empty;

    [ObservableProperty]
    private string _remainingBalanceText = string.Empty;

    [ObservableProperty]
    private string _currentBalanceLabel = "الرصيد الحالي";

    [ObservableProperty]
    private string _remainingBalanceLabel = "الرصيد المتبقي";

    [ObservableProperty]
    private bool _hasOutstandingSettlementBalance;

    public ObservableCollection<CurrencyOption> SettlementCurrencyOptions { get; } = new(CurrencyOption.All);

    private void InitializeCreateFormFeatures()
    {
        ShowMultiCurrency = _userPreferences.Current.FeatureFlags.MultiCurrency;
        SelectedCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
        SelectedSettlementCurrencyOption = SettlementCurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
        ApplyCashBoxCurrencyFilter();
        RefreshFxVisibility();
        RefreshBalancePreview();
    }

    [RelayCommand]
    private async Task OpenCreateDialogAsync()
    {
        ResetCreateFormFields(keepType: false);
        await GenerateVoucherNumberAsync();
        await RefreshFxRateAsync();
        IsCreateDialogOpen = true;
    }

    [RelayCommand]
    private void CloseCreateDialog()
    {
        IsCreateDialogOpen = false;
        ResetCreateFormFields(keepType: true);
    }

    private void ResetCreateFormFields(bool keepType)
    {
        Amount = 0;
        AmountText = string.Empty;
        BankFees = 0;
        BankFeesText = string.Empty;
        Notes = string.Empty;
        SelectedCustomer = null;
        CustomerSearchText = string.Empty;
        SelectedSupplier = null;
        SupplierSearchText = string.Empty;
        SelectedEmployee = null;
        EmployeeSearchText = string.Empty;
        SelectedInvestor = null;
        InvestorSearchText = string.Empty;
        CustomerComboBoxFilter.Apply(Customers, FilteredCustomers, null);
        SupplierComboBoxFilter.Apply(Suppliers, FilteredSuppliers, null);
        InvestorComboBoxFilter.Apply(Investors, FilteredInvestors, null);
        FilteredEmployees.Clear();
        foreach (var e in Employees)
            FilteredEmployees.Add(e);
        SelectedBankAccount = null;
        ClearDocumentLinks();
        ClearPartyBalances();
        VoucherDate = DateTime.Now;
        if (!keepType)
            SelectedVoucherType = VoucherTypes[0].Type;

        SelectedCurrency = AccountingCurrency.IQD;
        SelectedCurrencyOption = CurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
        SettlementCurrency = AccountingCurrency.IQD;
        SelectedSettlementCurrencyOption =
            SettlementCurrencyOptions.FirstOrDefault(c => c.Currency == AccountingCurrency.IQD);
        ApplyCashBoxCurrencyFilter();
        RefreshBalancePreview();
    }

    partial void OnAmountTextChanged(string value)
    {
        if (_suppressAmountTextSync)
            return;

        var parsed = ParseFormattedAmount(value);
        var formatted = parsed <= 0
            ? (string.IsNullOrWhiteSpace(value) ? string.Empty : value)
            : FormatAmountForCurrency(parsed, SelectedCurrency);

        _suppressAmountTextSync = true;
        Amount = parsed;
        if (!string.Equals(AmountText, formatted, StringComparison.Ordinal))
            AmountText = formatted;
        _suppressAmountTextSync = false;
        RefreshBalancePreview();
    }

    partial void OnBankFeesTextChanged(string value)
    {
        if (_suppressAmountTextSync)
            return;

        var parsed = ParseFormattedAmount(value);
        var formatted = parsed <= 0
            ? (string.IsNullOrWhiteSpace(value) ? string.Empty : value)
            : FormatAmountForCurrency(parsed, SelectedCurrency);

        _suppressAmountTextSync = true;
        BankFees = parsed;
        if (!string.Equals(BankFeesText, formatted, StringComparison.Ordinal))
            BankFeesText = formatted;
        _suppressAmountTextSync = false;
        UpdateNetAmountText();
    }

    partial void OnAmountChanged(decimal value)
    {
        if (!_suppressAmountTextSync)
            SyncAmountTextFormat();
        UpdateNetAmountText();
        RefreshBalancePreview();
    }

    partial void OnBankFeesChanged(decimal value)
    {
        if (!_suppressAmountTextSync)
            SyncBankFeesTextFormat();
        UpdateNetAmountText();
    }

    private void SyncAmountTextFormat()
    {
        _suppressAmountTextSync = true;
        AmountText = Amount <= 0
            ? string.Empty
            : FormatAmountForCurrency(Amount, SelectedCurrency);
        _suppressAmountTextSync = false;
    }

    private void SyncBankFeesTextFormat()
    {
        _suppressAmountTextSync = true;
        BankFeesText = BankFees <= 0
            ? string.Empty
            : FormatAmountForCurrency(BankFees, SelectedCurrency);
        _suppressAmountTextSync = false;
    }

    private static decimal ParseFormattedAmount(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var cleaned = text.Replace(",", "")
            .Replace("،", "")
            .Replace(" ", "")
            .Trim();

        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            || decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.CurrentCulture, out value)
            ? value
            : 0;
    }

    private static string FormatAmountForCurrency(decimal amount, AccountingCurrency currency) =>
        currency == AccountingCurrency.USD
            ? amount.ToString("N2", CultureInfo.CurrentCulture)
            : amount.ToString("N0", CultureInfo.CurrentCulture);

    partial void OnSelectedCurrencyOptionChanged(CurrencyOption? value)
    {
        if (value is null) return;
        SelectedCurrency = value.Currency;
        CurrencyAmountSuffix = AccountingCurrencyHelper.GetLabel(value.Currency);
        RefreshFxVisibility();
        ApplyCashBoxCurrencyFilter();
        SyncAmountTextFormat();
        SyncBankFeesTextFormat();
        AutoPickSettlementCurrency();
        _ = RefreshFxRateAsync();
        RefreshBalancePreview();
    }

    partial void OnSelectedSettlementCurrencyOptionChanged(CurrencyOption? value)
    {
        if (value is null || _suppressSettlementAutoPick) return;
        SettlementCurrency = value.Currency;
        RefreshBalancePreview();
    }

    partial void OnFxRateChanged(decimal value) => RefreshBalancePreview();

    partial void OnVoucherDateChanged(DateTime value) => _ = RefreshFxRateAsync();

    private void RefreshFxVisibility()
    {
        ShowFxRateInput = ShowMultiCurrency &&
                          (SelectedCurrency == AccountingCurrency.USD ||
                           SettlementCurrency != SelectedCurrency ||
                           IsCrossCurrencySettlement);
    }

    private void ApplyCashBoxCurrencyFilter()
    {
        var previousId = SelectedCashBox?.Id;
        FilteredCashBoxes.Clear();

        var filtered = ShowMultiCurrency
            ? _allCashBoxes.Where(c => c.Currency == SelectedCurrency)
            : _allCashBoxes.Where(c => c.Currency == AccountingCurrency.IQD);

        foreach (var box in filtered)
            FilteredCashBoxes.Add(box);

        if (FilteredCashBoxes.Count == 0)
        {
            SelectedCashBox = null;
            return;
        }

        SelectedCashBox = FilteredCashBoxes.FirstOrDefault(c => c.Id == previousId)
                          ?? FilteredCashBoxes[0];
    }

    private async Task RefreshFxRateAsync()
    {
        if (!ShowMultiCurrency ||
            (SelectedCurrency == AccountingCurrency.IQD && SettlementCurrency == AccountingCurrency.IQD))
        {
            if (SelectedCurrency == AccountingCurrency.IQD && SettlementCurrency == AccountingCurrency.IQD)
                FxRate = 1m;
            return;
        }

        try
        {
            var rate = await _exchangeRateService.GetUsdToIqdForDateOrLatestAsync(VoucherDate);
            if (rate > 0)
                FxRate = rate;
        }
        catch
        {
            // keep previous rate
        }
    }

    private void ClearPartyBalances()
    {
        _customerBalanceCts?.Cancel();
        _supplierBalanceCts?.Cancel();
        _employeeBalanceCts?.Cancel();
        PartyBalanceIqd = null;
        PartyBalanceUsd = null;
        IsPartyBalanceLoading = false;
        ShowPartyBalancePanel = false;
        HasOutstandingSettlementBalance = false;
        RemainingBalanceText = string.Empty;
        ConvertedAmountText = string.Empty;
        CrossCurrencyHint = string.Empty;
        IsCrossCurrencySettlement = false;
        ShowSettlementCurrencyPicker = false;
    }

    private void SetPartyBalances(decimal iqd, decimal usd, string title)
    {
        PartyBalanceIqd = iqd;
        PartyBalanceUsd = usd;
        PartyBalanceTitle = title;
        ShowPartyBalancePanel = true;
        IsPartyBalanceLoading = false;
        AutoPickSettlementCurrency();
        RefreshBalancePreview();
    }

    private void AutoPickSettlementCurrency()
    {
        if (!ShowPartyBalancePanel)
            return;

        var iqd = PartyBalanceIqd ?? 0;
        var usd = PartyBalanceUsd ?? 0;
        AccountingCurrency pick;

        if (SelectedCurrency == AccountingCurrency.IQD && iqd > 0)
            pick = AccountingCurrency.IQD;
        else if (SelectedCurrency == AccountingCurrency.USD && usd > 0)
            pick = AccountingCurrency.USD;
        else if (iqd > 0 && usd <= 0)
            pick = AccountingCurrency.IQD;
        else if (usd > 0 && iqd <= 0)
            pick = AccountingCurrency.USD;
        else if (iqd > 0)
            pick = AccountingCurrency.IQD;
        else if (usd > 0)
            pick = AccountingCurrency.USD;
        else
            pick = SelectedCurrency;

        _suppressSettlementAutoPick = true;
        SettlementCurrency = pick;
        SelectedSettlementCurrencyOption =
            SettlementCurrencyOptions.FirstOrDefault(c => c.Currency == pick);
        _suppressSettlementAutoPick = false;
    }

    private void RefreshBalancePreview()
    {
        IsCrossCurrencySettlement = ShowMultiCurrency &&
                                    ShowPartyBalancePanel &&
                                    SettlementCurrency != SelectedCurrency;

        ShowSettlementCurrencyPicker = ShowMultiCurrency && ShowPartyBalancePanel;

        RefreshFxVisibility();

        var current = SettlementCurrency == AccountingCurrency.USD
            ? PartyBalanceUsd ?? 0
            : PartyBalanceIqd ?? 0;

        // سند دفع لموظف = سلفة تُضاف إلى الرصيد (افتتاحي + دفع − قبض)
        // سند قبض من موظف = تسديد سلفة يُنقص الرصيد
        var isEmployeeAdvancePayment = ShowEmployeeField
                                       && SelectedEmployee is not null
                                       && SelectedVoucherType == VoucherType.Payment;

        HasOutstandingSettlementBalance = isEmployeeAdvancePayment || current > 0;
        CurrentBalanceLabel = SettlementCurrency == AccountingCurrency.USD
            ? "الرصيد الحالي ($)"
            : "الرصيد الحالي (د.ع)";
        RemainingBalanceLabel = isEmployeeAdvancePayment
            ? (SettlementCurrency == AccountingCurrency.USD
                ? "الرصيد بعد السند ($)"
                : "الرصيد بعد السند (د.ع)")
            : (SettlementCurrency == AccountingCurrency.USD
                ? "الرصيد المتبقي ($)"
                : "الرصيد المتبقي (د.ع)");

        decimal settleAmount = Amount;
        if (Amount > 0 && SettlementCurrency != SelectedCurrency)
        {
            if (FxRate <= 0)
            {
                ConvertedAmountText = "سعر الصرف مطلوب للتحويل";
                RemainingBalanceText = AccountingCurrencyHelper.Format(current, SettlementCurrency);
                CrossCurrencyHint = isEmployeeAdvancePayment
                    ? "سجّل سعر الصرف أولاً لإضافة السلفة بعملة مختلفة."
                    : "سجّل سعر الصرف أولاً لتسديد ذمة بعملة مختلفة.";
                return;
            }

            settleAmount = AccountingCurrencyHelper.Convert(
                Amount, SelectedCurrency, SettlementCurrency, FxRate);

            ConvertedAmountText =
                $"{FormatAmountForCurrency(Amount, SelectedCurrency)} {AccountingCurrencyHelper.GetLabel(SelectedCurrency)}" +
                $"  ←  {AccountingCurrencyHelper.Format(settleAmount, SettlementCurrency)}" +
                $"  @ {FxRate:N0}";
            CrossCurrencyHint = isEmployeeAdvancePayment
                ? $"سيتم تحويل المبلغ حسب سعر الصرف وإضافته إلى رصيد سلفة ال{(SettlementCurrency == AccountingCurrency.USD ? "دولار" : "دينار")}."
                : $"سيتم تحويل المبلغ حسب سعر الصرف وتسديد رصيد ال{(SettlementCurrency == AccountingCurrency.USD ? "دولار" : "دينار")}.";
        }
        else
        {
            ConvertedAmountText = string.Empty;
            CrossCurrencyHint = string.Empty;
            settleAmount = Amount;
        }

        var remaining = isEmployeeAdvancePayment
            ? current + settleAmount
            : Math.Max(0, current - settleAmount);
        RemainingBalanceText = ShowPartyBalancePanel
            ? AccountingCurrencyHelper.Format(remaining, SettlementCurrency)
            : string.Empty;
    }

    private async Task RefreshEmployeeBalanceAsync()
    {
        _employeeBalanceCts?.Cancel();
        _employeeBalanceCts?.Dispose();
        _employeeBalanceCts = new CancellationTokenSource();
        var token = _employeeBalanceCts.Token;

        if (SelectedEmployee is null)
        {
            ClearPartyBalances();
            return;
        }

        IsPartyBalanceLoading = true;
        ShowPartyBalancePanel = true;
        PartyBalanceTitle = "رصيد سلفة الموظف";

        try
        {
            var vouchers = await _unitOfWork.Vouchers.FindAsync(v =>
                v.EmployeeId == SelectedEmployee.Id &&
                (v.VoucherType == VoucherType.Payment ||
                 v.VoucherType == VoucherType.Receipt ||
                 v.VoucherType == VoucherType.DebtReceipt));

            if (token.IsCancellationRequested)
                return;

            var dual = EmployeeBalanceHelper.ComputeAdvanceBalances(
                SelectedEmployee.OpeningBalance,
                SelectedEmployee.OpeningBalanceCurrency,
                vouchers.Select(v => (v.Currency, v.VoucherType, v.Amount)));

            SetPartyBalances(dual.Iqd, dual.Usd, "رصيد سلفة الموظف");
        }
        catch
        {
            if (!token.IsCancellationRequested)
                ClearPartyBalances();
        }
    }
}
