using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AlMuhasib.Cloud.Core.Interfaces;
using AlMuhasib.Cloud.Infrastructure.Data;
using AlMuhasib.Cloud.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AlMuhasib.Cloud.Infrastructure.Services;

public sealed class OneSignalNotificationService : INotificationService
{
    private readonly CloudDbContext _db;
    private readonly OneSignalOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;

    public OneSignalNotificationService(
        CloudDbContext db,
        IOptions<OneSignalOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task RegisterDeviceAsync(int tenantId, string playerId, string? deviceName, string? platform, CancellationToken ct = default)
    {
        var existing = await _db.DeviceSubscriptions
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.PlayerId == playerId, ct);

        if (existing is null)
        {
            _db.DeviceSubscriptions.Add(new Core.Entities.DeviceSubscription
            {
                TenantId = tenantId,
                PlayerId = playerId,
                DeviceName = deviceName,
                Platform = platform,
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.DeviceName = deviceName;
            existing.Platform = platform;
            existing.IsActive = true;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task SendToTenantAsync(int tenantId, string title, string message, CancellationToken ct = default)
    {
        if (!_options.Enabled) return;

        var playerIds = await _db.DeviceSubscriptions.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.IsActive)
            .Select(d => d.PlayerId)
            .ToListAsync(ct);

        if (playerIds.Count == 0) return;
        await SendAsync(playerIds, title, message, ct);
    }

    public async Task SendToAllAsync(string title, string message, CancellationToken ct = default)
    {
        if (!_options.Enabled) return;
        await SendAsync(null, title, message, ct);
    }

    private async Task SendAsync(List<string>? playerIds, string title, string message, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("OneSignal");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _options.RestApiKey);

        var payload = new Dictionary<string, object>
        {
            ["app_id"] = _options.AppId,
            ["headings"] = new { en = title, ar = title },
            ["contents"] = new { en = message, ar = message }
        };

        if (playerIds is { Count: > 0 })
            payload["include_player_ids"] = playerIds;
        else
            payload["included_segments"] = new[] { "All" };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        await client.PostAsync("https://onesignal.com/api/v1/notifications", content, ct);
    }
}
