using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class LoyaltyPointsCalculatorTests
{
    private static LoyaltySettings DefaultSettings() => new()
    {
        PointsPerAmount = 1000m,
        PointValueInCurrency = 100m,
        MinInvoiceAmountToEarn = 0m,
        MinPointsToRedeem = 10,
        MaxRedeemPercentOfInvoice = 50m,
        RoundEarnDown = true
    };

    [Fact]
    public void CalculateEarnPoints_FloorsByDefault()
    {
        var points = LoyaltyPointsCalculator.CalculateEarnPoints(2500m, DefaultSettings());
        Assert.Equal(2, points);
    }

    [Fact]
    public void CalculateEarnPoints_RespectsMinimumInvoice()
    {
        var settings = DefaultSettings();
        settings.MinInvoiceAmountToEarn = 5000m;
        var points = LoyaltyPointsCalculator.CalculateEarnPoints(4999m, settings);
        Assert.Equal(0, points);
    }

    [Fact]
    public void ValidateRedeem_RejectsBelowMinimum()
    {
        var (_, _, error) = LoyaltyPointsCalculator.ValidateRedeem(5, 100, 10000m, DefaultSettings());
        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateRedeem_RejectsInsufficientBalance()
    {
        var (_, _, error) = LoyaltyPointsCalculator.ValidateRedeem(20, 15, 10000m, DefaultSettings());
        Assert.NotNull(error);
    }

    [Fact]
    public void ValidateRedeem_CapsByInvoicePercent()
    {
        // 50% of 10,000 = 5,000 discount max => 50 points at 100/point
        var max = LoyaltyPointsCalculator.MaxRedeemablePoints(1000, 10000m, DefaultSettings());
        Assert.Equal(50, max);

        var (points, discount, error) = LoyaltyPointsCalculator.ValidateRedeem(50, 1000, 10000m, DefaultSettings());
        Assert.Null(error);
        Assert.Equal(50, points);
        Assert.Equal(5000m, discount);
    }

    [Fact]
    public void CalculateRedeemDiscount_MultipliesPointValue()
    {
        var discount = LoyaltyPointsCalculator.CalculateRedeemDiscount(25, DefaultSettings());
        Assert.Equal(2500m, discount);
    }
}
