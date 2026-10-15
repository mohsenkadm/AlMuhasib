using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Helpers;

/// <summary>حساب سعر البيع الجديد عند التعديل الجماعي (نسبة + تقريب).</summary>
public static class BulkSalePriceAdjuster
{
    public static decimal Apply(
        decimal currentPrice,
        decimal percent,
        bool isIncrease,
        PriceRoundingMode roundingMode)
    {
        if (percent < 0)
            throw new ArgumentOutOfRangeException(nameof(percent), "النسبة لا يمكن أن تكون سالبة.");

        if (currentPrice <= 0 || percent == 0)
            return ApplyRounding(Math.Max(0m, currentPrice), roundingMode);

        var factor = percent / 100m;
        var raw = isIncrease
            ? currentPrice * (1m + factor)
            : currentPrice * (1m - factor);

        if (raw < 0)
            raw = 0m;

        return ApplyRounding(raw, roundingMode);
    }

    public static decimal ApplyRounding(decimal value, PriceRoundingMode mode)
    {
        if (value < 0)
            value = 0m;

        return mode switch
        {
            PriceRoundingMode.None => value,
            PriceRoundingMode.TwoDecimals => Math.Round(value, 2, MidpointRounding.AwayFromZero),
            PriceRoundingMode.ThreeDecimals => Math.Round(value, 3, MidpointRounding.AwayFromZero),
            PriceRoundingMode.NearestQuarter => RoundToStep(value, 0.25m),
            PriceRoundingMode.NearestHalf => RoundToStep(value, 0.5m),
            PriceRoundingMode.NearestOne => Math.Round(value, 0, MidpointRounding.AwayFromZero),
            PriceRoundingMode.CeilingToOne => Math.Ceiling(value),
            _ => value
        };
    }

    public static string GetDisplayName(PriceRoundingMode mode) => mode switch
    {
        PriceRoundingMode.None => "بدون تقريب",
        PriceRoundingMode.TwoDecimals => "رقمين عشريين",
        PriceRoundingMode.ThreeDecimals => "ثلاثة أرقام عشرية",
        PriceRoundingMode.NearestQuarter => "أقرب 0.25",
        PriceRoundingMode.NearestHalf => "أقرب 0.5",
        PriceRoundingMode.NearestOne => "أقرب واحد صحيح",
        PriceRoundingMode.CeilingToOne => "للأعلى إلى مضاعف صحيح",
        _ => mode.ToString()
    };

    private static decimal RoundToStep(decimal value, decimal step)
    {
        if (step <= 0)
            return value;
        return Math.Round(value / step, 0, MidpointRounding.AwayFromZero) * step;
    }
}
