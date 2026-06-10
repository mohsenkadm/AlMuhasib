namespace AlMuhasib.Cloud.Core.Interfaces;

public interface INotificationService
{
    Task RegisterDeviceAsync(int tenantId, string playerId, string? deviceName, string? platform, CancellationToken ct = default);
    Task SendToTenantAsync(int tenantId, string title, string message, CancellationToken ct = default);
    Task SendToAllAsync(string title, string message, CancellationToken ct = default);
}
