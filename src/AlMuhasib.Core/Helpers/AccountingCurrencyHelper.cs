using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Helpers;

public static class AccountingCurrencyHelper
{
    public const string IqdLabel = "د.ع";
    public const string UsdLabel = "$";

    public static string GetLabel(AccountingCurrency currency) =>
        currency == AccountingCurrency.USD ? UsdLabel : IqdLabel;

    public static string GetDisplayName(AccountingCurrency currency) =>
        currency == AccountingCurrency.USD ? "دولار" : "دينار";

    public static string Format(decimal amount, AccountingCurrency currency)
    {
        var decimals = currency == AccountingCurrency.USD ? "N2" : "N0";
        return $"{amount.ToString(decimals)} {GetLabel(currency)}";
    }

    /// <summary>تحويل مبلغ إلى الدينار (العملة الأساسية) باستخدام سعر محفوظ على المستند.</summary>
    public static decimal ToBaseIqd(decimal amount, AccountingCurrency currency, decimal fxRate)
    {
        if (currency == AccountingCurrency.IQD)
            return RoundIqd(amount);

        if (fxRate <= 0)
            throw new InvalidOperationException("لا يمكن تحويل مبلغ دولار بدون سعر صرف صالح.");

        return RoundIqd(amount * fxRate);
    }

    public static decimal Convert(
        decimal amount,
        AccountingCurrency from,
        AccountingCurrency to,
        decimal fxRate)
    {
        if (from == to)
            return from == AccountingCurrency.USD ? RoundUsd(amount) : RoundIqd(amount);

        if (fxRate <= 0)
            throw new InvalidOperationException("لا يمكن التحويل بين العملات بدون سعر صرف صالح.");

        return to == AccountingCurrency.IQD
            ? RoundIqd(amount * fxRate)
            : RoundUsd(amount / fxRate);
    }

    public static decimal RoundIqd(decimal value) =>
        Math.Round(value, 0, MidpointRounding.AwayFromZero);

    public static decimal RoundUsd(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal NormalizeAmount(decimal amount, AccountingCurrency currency) =>
        currency == AccountingCurrency.USD ? RoundUsd(amount) : RoundIqd(amount);
}
