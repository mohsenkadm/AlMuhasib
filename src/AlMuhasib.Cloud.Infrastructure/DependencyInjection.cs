using System.Text;
using AlMuhasib.Cloud.Application.Abstractions;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Cloud.Infrastructure.Options;
using AlMuhasib.Cloud.Infrastructure.Reports;
using AlMuhasib.Cloud.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AlMuhasib.Cloud.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCloudInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<OneSignalOptions>(configuration.GetSection(OneSignalOptions.SectionName));

        services.AddDbContext<CloudDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("CloudConnection"),
                b => b.MigrationsAssembly(typeof(CloudDbContext).Assembly.FullName)));

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IAuthTokenService, AuthTokenService>();
        services.AddScoped<ILicenseValidator, LicenseValidator>();
        services.AddScoped<ISyncEngine, SyncEngine>();
        services.AddScoped<INotificationService, OneSignalNotificationService>();
        services.AddScoped<ICloudReportService, CloudReportService>();
        services.AddScoped<ICloudDashboardService, CloudDashboardService>();
        services.AddScoped<ICloudMasterDataService, CloudMasterDataService>();

        services.AddHttpClient("OneSignal");

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret))
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("Developer", p => p.RequireRole("Developer"))
            .AddPolicy("Tenant", p => p.RequireRole("Tenant"));

        return services;
    }
}
