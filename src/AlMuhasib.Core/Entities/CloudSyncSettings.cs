namespace AlMuhasib.Core.Entities;

/// <summary>إعدادات المزامنة السحابية (سجل واحد Id=1)</summary>
public class CloudSyncSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool AutoSyncEnabled { get; set; }
    public int AutoSyncIntervalMinutes { get; set; } = 15;
    public DateTime? LastSuccessfulSyncAt { get; set; }
    public string? LastSyncError { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? AccessTokenExpiresAt { get; set; }
}
