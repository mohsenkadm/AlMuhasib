using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AlMuhasib.Cloud.Infrastructure.Data;

public sealed class CloudDbContextFactory : IDesignTimeDbContextFactory<CloudDbContext>
{
    public CloudDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CloudDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=AlMuhasibCloudDb;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new CloudDbContext(options);
    }
}
