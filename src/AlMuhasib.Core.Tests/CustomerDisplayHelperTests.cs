using AlMuhasib.Core;
using AlMuhasib.Core.Entities;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class CustomerDisplayHelperTests
{
    [Theory]
    [InlineData("محسن كاظم", "289", "محسن كاظم 289")]
    [InlineData("محسن كاظم", null, "محسن كاظم")]
    [InlineData("محسن كاظم", "", "محسن كاظم")]
    [InlineData("محسن كاظم", "  ", "محسن كاظم")]
    [InlineData("", "289", "")]
    public void FormatDisplayName_FormatsCorrectly(string name, string? fileNumber, string expected)
    {
        Assert.Equal(expected, CustomerDisplayHelper.FormatDisplayName(name, fileNumber));
    }

    [Fact]
    public void FormatDisplayName_FromCustomer_UsesEntityFields()
    {
        var customer = new Customer { Name = "أحمد", FileNumber = "101" };
        Assert.Equal("أحمد 101", CustomerDisplayHelper.FormatDisplayName(customer));
    }

    [Theory]
    [InlineData("289", true)]
    [InlineData("محسن", true)]
    [InlineData("0770", true)]
    [InlineData("xyz", false)]
    public void MatchesSearch_MatchesNamePhoneOrFileNumber(string term, bool expected)
    {
        var customer = new Customer
        {
            Name = "محسن كاظم",
            Phone = "07701234567",
            FileNumber = "289"
        };

        Assert.Equal(expected, CustomerDisplayHelper.MatchesSearch(customer, term));
    }

    [Fact]
    public void MatchesSearch_EmptyTerm_MatchesAll()
    {
        var customer = new Customer { Name = "Test" };
        Assert.True(CustomerDisplayHelper.MatchesSearch(customer, ""));
        Assert.True(CustomerDisplayHelper.MatchesSearch(customer, "   "));
    }
}
