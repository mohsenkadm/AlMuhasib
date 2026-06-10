using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ICloudSyncSettingsService
{
    Task<CloudSyncSettings> GetAsync();
    Task SaveAsync(CloudSyncSettings settings);
}
