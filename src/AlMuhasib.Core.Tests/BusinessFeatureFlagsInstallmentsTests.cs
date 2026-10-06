using AlMuhasib.Core.Models.Ux;
using System.Text.Json;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class BusinessFeatureFlagsInstallmentsTests
{
    [Fact]
    public void Installments_DefaultsToEnabled()
    {
        var flags = new BusinessFeatureFlags();
        Assert.True(flags.Installments);
    }

    [Fact]
    public void Installments_MissingJsonProperty_RemainsEnabled()
    {
        // مستخدمون حاليون بلا مفتاح Installments في التفضيلات → تبقى الميزة مفعّلة
        const string json = """{"PurchaseReturns":true,"SalesReturns":false}""";
        var flags = JsonSerializer.Deserialize<BusinessFeatureFlags>(json);
        Assert.NotNull(flags);
        Assert.True(flags!.Installments);
        Assert.True(flags.PurchaseReturns);
        Assert.False(flags.SalesReturns);
    }

    [Fact]
    public void Installments_CanBeDisabledExplicitly()
    {
        const string json = """{"Installments":false}""";
        var flags = JsonSerializer.Deserialize<BusinessFeatureFlags>(json);
        Assert.NotNull(flags);
        Assert.False(flags!.Installments);
    }
}
