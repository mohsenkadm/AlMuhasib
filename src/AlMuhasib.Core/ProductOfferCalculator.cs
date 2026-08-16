namespace AlMuhasib.Core;

/// <summary>حاسبة كميات هدايا عروض المنتجات (Buy X Get Y).</summary>
public static class ProductOfferCalculator
{
    /// <summary>
    /// يحسب كمية الهدية: floor(كمية_البيع / كمية_التفعيل) × كمية_الهدية.
    /// </summary>
    public static decimal ComputeGiftQuantity(
        decimal soldQuantity,
        decimal triggerQuantity,
        decimal giftQuantityPerCycle)
    {
        if (soldQuantity <= 0 || triggerQuantity <= 0 || giftQuantityPerCycle <= 0)
            return 0m;

        var cycles = Math.Floor(soldQuantity / triggerQuantity);
        return cycles * giftQuantityPerCycle;
    }
}
