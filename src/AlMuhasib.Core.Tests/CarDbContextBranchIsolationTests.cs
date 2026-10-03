using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Entities.Car;
using AlMuhasib.Infrastructure.Data.Car;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AlMuhasib.Core.Tests;

/// <summary>
/// عقود السيارات منفصلة عن تعدد الفروع المحاسبي — يجب ألا يُدرج BranchId في SQL.
/// </summary>
public class CarDbContextBranchIsolationTests
{
    [Fact]
    public void Car_model_does_not_map_BranchId_on_shared_entities()
    {
        using var db = new CarDbContext(
            new DbContextOptionsBuilder<CarDbContext>()
                .UseInMemoryDatabase($"car-branch-iso-{Guid.NewGuid():N}")
                .Options);

        AssertNoBranchProperty(db, typeof(AuditLog));
        AssertNoBranchProperty(db, typeof(PrintBrandingSettings));
        AssertNoBranchProperty(db, typeof(CarSaleContract));
        AssertNoBranchProperty(db, typeof(CarContractPayment));
        Assert.Null(db.Model.FindEntityType(typeof(Branch)));
    }

    private static void AssertNoBranchProperty(CarDbContext db, Type clrType)
    {
        var et = db.Model.FindEntityType(clrType);
        Assert.NotNull(et);
        Assert.DoesNotContain(et!.GetProperties(), p =>
            p.Name.Equals("BranchId", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(et.GetNavigations(), n =>
            n.Name.Equals("Branch", StringComparison.OrdinalIgnoreCase));
    }
}
