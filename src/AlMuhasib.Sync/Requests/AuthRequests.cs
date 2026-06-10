namespace AlMuhasib.Sync.Requests;

public sealed class TenantLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class DeveloperLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class RegisterDeviceRequest
{
    public string PlayerId { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? Platform { get; set; }
}

public sealed class SendNotificationRequest
{
    public int? TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
