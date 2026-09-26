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
            DisplayName = AccountingCurrencyHelper.GetDisplayName(AccountingCurrency.IQD),
            Label = AccountingCurrencyHelper.IqdLabel
        },
        new()
        {
            Currency = AccountingCurrency.USD,
            DisplayName = AccountingCurrencyHelper.GetDisplayName(AccountingCurrency.USD),
            Label = AccountingCurrencyHelper.UsdLabel
        }
    ];
}
