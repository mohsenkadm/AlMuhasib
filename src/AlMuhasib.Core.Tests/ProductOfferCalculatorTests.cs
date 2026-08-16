using AlMuhasib.Core;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class ProductOfferCalculatorTests
{
    [Theory]
    [InlineData(4, 4, 1, 1)]
    [InlineData(8, 4, 1, 2)]
    [InlineData(3, 4, 1, 0)]
    [InlineData(40, 4, 1, 10)]
    [InlineData(5, 2, 3, 6)]
    [InlineData(0, 4, 1, 0)]
    [InlineData(4, 0, 1, 0)]
    [InlineData(4, 4, 0, 0)]
    [InlineData(-1, 4, 1, 0)]
    public void ComputeGiftQuantity_MatchesBuyXGetY(
        decimal sold, decimal trigger, decimal giftPerCycle, decimal expected)
    {
        var result = ProductOfferCalculator.ComputeGiftQuantity(sold, trigger, giftPerCycle);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ComputeGiftQuantity_PartialCycleDoesNotGrantGift()
    {
        Assert.Equal(0m, ProductOfferCalculator.ComputeGiftQuantity(3.9m, 4m, 1m));
    }

    [Fact]
    public void ComputeGiftQuantity_ExactMultiple()
    {
        Assert.Equal(5m, ProductOfferCalculator.ComputeGiftQuantity(20m, 4m, 1m));
    }
}
