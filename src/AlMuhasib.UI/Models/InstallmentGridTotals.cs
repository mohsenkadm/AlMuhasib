using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlMuhasib.UI.Models;

public partial class InstallmentGridTotals : ObservableObject
{
    [ObservableProperty] private int _count;
    [ObservableProperty] private decimal _totalAmount;
    [ObservableProperty] private decimal _paidAmount;
    [ObservableProperty] private decimal _remainingAmount;
    [ObservableProperty] private int _countUsd;
    [ObservableProperty] private decimal _totalAmountUsd;
    [ObservableProperty] private decimal _paidAmountUsd;
    [ObservableProperty] private decimal _remainingAmountUsd;
    [ObservableProperty] private string _scopeNote = string.Empty;
    [ObservableProperty] private bool _showUsd;

    public bool HasScopeNote => !string.IsNullOrWhiteSpace(ScopeNote);
    public bool ShowUsdSection => ShowUsd && (CountUsd > 0 || TotalAmountUsd != 0 || PaidAmountUsd != 0 || RemainingAmountUsd != 0);

    public string TotalAmountText => AccountingCurrencyHelper.Format(TotalAmount, AccountingCurrency.IQD);
    public string PaidAmountText => AccountingCurrencyHelper.Format(PaidAmount, AccountingCurrency.IQD);
    public string RemainingAmountText => AccountingCurrencyHelper.Format(RemainingAmount, AccountingCurrency.IQD);
    public string TotalAmountUsdText => AccountingCurrencyHelper.Format(TotalAmountUsd, AccountingCurrency.USD);
    public string PaidAmountUsdText => AccountingCurrencyHelper.Format(PaidAmountUsd, AccountingCurrency.USD);
    public string RemainingAmountUsdText => AccountingCurrencyHelper.Format(RemainingAmountUsd, AccountingCurrency.USD);

    partial void OnScopeNoteChanged(string value) => OnPropertyChanged(nameof(HasScopeNote));
    partial void OnShowUsdChanged(bool value) => NotifyAmountTexts();
    partial void OnCountUsdChanged(int value) => OnPropertyChanged(nameof(ShowUsdSection));
    partial void OnTotalAmountChanged(decimal value) => OnPropertyChanged(nameof(TotalAmountText));
    partial void OnPaidAmountChanged(decimal value) => OnPropertyChanged(nameof(PaidAmountText));
    partial void OnRemainingAmountChanged(decimal value) => OnPropertyChanged(nameof(RemainingAmountText));
    partial void OnTotalAmountUsdChanged(decimal value)
    {
        OnPropertyChanged(nameof(TotalAmountUsdText));
        OnPropertyChanged(nameof(ShowUsdSection));
    }
    partial void OnPaidAmountUsdChanged(decimal value)
    {
        OnPropertyChanged(nameof(PaidAmountUsdText));
        OnPropertyChanged(nameof(ShowUsdSection));
    }
    partial void OnRemainingAmountUsdChanged(decimal value)
    {
        OnPropertyChanged(nameof(RemainingAmountUsdText));
        OnPropertyChanged(nameof(ShowUsdSection));
    }

    private void NotifyAmountTexts()
    {
        OnPropertyChanged(nameof(ShowUsdSection));
        OnPropertyChanged(nameof(TotalAmountText));
        OnPropertyChanged(nameof(PaidAmountText));
        OnPropertyChanged(nameof(RemainingAmountText));
        OnPropertyChanged(nameof(TotalAmountUsdText));
        OnPropertyChanged(nameof(PaidAmountUsdText));
        OnPropertyChanged(nameof(RemainingAmountUsdText));
    }

    public void Clear(string scopeNote = "")
    {
        Count = 0;
        TotalAmount = 0;
        PaidAmount = 0;
        RemainingAmount = 0;
        CountUsd = 0;
        TotalAmountUsd = 0;
        PaidAmountUsd = 0;
        RemainingAmountUsd = 0;
        ScopeNote = scopeNote;
    }

    private static AccountingCurrency ResolveCurrency(Installment i)
        => i.InstallmentPlan?.Invoice?.Currency ?? AccountingCurrency.IQD;

