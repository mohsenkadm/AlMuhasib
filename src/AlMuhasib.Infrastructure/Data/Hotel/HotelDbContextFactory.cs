using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlMuhasib.Infrastructure.Data.Hotel;

public class HotelDbContextFactory : IDesignTimeDbContextFactory<HotelDbContext>
{
    public HotelDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HotelDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=.;Database=AlMuhasibHotelsDb;Trusted_Connection=true;TrustServerCertificate=true;");

        return new HotelDbContext(optionsBuilder.Options);
    }
}
