using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlMuhasib.Infrastructure.Data.CarTrade;

public class CarTradeDbContextFactory : IDesignTimeDbContextFactory<CarTradeDbContext>
{
    public CarTradeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CarTradeDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.;Database=AlMuhasibCarTradingDb;Trusted_Connection=true;TrustServerCertificate=true;");

        return new CarTradeDbContext(optionsBuilder.Options);
    }
}
