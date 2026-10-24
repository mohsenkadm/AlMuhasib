using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class WarrantyStatusHelperTests
{
    [Fact]
    public void StatusLabel_Null_ReturnsEmpty()
    {
        Assert.Equal(WarrantyStatusHelper.EmptyLabel, WarrantyStatusHelper.StatusLabel(null));
        Assert.Equal(WarrantyStatusHelper.EmptyLabel, WarrantyStatusHelper.RemainingLabel(null));
    }

    [Fact]
    public void StatusLabel_FutureDate_IsActive()
    {
        var end = new DateTime(2026, 12, 31);
        var asOf = new DateTime(2026, 10, 24);
        Assert.True(WarrantyStatusHelper.IsActive(end, asOf));
        Assert.Equal(WarrantyStatusHelper.ActiveLabel, WarrantyStatusHelper.StatusLabel(end, asOf));
        Assert.Contains("متبقي", WarrantyStatusHelper.RemainingLabel(end, asOf));
    }

    [Fact]
    public void StatusLabel_PastDate_IsExpired()
    {
        var end = new DateTime(2026, 1, 1);
        var asOf = new DateTime(2026, 10, 24);
        Assert.False(WarrantyStatusHelper.IsActive(end, asOf));
        Assert.Equal(WarrantyStatusHelper.ExpiredLabel, WarrantyStatusHelper.StatusLabel(end, asOf));
        Assert.Contains("منتهٍ", WarrantyStatusHelper.RemainingLabel(end, asOf));
    }

    [Fact]
    public void StatusLabel_SameDay_IsActive()
    {
        var day = new DateTime(2026, 10, 24);
        Assert.True(WarrantyStatusHelper.IsActive(day, day));
        Assert.Equal("ينتهي اليوم", WarrantyStatusHelper.RemainingLabel(day, day));
    }
}
