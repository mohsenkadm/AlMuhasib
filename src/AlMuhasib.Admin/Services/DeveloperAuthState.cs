using Microsoft.AspNetCore.Http;

namespace AlMuhasib.Admin.Services;

public sealed class DeveloperAuthState
{
    public const string CookieName = "AlMuhasib.DevAuth";

    /// <summary>جلسة محلية عند غياب قاعدة البيانات؛ لا تمنح وصولاً لبيانات العملاء.</summary>
    public const string OfflineUsername = "offline";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeveloperAuthState(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public bool IsAuthenticated => !string.IsNullOrEmpty(Username);

    public bool IsOffline =>
        string.Equals(Username, OfflineUsername, StringComparison.Ordinal);

    public string Username =>
        _httpContextAccessor.HttpContext?.Request.Cookies[CookieName] ?? string.Empty;

    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();

    public static CookieOptions CreateCookieOptions() => new()
    {
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
        Path = "/",
        Expires = DateTimeOffset.UtcNow.AddHours(8)
    };
}
