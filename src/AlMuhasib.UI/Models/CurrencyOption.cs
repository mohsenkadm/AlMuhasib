using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;

namespace AlMuhasib.UI.Models;

public sealed class CurrencyOption
{
    public AccountingCurrency Currency { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;

    public static IReadOnlyList<CurrencyOption> All { get; } =
    [
        new()
        {
            Currency = AccountingCurrency.IQD,
            DisplayName = "دينار (د.ع)",
            Label = AccountingCurrencyHelper.IqdLabel
        },
        new()
        {
            Currency = AccountingCurrency.USD,
            DisplayName = "دولار ($)",
            Label = AccountingCurrencyHelper.UsdLabel
        }
    ];
}
