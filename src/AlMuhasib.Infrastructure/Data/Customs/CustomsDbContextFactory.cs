using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlMuhasib.Infrastructure.Data.Customs;

public class CustomsDbContextFactory : IDesignTimeDbContextFactory<CustomsDbContext>
{
    public CustomsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CustomsDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.;Database=AlMuhasibCustomsDb;Trusted_Connection=true;TrustServerCertificate=true;");
        return new CustomsDbContext(optionsBuilder.Options);
    }
}
