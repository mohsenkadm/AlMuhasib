using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core;

/// <summary>قواعد احتساب نقاط الولاء — منطق نقي بدون قاعدة بيانات.</summary>
public static class LoyaltyPointsCalculator
{
    public static int CalculateEarnPoints(decimal invoiceBaseAmount, LoyaltySettings settings)
    {
        if (settings.PointsPerAmount <= 0m)
            return 0;
        if (invoiceBaseAmount < settings.MinInvoiceAmountToEarn)
            return 0;

        var raw = invoiceBaseAmount / settings.PointsPerAmount;
        if (settings.RoundEarnDown)
            return (int)Math.Floor(raw);
        return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
    }

    public static decimal CalculateRedeemDiscount(int points, LoyaltySettings settings)
    {
        if (points <= 0 || settings.PointValueInCurrency <= 0m)
            return 0m;
        return decimal.Round(points * settings.PointValueInCurrency, 2, MidpointRounding.AwayFromZero);
    }

    public static int MaxRedeemablePoints(
        int balance,
        decimal invoiceBaseAmount,
        LoyaltySettings settings)
    {
        if (balance <= 0 || invoiceBaseAmount <= 0m || settings.PointValueInCurrency <= 0m)
            return 0;

        var percentCap = Math.Clamp(settings.MaxRedeemPercentOfInvoice, 0m, 100m);
        var maxDiscount = decimal.Round(invoiceBaseAmount * percentCap / 100m, 2, MidpointRounding.AwayFromZero);
        if (maxDiscount <= 0m)
            return 0;

        var byValue = (int)Math.Floor(maxDiscount / settings.PointValueInCurrency);
        var capped = Math.Min(balance, byValue);
        if (capped < settings.MinPointsToRedeem)
            return 0;
        return capped;
    }

    public static (int Points, decimal Discount, string? Error) ValidateRedeem(
        int requestedPoints,
        int balance,
        decimal invoiceBaseAmount,
        LoyaltySettings settings)
    {
        if (requestedPoints <= 0)
            return (0, 0m, null);

        if (requestedPoints < settings.MinPointsToRedeem)
            return (0, 0m, $"الحد الأدنى للاستبدال {settings.MinPointsToRedeem} نقطة");

        if (requestedPoints > balance)
            return (0, 0m, "رصيد النقاط غير كافٍ");

        var max = MaxRedeemablePoints(balance, invoiceBaseAmount, settings);
        if (requestedPoints > max)
            return (0, 0m, $"لا يمكن استبدال أكثر من {max} نقطة لهذه الفاتورة");

        var discount = CalculateRedeemDiscount(requestedPoints, settings);
        if (discount > invoiceBaseAmount)
            discount = invoiceBaseAmount;

        return (requestedPoints, discount, null);
    }
}
