namespace AlMuhasib.Sync.Responses;

public sealed class TenantLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public int TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsMobileEnabled { get; set; }
    public DateTime? LicenseExpiresAt { get; set; }
    public DateTime? AccountExpiresAt { get; set; }
}

public sealed class LicenseStatusResponse
{
    public bool IsActive { get; set; }
    public bool IsMobileEnabled { get; set; }
    public DateTime? LicenseExpiresAt { get; set; }
    public DateTime? AccountExpiresAt { get; set; }
    public string? StatusCode { get; set; }
    public string? Message { get; set; }
}

public sealed class ApiErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
