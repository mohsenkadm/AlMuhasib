using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Infrastructure.Services.Gold;

/// <summary>
/// Shared gold-value / making-charge math for quote, sale, purchase, and exchange lines.
/// Iraqi market: mithqal price is already per karat (سعر مثقال عيار 21), so:
/// goldValue = weight × (mithqalPrice / mithqalGrams)
/// Purity is tracked only for pureGrams reporting — never re-applied to the gold value.
/// </summary>
public static class GoldLinePricingHelper
{
    public static decimal ResolvePurityFactor(GoldKarat? karat) =>
        karat is null || karat.PurityFactor <= 0 ? 1m : karat.PurityFactor;

    public static decimal ComputeMakingCharge(
        GoldMakingChargeMode mode,
        decimal makingChargeFixed,
        decimal makingChargeRate,
        decimal weightGrams,
        decimal goldValue)
    {
        var making = mode switch
        {
            GoldMakingChargeMode.PerGram => weightGrams * Math.Max(0, makingChargeRate),
            GoldMakingChargeMode.PercentOfGold => goldValue * Math.Max(0, makingChargeRate) / 100m,
            _ => Math.Max(0, makingChargeFixed)
        };
        return GoldCurrencyHelper.Round(making);
    }

    public static (
        decimal PricePerGram,
        decimal PureGrams,
        decimal GoldValue,
        decimal MakingCharge,
        decimal LineTotal) Calculate(
        decimal weightGrams,
        decimal mithqalPrice,
        decimal mithqalGrams,
        decimal purityFactor,
        GoldMakingChargeMode makingChargeMode,
        decimal makingChargeFixed,
        decimal makingChargeRate)
    {
        var grams = mithqalGrams <= 0 ? 5m : mithqalGrams;
        var purity = purityFactor <= 0 ? 1m : purityFactor;
        var pricePerGram = GoldCurrencyHelper.Round(mithqalPrice / grams, 6);
        var pureGrams = GoldCurrencyHelper.Round(weightGrams * purity, 6);
        // Per-karat mithqal price — do not multiply purity again (would understate 21k by ~12.5%).
        var goldValue = GoldCurrencyHelper.Round(weightGrams * pricePerGram);
        var making = ComputeMakingCharge(
            makingChargeMode,
            makingChargeFixed,
            makingChargeRate,
            weightGrams,
            goldValue);
        var lineTotal = GoldCurrencyHelper.Round(goldValue + making);
        return (pricePerGram, pureGrams, goldValue, making, lineTotal);
    }

    public static GoldInvoiceLine BuildInvoiceLine(
        GoldSaleLineRequest lineReq,
        decimal mithqalGrams,
        decimal purityFactor,
        GoldInvoiceLineDirection direction,
        string defaultDescription)
    {
        if (lineReq.WeightGrams <= 0)
            throw new InvalidOperationException("وزن البند يجب أن يكون أكبر من صفر");
        if (lineReq.MithqalPrice <= 0)
            throw new InvalidOperationException("سعر المثقال يجب أن يكون أكبر من صفر");

        var (pricePerGram, _, goldValue, making, lineTotal) = Calculate(
            lineReq.WeightGrams,
            lineReq.MithqalPrice,
            mithqalGrams,
            purityFactor,
            lineReq.MakingChargeMode,
            lineReq.MakingCharge,
            lineReq.MakingChargeRate);

        return new GoldInvoiceLine
        {
            ItemId = lineReq.ItemId,
            KaratValue = lineReq.KaratValue,
            WeightGrams = lineReq.WeightGrams,
            MithqalPrice = lineReq.MithqalPrice,
            PricePerGram = pricePerGram,
            GoldValue = goldValue,
            MakingCharge = making,
            MakingChargeMode = lineReq.MakingChargeMode,
            MakingChargeRate = lineReq.MakingChargeRate,
            LineTotal = lineTotal,
            Description = string.IsNullOrWhiteSpace(lineReq.Description)
                ? $"{defaultDescription} عيار {lineReq.KaratValue}"
                : lineReq.Description,
            WeightFromScale = lineReq.WeightFromScale,
            LineDirection = direction
        };
    }
}
