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

    /// <summary>
    /// يحوّل قيمة قادمة من ربط WPF/JSON إلى عملة — يدعم الـ enum مباشرة أو int/byte/string
    /// لأن MultiBinding أحياناً يمرّر القيمة الأساسية للـ enum كـ Int32.
    /// </summary>
    public static bool TryResolve(object? value, out AccountingCurrency currency)
    {
        currency = AccountingCurrency.IQD;
        if (value is null)
            return false;

        switch (value)
        {
            case AccountingCurrency c:
                currency = c;
                return true;
            case int i when Enum.IsDefined(typeof(AccountingCurrency), i):
                currency = (AccountingCurrency)i;
                return true;
            case long l when l is >= int.MinValue and <= int.MaxValue
                            && Enum.IsDefined(typeof(AccountingCurrency), (int)l):
                currency = (AccountingCurrency)(int)l;
                return true;
            case byte b when Enum.IsDefined(typeof(AccountingCurrency), (int)b):
                currency = (AccountingCurrency)b;
                return true;
            case short s when Enum.IsDefined(typeof(AccountingCurrency), (int)s):
                currency = (AccountingCurrency)s;
                return true;
            case string str when !string.IsNullOrWhiteSpace(str):
                if (Enum.TryParse(str.Trim(), ignoreCase: true, out AccountingCurrency parsed))
                {
                    currency = parsed;
                    return true;
                }
                if (string.Equals(str.Trim(), "د.ع", StringComparison.Ordinal)
                    || string.Equals(str.Trim(), "دينار", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(str.Trim(), "IQD", StringComparison.OrdinalIgnoreCase))
                {
                    currency = AccountingCurrency.IQD;
                    return true;
                }
                if (str.Trim() is "$" or "دولار" or "USD" or "usd")
                {
                    currency = AccountingCurrency.USD;
                    return true;
                }
                return false;
            default:
                return false;
        }
    }

    public static AccountingCurrency ResolveOrDefault(object? value, AccountingCurrency fallback = AccountingCurrency.IQD)
        => TryResolve(value, out var currency) ? currency : fallback;

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
