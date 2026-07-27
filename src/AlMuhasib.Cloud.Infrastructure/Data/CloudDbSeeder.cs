using AlMuhasib.Cloud.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AlMuhasib.Cloud.Infrastructure.Data;

public static class CloudDbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("CloudDbSeeder");

        try
        {
            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count == 0)
            {
                logger.LogInformation("Cloud database is up to date. No pending migrations.");
            }
            else
            {
                logger.LogWarning(
                    "Applying {Count} pending cloud migration(s): {Migrations}",
                    pending.Count,
                    string.Join(", ", pending));
            }

            await db.Database.MigrateAsync();
            logger.LogInformation("Cloud database MigrateAsync completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex,
                "Failed to apply cloud database migrations. " +
                "Connection: {HasConnection}. Check SQL permissions and PendingModelChanges.",
                !string.IsNullOrWhiteSpace(config.GetConnectionString("CloudConnection")));
            throw;
        }

        if (!await db.DeveloperUsers.AnyAsync())
        {
            db.DeveloperUsers.Add(new DeveloperUser
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded default developer user 'admin'.");
        }

        if (env.IsDevelopment() && !await db.Tenants.AnyAsync())
        {
            var demoUser = config["LocalDev:DemoTenantUsername"] ?? "demo";
            var demoPass = config["LocalDev:DemoTenantPassword"] ?? "demo123";

            var tenant = new Tenant
            {
                CompanyName = "عميل تجريبي — محلي",
                IsMobileEnabled = true,
                LicenseExpiresAt = DateTime.UtcNow.AddYears(2),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            db.TenantAccounts.Add(new TenantAccount
            {
                TenantId = tenant.Id,
                Username = demoUser,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(demoPass),
                IsActive = true,
                ExpiresAt = DateTime.UtcNow.AddYears(2),
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
            logger.LogInformation("Seeded local demo tenant '{User}'.", demoUser);
        }
    }
}
