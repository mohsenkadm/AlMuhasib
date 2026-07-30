namespace AlMuhasib.UI.Helpers;

/// <summary>
/// Iraqi dinar banknote denominations and greedy change breakdown.
/// </summary>
public static class IraqiCurrencyHelper
{
    public static readonly decimal[] Denominations =
    [
        50_000m,
        25_000m,
        10_000m,
        5_000m,
        1_000m,
        500m,
        250m
    ];

    public static IReadOnlyList<DenominationCount> BreakDown(decimal amount)
    {
        if (amount <= 0)
            return Array.Empty<DenominationCount>();

        // Work in whole dinars; ignore fractional fils for cashier UX.
        var remaining = Math.Floor(amount);
        var result = new List<DenominationCount>(Denominations.Length);

        foreach (var denom in Denominations)
        {
            var count = (int)(remaining / denom);
            if (count <= 0)
                continue;

            result.Add(new DenominationCount(denom, count));
            remaining -= count * denom;
        }

        return result;
    }

    public static string FormatLabel(decimal denomination) =>
        denomination switch
        {
            50_000m => "50,000",
            25_000m => "25,000",
            10_000m => "10,000",
            5_000m => "5,000",
            1_000m => "1,000",
            500m => "500",
            250m => "250",
            _ => denomination.ToString("N0")
        };

    /// <summary>Distinct palette approximating Iraqi note colors (stylized, not photographic).</summary>
    public static (string Primary, string Secondary, string Accent) GetColors(decimal denomination) =>
        denomination switch
        {
            50_000m => ("#5D4037", "#8D6E63", "#D7CCC8"),
            25_000m => ("#0D47A1", "#1976D2", "#BBDEFB"),
            10_000m => ("#1B5E20", "#43A047", "#C8E6C9"),
            5_000m => ("#4A148C", "#7B1FA2", "#E1BEE7"),
            1_000m => ("#BF360C", "#E64A19", "#FFCCBC"),
            500m => ("#00695C", "#00897B", "#B2DFDB"),
            250m => ("#4527A0", "#7E57C2", "#D1C4E9"),
            _ => ("#37474F", "#607D8B", "#CFD8DC")
        };
}

public sealed record DenominationCount(decimal Denomination, int Count)
{
    public decimal Total => Denomination * Count;
}
