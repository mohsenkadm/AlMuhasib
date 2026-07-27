using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlMuhasib.Infrastructure.Data.RealEstate;

public class RealEstateDbContextFactory : IDesignTimeDbContextFactory<RealEstateDbContext>
{
    public RealEstateDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RealEstateDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.;Database=AlMuhasibRealEstateDb;Trusted_Connection=true;TrustServerCertificate=true;");

        return new RealEstateDbContext(optionsBuilder.Options);
    }
}
