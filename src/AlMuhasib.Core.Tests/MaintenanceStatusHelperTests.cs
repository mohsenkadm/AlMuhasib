using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;
using Xunit;

namespace AlMuhasib.Core.Tests;

public class MaintenanceStatusHelperTests
{
    [Theory]
    [InlineData(MaintenanceStatus.Working, "يعمل")]
    [InlineData(MaintenanceStatus.NotWorking, "لا يعمل")]
    [InlineData(MaintenanceStatus.UnderRepair, "تحت الصيانة")]
    [InlineData(MaintenanceStatus.Delivered, "تم التسليم")]
    public void ToArabic_MapsExpectedLabels(MaintenanceStatus status, string expected)
    {
        Assert.Equal(expected, MaintenanceStatusHelper.ToArabic(status));
    }

    [Fact]
    public void All_ContainsFourStatuses()
    {
        Assert.Equal(4, MaintenanceStatusHelper.All.Count);
        Assert.Equal(5, MaintenanceStatusHelper.FilterOptions.Count);
        Assert.Null(MaintenanceStatusHelper.FilterOptions[0].Value);
    }
}
