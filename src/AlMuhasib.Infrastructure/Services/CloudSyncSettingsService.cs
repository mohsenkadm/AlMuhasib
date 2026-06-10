using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class CloudSyncSettingsService : ICloudSyncSettingsService
{
    private const string DefaultLocalApiUrl = "http://localhost:5265";
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CloudSyncSettingsService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CloudSyncSettings> GetAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var settings = await context.CloudSyncSettings.FindAsync(CloudSyncSettings.SingletonId);
        if (settings is null)
        {
            settings = new CloudSyncSettings { ApiBaseUrl = DefaultLocalApiUrl };
            context.CloudSyncSettings.Add(settings);
            await context.SaveChangesAsync();
        }
        else if (string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
        {
            settings.ApiBaseUrl = DefaultLocalApiUrl;
            await context.SaveChangesAsync();
        }
        return settings;
    }

    public async Task SaveAsync(CloudSyncSettings settings)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        settings.Id = CloudSyncSettings.SingletonId;
        context.CloudSyncSettings.Update(settings);
        await context.SaveChangesAsync();
    }
}
