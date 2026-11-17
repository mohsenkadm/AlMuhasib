using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class CompanyProfitMathTests
{
    [Fact]
    public void ZeroOtherIncome_MatchesPreviousFormulas()
    {
        const decimal gross = 1000m;
        const decimal expenses = 200m;
        const decimal fees = 30m;
        const decimal distributed = 50m;
        const decimal opening = 10m;

        Assert.Equal(gross - expenses, CompanyProfitMath.CardNet(gross, 0m, expenses));
        Assert.Equal(gross - expenses - fees, CompanyProfitMath.Operating(gross, 0m, expenses, fees));
        Assert.Equal(
            gross - expenses - fees - distributed + opening,
            CompanyProfitMath.FullNet(gross, 0m, expenses, fees, distributed, opening));
    }

    [Fact]
    public void OtherIncome_IsAddedAfterGross_AndDoesNotChangeGrossItself()
    {
        const decimal gross = 1000m;
        const decimal other = 150m;
        const decimal expenses = 200m;
        const decimal fees = 30m;
        const decimal distributed = 50m;
        const decimal opening = 10m;

        Assert.Equal(950m, CompanyProfitMath.CardNet(gross, other, expenses));
        Assert.Equal(920m, CompanyProfitMath.Operating(gross, other, expenses, fees));
        Assert.Equal(880m, CompanyProfitMath.FullNet(gross, other, expenses, fees, distributed, opening));
    }
}
