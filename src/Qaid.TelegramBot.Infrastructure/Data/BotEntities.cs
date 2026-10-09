namespace Qaid.TelegramBot.Infrastructure.Data;

public sealed class LinkCodeEntity
{
    public Guid Id { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public int TenantAccountId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int? CurrentBranchId { get; set; }
    public string ProtectedAccessToken { get; set; } = string.Empty;
    public string ProtectedRefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public long? UsedByTelegramUserId { get; set; }
}

public sealed class TelegramLinkEntity
{
    public long TelegramUserId { get; set; }
    public int TenantId { get; set; }
    public int TenantAccountId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int? CurrentBranchId { get; set; }
    public string ProtectedAccessToken { get; set; } = string.Empty;
    public string ProtectedRefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime LinkedAtUtc { get; set; }
    public DateTime? UnlinkedAtUtc { get; set; }
}
