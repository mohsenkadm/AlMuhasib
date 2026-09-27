using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Helpers;

/// <summary>مساعد فلترة تقارير المحاسبة حسب نطاق العملة.</summary>
public static class ReportCurrencyScopeHelper
{
    public static bool IncludesIqd(ReportCurrencyScope scope) =>
        scope is ReportCurrencyScope.Iqd or ReportCurrencyScope.All;

    public static bool IncludesUsd(ReportCurrencyScope scope) =>
        scope is ReportCurrencyScope.Usd or ReportCurrencyScope.All;

    /// <summary>
    /// عند Iqd/Usd يُرجع العملة الصارمة؛ عند All يُرجع null (لا فلتر — استخدم حقول *Usd منفصلة).
    /// </summary>
    public static AccountingCurrency? ToStrictFilter(ReportCurrencyScope scope) =>
        scope switch
        {
            ReportCurrencyScope.Iqd => AccountingCurrency.IQD,
            ReportCurrencyScope.Usd => AccountingCurrency.USD,
            _ => null
        };

    public static bool Matches(ReportCurrencyScope scope, AccountingCurrency currency) =>
        scope switch
        {
            ReportCurrencyScope.Iqd => currency == AccountingCurrency.IQD,
            ReportCurrencyScope.Usd => currency == AccountingCurrency.USD,
            ReportCurrencyScope.All => true,
            _ => currency == AccountingCurrency.IQD
        };

    public static string GetDisplayName(ReportCurrencyScope scope) =>
        scope switch
        {
            ReportCurrencyScope.Usd => "دولار",
            ReportCurrencyScope.All => "الكل (منفصل)",
            _ => "دينار"
        };
}
