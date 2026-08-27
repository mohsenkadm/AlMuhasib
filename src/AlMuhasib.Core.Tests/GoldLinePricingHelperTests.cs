using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Infrastructure.Services.Gold;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class GoldLinePricingHelperTests
{
    [Fact]
    public void Calculate_UsesWeightTimesMithqalOverGrams_WithoutPurityOnGoldValue()
    {
        // 10g @ 21k: mithqal 800_000 / 5g = 160_000 per gram → gold = 1_600_000
        // purity 0.875 must NOT reduce gold value again
        var (_, pureGrams, goldValue, making, lineTotal) = GoldLinePricingHelper.Calculate(
            weightGrams: 10m,
            mithqalPrice: 800_000m,
            mithqalGrams: 5m,
            purityFactor: 0.875m,
            makingChargeMode: GoldMakingChargeMode.Fixed,
            makingChargeFixed: 50_000m,
            makingChargeRate: 0);

        Assert.Equal(8.75m, pureGrams);
        Assert.Equal(1_600_000m, goldValue);
        Assert.Equal(50_000m, making);
        Assert.Equal(1_650_000m, lineTotal);
    }

    [Fact]
    public void Calculate_RespectsCustomMithqalGrams()
    {
        // mithqal = 4.25g (some markets): 850_000 / 4.25 ≈ 200_000/g → 5g = 1_000_000
        var (pricePerGram, _, goldValue, _, _) = GoldLinePricingHelper.Calculate(
            weightGrams: 5m,
            mithqalPrice: 850_000m,
            mithqalGrams: 4.25m,
            purityFactor: 1m,
            makingChargeMode: GoldMakingChargeMode.Fixed,
            makingChargeFixed: 0,
            makingChargeRate: 0);

        Assert.Equal(200_000m, pricePerGram);
        Assert.Equal(1_000_000m, goldValue);
    }

    [Fact]
    public void Calculate_MakingChargePerGram()
    {
        var (_, _, goldValue, making, lineTotal) = GoldLinePricingHelper.Calculate(
            weightGrams: 10m,
            mithqalPrice: 500_000m,
            mithqalGrams: 5m,
            purityFactor: 1m,
            makingChargeMode: GoldMakingChargeMode.PerGram,
            makingChargeFixed: 0,
            makingChargeRate: 2_000m);

        Assert.Equal(1_000_000m, goldValue);
        Assert.Equal(20_000m, making);
        Assert.Equal(1_020_000m, lineTotal);
    }

    [Fact]
    public void Calculate_MakingChargePercentOfGold()
    {
        var (_, _, goldValue, making, lineTotal) = GoldLinePricingHelper.Calculate(
            weightGrams: 10m,
            mithqalPrice: 500_000m,
            mithqalGrams: 5m,
            purityFactor: 1m,
            makingChargeMode: GoldMakingChargeMode.PercentOfGold,
            makingChargeFixed: 0,
            makingChargeRate: 5m);

        Assert.Equal(1_000_000m, goldValue);
        Assert.Equal(50_000m, making);
        Assert.Equal(1_050_000m, lineTotal);
    }

    [Fact]
    public void Calculate_FallsBackToFiveGramsWhenMithqalGramsInvalid()
    {
        var (pricePerGram, _, goldValue, _, _) = GoldLinePricingHelper.Calculate(
            weightGrams: 5m,
            mithqalPrice: 500_000m,
            mithqalGrams: 0,
            purityFactor: 1m,
            makingChargeMode: GoldMakingChargeMode.Fixed,
            makingChargeFixed: 0,
            makingChargeRate: 0);

        Assert.Equal(100_000m, pricePerGram);
        Assert.Equal(500_000m, goldValue);
    }
}
