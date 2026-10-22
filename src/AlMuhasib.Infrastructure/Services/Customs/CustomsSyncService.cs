using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Infrastructure.Services.Customs;

/// <summary>مزامنة سحابية للتخليص — هيكل أولي (غير مفعّل بعد).</summary>
public sealed class CustomsSyncService : ISyncService, IDisposable
{
    public Task<SyncConnectionResult> TestConnectionAsync(CancellationToken ct = default) =>
        Task.FromResult(new SyncConnectionResult
        {
            IsSuccess = false,
            IsLicensed = false,
            Message = "مزامنة نظام التخليص الكمركي غير مفعّلة بعد"
        });

    public Task<SyncRunResult> SyncNowAsync(IProgress<SyncProgressUpdate>? progress = null, CancellationToken ct = default) =>
        Task.FromResult(new SyncRunResult
        {
            IsSuccess = false,
            Message = "مزامنة نظام التخليص الكمركي غير مفعّلة بعد"
        });

    public Task StartAutoSyncAsync(CancellationToken ct = default) => Task.CompletedTask;

    public void StopAutoSync()
    {
    }

    public void Dispose()
    {
    }
}
