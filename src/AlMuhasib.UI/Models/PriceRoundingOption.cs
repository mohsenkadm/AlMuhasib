using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;

namespace AlMuhasib.UI.Models;

public sealed class PriceRoundingOption
{
    public PriceRoundingMode Mode { get; init; }
    public string DisplayName { get; init; } = string.Empty;

    public static IReadOnlyList<PriceRoundingOption> All { get; } =
        Enum.GetValues<PriceRoundingMode>()
            .Select(m => new PriceRoundingOption
            {
                Mode = m,
                DisplayName = BulkSalePriceAdjuster.GetDisplayName(m)
            })
            .ToList();
}
