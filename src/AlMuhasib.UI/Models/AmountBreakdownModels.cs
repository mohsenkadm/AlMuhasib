using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;

namespace AlMuhasib.UI.Models;

public sealed class AmountBreakdownModel
{
    public string Title { get; init; } = "تفاصيل المبلغ";
    public string Subtitle { get; init; } = string.Empty;
    public string Formula { get; init; } = string.Empty;
    public decimal ResultAmount { get; init; }
    public AccountingCurrency ResultCurrency { get; init; } = AccountingCurrency.IQD;
    public string ResultLabel { get; init; } = "النتيجة";
    /// <summary>نتيجة دولار منفصلة تُعرض تحت النتيجة بالدينار عند التفعيل.</summary>
    public decimal? ResultAmountUsd { get; init; }
    public string? ResultLabelUsd { get; init; }
    public string? Note { get; init; }
    public IReadOnlyList<AmountBreakdownLine> Lines { get; init; } = [];
    public string? EntitiesTitle { get; init; }
    public IReadOnlyList<AmountBreakdownEntityRow> Entities { get; init; } = [];

    public string ResultAmountText =>
        AccountingCurrencyHelper.Format(ResultAmount, ResultCurrency);

    public bool ShowResultUsd => ResultAmountUsd.HasValue;
    public string ResultAmountUsdText =>
        ResultAmountUsd is { } usd
            ? AccountingCurrencyHelper.Format(usd, AccountingCurrency.USD)
            : string.Empty;
}

public sealed class AmountBreakdownLine
{
    public string Label { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public decimal AmountUsd { get; init; }
    public AccountingCurrency Currency { get; init; } = AccountingCurrency.IQD;
    public string Operator { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsResult { get; init; }

    public string FormattedAmount
    {
        get
        {
            if (AmountUsd != 0 && Amount != 0)
                return $"{AccountingCurrencyHelper.Format(Amount, AccountingCurrency.IQD)} · {AccountingCurrencyHelper.Format(AmountUsd, AccountingCurrency.USD)}";
            if (AmountUsd != 0)
                return AccountingCurrencyHelper.Format(AmountUsd, AccountingCurrency.USD);
            return AccountingCurrencyHelper.Format(Amount, Currency);
        }
    }
}

public sealed class AmountBreakdownEntityRow
{
    public string Name { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public decimal Amount { get; init; }
    public decimal AmountUsd { get; init; }
    public AccountingCurrency Currency { get; init; } = AccountingCurrency.IQD;
    public bool ShowAmount { get; init; } = true;

    public string FormattedAmount
    {
        get
        {
            if (AmountUsd != 0 && Amount != 0)
                return $"{AccountingCurrencyHelper.Format(Amount, AccountingCurrency.IQD)} · {AccountingCurrencyHelper.Format(AmountUsd, AccountingCurrency.USD)}";
            if (AmountUsd != 0)
                return AccountingCurrencyHelper.Format(AmountUsd, AccountingCurrency.USD);
            return AccountingCurrencyHelper.Format(Amount, Currency);
        }
    }
}
