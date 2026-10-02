using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Helpers;

/// <summary>
/// دمج مبالغ الدولار في مجاميع التقارير المالية بالدينار عند تفعيل MultiCurrency.
/// بدون الميزة: يُحتسب الدينار فقط ويُتجاهل الدولار.
/// </summary>
public static class FinancialReportIqdFoldIn
{
    /// <summary>طيّ الدولار إلى الدينار في المجاميع الرئيسية — ما لم يكن نطاق العرض دولاراً فقط.</summary>
    public static bool ShouldFoldUsdIntoIqd(bool multiCurrencyEnabled, ReportCurrencyScope scope = ReportCurrencyScope.Iqd)
        => multiCurrencyEnabled && scope != ReportCurrencyScope.Usd;

    /// <summary>مبلغ مستند إلى الدينار: IQD كما هو؛ USD × FxRate عند الطيّ.</summary>
    public static decimal AmountInBaseIqd(
        decimal amount,
        AccountingCurrency currency,
        decimal fxRate,
        bool foldInUsd)
    {
        if (currency == AccountingCurrency.IQD)
            return AccountingCurrencyHelper.RoundIqd(amount);

        if (!foldInUsd)
            return 0m;

        return AccountingCurrencyRules.ToBaseIqdStrict(amount, currency, fxRate);
    }

    /// <summary>صافي مبيعات موقّع (مرتجع سالب) محوّل إلى دينار عند الحاجة.</summary>
    public static decimal SignedSalesInBaseIqd(
        InvoiceType type,
        decimal netAmount,
        AccountingCurrency currency,
        decimal fxRate,
        bool foldInUsd)
        => AmountInBaseIqd(InvoiceFilters.SignedNetAmount(type, netAmount), currency, fxRate, foldInUsd);

    public static decimal SumSignedSalesInBaseIqd(
        IEnumerable<(InvoiceType Type, decimal NetAmount, AccountingCurrency Currency, decimal FxRate)> items,
        bool foldInUsd)
        => items.Sum(x => SignedSalesInBaseIqd(x.Type, x.NetAmount, x.Currency, x.FxRate, foldInUsd));

    public static decimal SumAmountsInBaseIqd(
        IEnumerable<(decimal Amount, AccountingCurrency Currency, decimal FxRate)> items,
        bool foldInUsd)
        => items.Sum(x => AmountInBaseIqd(x.Amount, x.Currency, x.FxRate, foldInUsd));

    /// <summary>
    /// رصيد نقد/مصرف: الدينار كما هو؛ الدولار بسعر صرف تاريخ التقرير (وليس Fx المستند).
    /// عند غياب سعر صالح يُتجاهل رصيد الدولار بدل كسر التقرير.
    /// </summary>
    public static decimal CashOrBankInBaseIqd(
        decimal balance,
        AccountingCurrency currency,
        decimal asOfUsdToIqd,
        bool foldInUsd)
    {
        if (currency == AccountingCurrency.IQD)
            return AccountingCurrencyHelper.RoundIqd(balance);

        if (!foldInUsd || asOfUsdToIqd <= 0)
            return 0m;

        return AccountingCurrencyHelper.RoundIqd(balance * asOfUsdToIqd);
    }

    public static decimal SumCashOrBankInBaseIqd(
        IEnumerable<(decimal Balance, AccountingCurrency Currency)> items,
        decimal asOfUsdToIqd,
        bool foldInUsd)
        => items.Sum(x => CashOrBankInBaseIqd(x.Balance, x.Currency, asOfUsdToIqd, foldInUsd));
}
