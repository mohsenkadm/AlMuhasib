using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

/// <summary>
/// Persists cloud sync settings in whichever system DbContext is active
/// (Accounting / Car / Hotel / CarTrade).
/// </summary>
public sealed class CloudSyncSettingsService<TContext> : ICloudSyncSettingsService
    where TContext : DbContext
{
    private const string DefaultLocalApiUrl = "https://mohsenkadmapple-001-site1.dtempurl.com";
    private readonly IDbContextFactory<TContext> _contextFactory;

    public CloudSyncSettingsService(IDbContextFactory<TContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CloudSyncSettings> GetAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var set = context.Set<CloudSyncSettings>();
        var settings = await set.FindAsync(CloudSyncSettings.SingletonId);
        if (settings is null)
        {
            settings = new CloudSyncSettings { ApiBaseUrl = DefaultLocalApiUrl };
            set.Add(settings);
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
        context.Set<CloudSyncSettings>().Update(settings);
        await context.SaveChangesAsync();
    }
}
