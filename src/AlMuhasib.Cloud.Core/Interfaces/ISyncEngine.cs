using AlMuhasib.Sync.Requests;
using AlMuhasib.Sync.Responses;

namespace AlMuhasib.Cloud.Core.Interfaces;

public interface ISyncEngine
{
    Task<SyncPushResponse> PushAsync(int tenantId, SyncPushRequest request, CancellationToken ct = default);
    Task<SyncPullResponse> PullAsync(int tenantId, SyncPullRequest request, CancellationToken ct = default);
    Task<SyncStatusResponse> GetStatusAsync(int tenantId, CancellationToken ct = default);
}
