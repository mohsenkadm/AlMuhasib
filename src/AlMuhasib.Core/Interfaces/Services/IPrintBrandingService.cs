using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Models;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IPrintBrandingService
{
    Task<PrintBrandingSettings> GetOrCreateSettingsAsync();
    Task<PrintBrandingSnapshot> GetSnapshotAsync();
    Task SaveAsync(PrintBrandingSettings settings);
    Task RefreshProviderAsync();
}
