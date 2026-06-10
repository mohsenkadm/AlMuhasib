using AlMuhasib.Cloud.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AlMuhasib.Cloud.Infrastructure.Data;

public static class CloudDbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        await db.Database.MigrateAsync();

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
        }
    }
}
