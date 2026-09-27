using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Helpers;

/// <summary>قواعد سلامة تعدد العملات — تمنع خلط العملات وFxRate=1 الصامت وتجاوز مفتاح الميزة.</summary>
public static class AccountingCurrencyRules
{
    public static void EnsureValidFxRate(AccountingCurrency currency, decimal fxRate, string? context = null)
    {
        if (currency == AccountingCurrency.IQD)
            return;

        if (fxRate <= 0)
        {
            var where = string.IsNullOrWhiteSpace(context) ? string.Empty : $" ({context})";
            throw new InvalidOperationException(
                $"سعر الصرف مطلوب وصالح للمستندات بالدولار{where}. سجّل سعر الصرف اليومي أولاً.");
        }
    }

    public static decimal RequireFxRateOrThrow(AccountingCurrency currency, decimal fxRate, string? context = null)
    {
        EnsureValidFxRate(currency, fxRate, context);
        return currency == AccountingCurrency.IQD ? 1m : fxRate;
    }

    /// <summary>
    /// فرض مفتاح Multi-Currency على عمليات الكتابة الجديدة.
    /// عند OFF: يرفض USD ويرجّع IQD/FxRate=1 — لا يُعتمد على إخفاء الواجهة فقط.
    /// عند ON: يطبّق RequireFxRateOrThrow كالمعتاد.
    /// المستندات التاريخية بالدولار تبقى مقروءة؛ هذا للحفظ الجديد فقط.
    /// </summary>
    public static (AccountingCurrency Currency, decimal FxRate) ApplyFeatureGate(
        bool multiCurrencyEnabled,
        AccountingCurrency currency,
        decimal fxRate,
        string? context = null)
    {
        if (multiCurrencyEnabled)
            return (currency, RequireFxRateOrThrow(currency, fxRate, context));

        if (currency != AccountingCurrency.IQD)
        {
            var where = string.IsNullOrWhiteSpace(context) ? string.Empty : $" ({context})";
            throw new InvalidOperationException(
                $"تعدد العملات غير مفعّل{where}. لا يمكن إنشاء أو تعديل مستند بالدولار. فعّل الميزة من إعدادات الميزات أو استخدم الدينار.");
        }

        return (AccountingCurrency.IQD, 1m);
    }

    public static void EnsureSameCurrency(
        AccountingCurrency left,
        AccountingCurrency right,
        string leftName,
        string rightName)
    {
        if (left == right)
            return;

        throw new InvalidOperationException(
            $"لا يمكن ربط {leftName} ({AccountingCurrencyHelper.GetDisplayName(left)}) مع {rightName} ({AccountingCurrencyHelper.GetDisplayName(right)}). يجب أن تكون العملة واحدة.");
    }

    /// <summary>تحويل آمن إلى الدينار — يرفض Fx غير صالح بدل fallback إلى 1.</summary>
    public static decimal ToBaseIqdStrict(decimal amount, AccountingCurrency currency, decimal fxRate)
    {
        if (currency == AccountingCurrency.IQD)
            return AccountingCurrencyHelper.RoundIqd(amount);

        EnsureValidFxRate(currency, fxRate);
        return AccountingCurrencyHelper.RoundIqd(amount * fxRate);
    }
}

public readonly record struct DualCurrencyBalance(decimal Iqd, decimal Usd)
{
    public bool HasUsd => Usd != 0m;
    public bool HasIqd => Iqd != 0m;
}
