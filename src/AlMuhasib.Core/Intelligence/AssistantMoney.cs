using System.Globalization;

namespace AlMuhasib.Core.Intelligence;

public static class AssistantMoney
{
    public static string Iqd(decimal value) =>
        value.ToString("N0", CultureInfo.InvariantCulture) + " دينار عراقي";

    public static string Usd(decimal value) =>
        value.ToString("N2", CultureInfo.InvariantCulture) + " دولار";

    public static string Count(int value) =>
        value.ToString("N0", CultureInfo.InvariantCulture);

    public static string Quantity(decimal value) =>
        value.ToString("N2", CultureInfo.InvariantCulture);

    public static string Percent(decimal? value) =>
        value is null ? "غير متاح" : value.Value.ToString("N1", CultureInfo.InvariantCulture) + "%";

    public static decimal? ChangePercent(decimal current, decimal previous)
    {
        if (previous == 0)
            return current == 0 ? 0 : null;

        return Math.Round((current - previous) / Math.Abs(previous) * 100m, 1, MidpointRounding.AwayFromZero);
    }
}
