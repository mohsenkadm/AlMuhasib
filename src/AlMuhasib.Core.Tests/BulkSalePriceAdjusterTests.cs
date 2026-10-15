using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class BulkSalePriceAdjusterTests
{
    [Fact]
    public void Apply_Increase_TwentyPercent()
    {
        var result = BulkSalePriceAdjuster.Apply(1000m, 20m, isIncrease: true, PriceRoundingMode.None);
        Assert.Equal(1200m, result);
    }

    [Fact]
    public void Apply_Decrease_TwentyFivePercent()
    {
        var result = BulkSalePriceAdjuster.Apply(1000m, 25m, isIncrease: false, PriceRoundingMode.None);
        Assert.Equal(750m, result);
    }

    [Fact]
    public void Apply_ZeroPercent_ReturnsCurrentWithRounding()
    {
        var result = BulkSalePriceAdjuster.Apply(1234.567m, 0m, isIncrease: true, PriceRoundingMode.TwoDecimals);
        Assert.Equal(1234.57m, result);
    }

    [Fact]
    public void Apply_ZeroCurrent_StaysZero()
    {
        var result = BulkSalePriceAdjuster.Apply(0m, 50m, isIncrease: true, PriceRoundingMode.NearestOne);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void Apply_DecreaseBeyondHundred_ClampsToZero()
    {
        var result = BulkSalePriceAdjuster.Apply(100m, 150m, isIncrease: false, PriceRoundingMode.None);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void Apply_NegativePercent_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BulkSalePriceAdjuster.Apply(100m, -5m, isIncrease: true, PriceRoundingMode.None));
    }

    [Theory]
    [InlineData(10.126, PriceRoundingMode.TwoDecimals, 10.13)]
    [InlineData(10.1264, PriceRoundingMode.ThreeDecimals, 10.126)]
    [InlineData(10.12, PriceRoundingMode.NearestQuarter, 10.00)]
    [InlineData(10.13, PriceRoundingMode.NearestQuarter, 10.25)]
    [InlineData(10.24, PriceRoundingMode.NearestHalf, 10.00)]
    [InlineData(10.25, PriceRoundingMode.NearestHalf, 10.50)]
    [InlineData(10.4, PriceRoundingMode.NearestOne, 10)]
    [InlineData(10.5, PriceRoundingMode.NearestOne, 11)]
    [InlineData(10.01, PriceRoundingMode.CeilingToOne, 11)]
    [InlineData(10.00, PriceRoundingMode.CeilingToOne, 10)]
    public void ApplyRounding_Modes(decimal input, PriceRoundingMode mode, decimal expected)
    {
        var result = BulkSalePriceAdjuster.ApplyRounding(input, mode);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Apply_IncreaseWithCeilingRounding()
    {
        // 8250 * 1.05 = 8662.5 → ceiling 8663
        var result = BulkSalePriceAdjuster.Apply(8250m, 5m, isIncrease: true, PriceRoundingMode.CeilingToOne);
        Assert.Equal(8663m, result);
    }

    [Fact]
    public void GetDisplayName_ReturnsArabicLabels()
    {
        Assert.Equal("بدون تقريب", BulkSalePriceAdjuster.GetDisplayName(PriceRoundingMode.None));
        Assert.Equal("للأعلى إلى مضاعف صحيح", BulkSalePriceAdjuster.GetDisplayName(PriceRoundingMode.CeilingToOne));
    }
}
