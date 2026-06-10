using AlMuhasib.Cloud.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AlMuhasib.Cloud.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCloudApplication(this IServiceCollection services)
    {
        // Implementations registered in Cloud.Infrastructure
        return services;
    }

    public static IServiceCollection AddCloudApplicationServices(
        this IServiceCollection services,
        Action<CloudApplicationServiceOptions>? configure = null)
    {
        services.AddCloudApplication();
        services.Configure<CloudApplicationServiceOptions>(_ => { });
        if (configure is not null)
            services.Configure(configure);
        return services;
    }
}

public sealed class CloudApplicationServiceOptions
{
    public int DefaultTopProductsCount { get; set; } = 30;
    public decimal DefaultLowStockThreshold { get; set; } = 5;
    public int DefaultDeadStockDays { get; set; } = 90;
}
