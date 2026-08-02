using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlMuhasib.Infrastructure.Data.Gold;

public class GoldDbContextFactory : IDesignTimeDbContextFactory<GoldDbContext>
{
    public GoldDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GoldDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.;Database=AlMuhasibGoldShopDb;Trusted_Connection=true;TrustServerCertificate=true;");

        return new GoldDbContext(optionsBuilder.Options);
    }
}
