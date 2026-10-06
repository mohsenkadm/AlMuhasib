using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class DailyOperationsReportCalculatorTests
{
    [Fact]
    public void ComputeNet_MatchesDailyOperationsFormula()
    {
        // 1000 - 100 - 200 - 150 + 300 = 850
        var net = DailyOperationsReportCalculator.ComputeNet(
            cashSales: 1000m,
            salesReturns: 100m,
            expenses: 200m,
            supplierPayments: 150m,
            receipts: 300m);

        Assert.Equal(850m, net);
    }

    [Fact]
    public void ComputeNet_CanBeNegative()
    {
        var net = DailyOperationsReportCalculator.ComputeNet(
            cashSales: 100m,
            salesReturns: 50m,
            expenses: 80m,
            supplierPayments: 40m,
            receipts: 10m);

        Assert.Equal(-60m, net);
    }

    [Fact]
    public void ComputeNet_ZeroInputs_IsZero()
    {
        Assert.Equal(0m, DailyOperationsReportCalculator.ComputeNet(0, 0, 0, 0, 0));
    }

    [Theory]
    [InlineData(25, 100, 25.0)]
    [InlineData(1, 3, 33.3)]
    [InlineData(0, 100, 0)]
    [InlineData(50, 0, 0)]
    public void SharePercent_RoundsToOneDecimal(decimal part, decimal total, decimal expected)
    {
        Assert.Equal(expected, DailyOperationsReportCalculator.SharePercent(part, total));
    }

    [Fact]
    public void SharePercents_SumNearHundred_ForPositiveAmounts()
    {
        var percents = DailyOperationsReportCalculator.SharePercents([50m, 30m, 20m]);
        Assert.Equal(3, percents.Count);
        Assert.Equal(50.0m, percents[0]);
        Assert.Equal(30.0m, percents[1]);
        Assert.Equal(20.0m, percents[2]);
        Assert.Equal(100.0m, percents.Sum());
    }

    [Fact]
    public void SharePercents_AllZero_ReturnsZeros()
    {
        var percents = DailyOperationsReportCalculator.SharePercents([0m, 0m]);
        Assert.All(percents, p => Assert.Equal(0m, p));
    }
}