    private static AccountingCurrency ResolveCurrency(InstallmentPlan p)
        => p.Invoice?.Currency ?? AccountingCurrency.IQD;

    public void SetFromInstallments(IEnumerable<Installment> items, string? scopeNote = null, bool showUsd = false)
    {
        ShowUsd = showUsd;
        var list = items.ToList();
        var iqd = list.Where(i => ResolveCurrency(i) != AccountingCurrency.USD).ToList();
        var usd = showUsd
            ? list.Where(i => ResolveCurrency(i) == AccountingCurrency.USD).ToList()
            : [];

        // عند إيقاف تعدد العملات: اعرض كل السجلات كدينار (البيانات يجب أن تكون IQD)
        if (!showUsd)
            iqd = list;

        Count = iqd.Count;
        TotalAmount = iqd.Sum(i => i.Amount);
        PaidAmount = iqd.Sum(i => i.PaidAmount);
        RemainingAmount = iqd.Sum(i => i.RemainingAmount);

        CountUsd = usd.Count;
        TotalAmountUsd = usd.Sum(i => i.Amount);
        PaidAmountUsd = usd.Sum(i => i.PaidAmount);
        RemainingAmountUsd = usd.Sum(i => i.RemainingAmount);

        if (scopeNote is not null)
            ScopeNote = scopeNote;
    }

    public void SetFromTotals(
        int count, decimal totalAmount, decimal paidAmount, decimal remainingAmount,
        string? scopeNote = null,
        bool showUsd = false,
        int countUsd = 0,
        decimal totalAmountUsd = 0,
        decimal paidAmountUsd = 0,
        decimal remainingAmountUsd = 0)
    {
        ShowUsd = showUsd;
        Count = count;
        TotalAmount = totalAmount;
        PaidAmount = paidAmount;
        RemainingAmount = remainingAmount;
        CountUsd = showUsd ? countUsd : 0;
        TotalAmountUsd = showUsd ? totalAmountUsd : 0;
        PaidAmountUsd = showUsd ? paidAmountUsd : 0;
        RemainingAmountUsd = showUsd ? remainingAmountUsd : 0;
        if (scopeNote is not null)
            ScopeNote = scopeNote;
    }

    public void SetFromPlans(IEnumerable<InstallmentPlan> plans, string? scopeNote = null, bool showUsd = false)
    {
        ShowUsd = showUsd;
        var list = plans.ToList();
        var iqdPlans = showUsd
            ? list.Where(p => ResolveCurrency(p) != AccountingCurrency.USD).ToList()
            : list;
        var usdPlans = showUsd
            ? list.Where(p => ResolveCurrency(p) == AccountingCurrency.USD).ToList()
            : [];

        Count = iqdPlans.Count;
        TotalAmount = iqdPlans.Sum(p => p.TotalAmount);
        var iqdInst = iqdPlans.SelectMany(p => p.Installments).ToList();
        PaidAmount = iqdInst.Sum(i => i.PaidAmount);
        RemainingAmount = iqdInst.Sum(i => i.RemainingAmount);

        CountUsd = usdPlans.Count;
        TotalAmountUsd = usdPlans.Sum(p => p.TotalAmount);
        var usdInst = usdPlans.SelectMany(p => p.Installments).ToList();
        PaidAmountUsd = usdInst.Sum(i => i.PaidAmount);
        RemainingAmountUsd = usdInst.Sum(i => i.RemainingAmount);

        if (scopeNote is not null)
            ScopeNote = scopeNote;
    }

    public IList<string> ToPrintSummary()
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(ScopeNote))
            lines.Add(ScopeNote);
        lines.Add($"عدد السجلات (د.ع): {Count:N0}");
        lines.Add($"إجمالي المبالغ: {TotalAmountText}");
        lines.Add($"المسدد: {PaidAmountText}");
        lines.Add($"المتبقي: {RemainingAmountText}");
        if (ShowUsdSection)
        {
            lines.Add($"عدد السجلات ($): {CountUsd:N0}");
            lines.Add($"إجمالي المبالغ: {TotalAmountUsdText}");
            lines.Add($"المسدد: {PaidAmountUsdText}");
            lines.Add($"المتبقي: {RemainingAmountUsdText}");
        }
        return lines;
    }
}
