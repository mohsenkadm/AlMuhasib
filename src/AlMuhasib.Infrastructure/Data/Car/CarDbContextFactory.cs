using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlMuhasib.Infrastructure.Data.Car;

public class CarDbContextFactory : IDesignTimeDbContextFactory<CarDbContext>
{
    public CarDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CarDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.;Database=AlMuhasibCarContractsDb;Trusted_Connection=true;TrustServerCertificate=true;");

        return new CarDbContext(optionsBuilder.Options);
    }
}
