using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Infrastructure.Services.Gold;

/// <summary>Placeholder cloud sync for Gold Shop until a dedicated mapper is added.</summary>
public sealed class GoldSyncService : ISyncService, IDisposable
{
    public Task<SyncConnectionResult> TestConnectionAsync(CancellationToken ct = default) =>
        Task.FromResult(new SyncConnectionResult
        {
            IsSuccess = false,
            IsLicensed = false,
            Message = "مزامنة محل الذهب غير مفعّلة حالياً"
        });

    public Task<SyncRunResult> SyncNowAsync(IProgress<SyncProgressUpdate>? progress = null, CancellationToken ct = default) =>
        Task.FromResult(new SyncRunResult
        {
            IsSuccess = false,
            Message = "مزامنة محل الذهب غير مفعّلة حالياً"
        });

    public Task StartAutoSyncAsync(CancellationToken ct = default) => Task.CompletedTask;

    public void StopAutoSync() { }

    public void Dispose() { }
}
