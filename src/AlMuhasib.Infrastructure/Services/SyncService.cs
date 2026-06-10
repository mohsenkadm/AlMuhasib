using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Infrastructure.Data;
using AlMuhasib.Sync.Requests;
using AlMuhasib.Sync.Responses;
using Microsoft.EntityFrameworkCore;

namespace AlMuhasib.Infrastructure.Services;

public sealed class SyncService : ISyncService, IDisposable
{
    private const string GlobalSyncKey = "Global";
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ICloudSyncSettingsService _settingsService;
    private readonly SyncApiClient _apiClient;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private CancellationTokenSource? _autoSyncCts;

    public SyncService(
        IDbContextFactory<AppDbContext> contextFactory,
        ICloudSyncSettingsService settingsService,
        SyncApiClient apiClient)
    {
        _contextFactory = contextFactory;
        _settingsService = settingsService;
        _apiClient = apiClient;
    }

    public async Task<SyncConnectionResult> TestConnectionAsync(CancellationToken ct = default)
    {
        var settings = await _settingsService.GetAsync();
        ValidateSettings(settings);
        await EnsureAuthenticatedAsync(settings, ct);
        var status = await _apiClient.GetLicenseStatusAsync(settings, ct);
        return new SyncConnectionResult
        {
            IsSuccess = status.IsActive && status.IsMobileEnabled,
            IsLicensed = status.IsActive && status.IsMobileEnabled,
            LicenseExpiresAt = status.LicenseExpiresAt,
            Message = status.Message ?? (status.IsMobileEnabled ? "الاتصال ناجح" : "المزامنة غير مفعّلة")
        };
    }

    public async Task<SyncRunResult> SyncNowAsync(CancellationToken ct = default)
    {
        await _syncLock.WaitAsync(ct);
        try
        {
            return await SyncNowCoreAsync(ct);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private async Task<SyncRunResult> SyncNowCoreAsync(CancellationToken ct)
    {
        var settings = await _settingsService.GetAsync();
        ValidateSettings(settings);
        await EnsureAuthenticatedAsync(settings, ct);

        await using var db = await _contextFactory.CreateDbContextAsync(ct);
        await SyncIdEnsurer.EnsureAllAsync(db, ct);
        var syncState = await GetOrCreateSyncStateAsync(db, ct);

        var pushBundle = await SyncMapper.BuildPushBundleAsync(db, syncState.LastPushedAt, ct);
        var pushResult = await _apiClient.PushAsync(settings, new SyncPushRequest { Data = pushBundle }, ct);
        var hasConflicts = pushResult.Conflicts.Count > 0;

        var pullResult = await _apiClient.PullAsync(settings, new SyncPullRequest
        {
            Since = syncState.LastPulledAt,
            Cursor = syncState.ServerCursor
        }, ct);

        await using var applyDb = await _contextFactory.CreateDbContextAsync(ct);
        await using var tx = await applyDb.Database.BeginTransactionAsync(ct);
        try
        {
            await SyncMapper.ApplyPullBundleAsync(applyDb, pullResult.Data, ct);
            syncState = await GetOrCreateSyncStateAsync(applyDb, ct);
            if (!hasConflicts)
                syncState.LastPushedAt = pushResult.ServerTime;
            syncState.LastPulledAt = pullResult.ServerTime;
            syncState.ServerCursor = pullResult.Cursor;
            applyDb.SyncStates.Update(syncState);
            await applyDb.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }

        settings.LastSuccessfulSyncAt = hasConflicts ? settings.LastSuccessfulSyncAt : DateTime.UtcNow;
        settings.LastSyncError = hasConflicts
            ? $"تعارضات: {pushResult.Conflicts.Count} — لم يُحدَّث مؤشر الرفع"
            : null;
        await _settingsService.SaveAsync(settings);

        return new SyncRunResult
        {
            IsSuccess = !hasConflicts,
            AcceptedCount = pushResult.AcceptedCount,
            ConflictCount = pushResult.Conflicts.Count,
            Message = hasConflicts
                ? $"تم السحب مع {pushResult.Conflicts.Count} تعارض في الرفع — أعد المحاولة"
                : $"تمت المزامنة — {pushResult.AcceptedCount} سجل"
        };
    }

    public async Task StartAutoSyncAsync(CancellationToken ct = default)
    {
        StopAutoSync();
        var settings = await _settingsService.GetAsync();
        if (!settings.AutoSyncEnabled || settings.AutoSyncIntervalMinutes <= 0)
            return;

        _autoSyncCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _autoSyncCts.Token;
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await SyncNowAsync(token);
                }
                catch (Exception ex)
                {
                    var s = await _settingsService.GetAsync();
                    s.LastSyncError = ex.Message;
                    await _settingsService.SaveAsync(s);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(settings.AutoSyncIntervalMinutes), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    public void StopAutoSync()
    {
        _autoSyncCts?.Cancel();
        _autoSyncCts?.Dispose();
        _autoSyncCts = null;
    }

    public void Dispose()
    {
        StopAutoSync();
        _syncLock.Dispose();
    }

    private static void ValidateSettings(CloudSyncSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiBaseUrl))
            throw new InvalidOperationException("يرجى إدخال عنوان API");
        if (string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.Password))
            throw new InvalidOperationException("يرجى إدخال بيانات الدخول");
    }

    private async Task EnsureAuthenticatedAsync(CloudSyncSettings settings, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(settings.AccessToken) &&
            settings.AccessTokenExpiresAt.HasValue &&
            settings.AccessTokenExpiresAt.Value > DateTime.UtcNow.AddMinutes(1))
            return;

        var login = await _apiClient.LoginAsync(settings, ct);
        settings.AccessToken = login.AccessToken;
        settings.RefreshToken = login.RefreshToken;
        settings.AccessTokenExpiresAt = login.AccessTokenExpiresAt;
        await _settingsService.SaveAsync(settings);
    }

    private static async Task<SyncState> GetOrCreateSyncStateAsync(AppDbContext db, CancellationToken ct)
    {
        var state = await db.SyncStates.FindAsync([GlobalSyncKey], ct);
        if (state is not null) return state;
        state = new SyncState { EntityType = GlobalSyncKey };
        db.SyncStates.Add(state);
        await db.SaveChangesAsync(ct);
        return state;
    }
}
