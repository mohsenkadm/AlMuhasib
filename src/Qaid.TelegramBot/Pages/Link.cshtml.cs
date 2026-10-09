using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Models;

namespace Qaid.TelegramBot.Pages;

[EnableRateLimiting("link-page")]
public sealed class LinkModel : PageModel
{
    private readonly IQaidApiClient _api;
    private readonly ILinkService _links;
    private readonly ILogger<LinkModel> _logger;

    public LinkModel(IQaidApiClient api, ILinkService links, ILogger<LinkModel> logger)
    {
        _api = api;
        _links = links;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }
    public string? GeneratedCode { get; private set; }
    public DateTime? CodeExpiresAtUtc { get; private set; }
    public string? CompanyName { get; private set; }
    public List<BranchInfoDto> Branches { get; private set; } = [];
    public bool NeedsBranchSelection { get; private set; }
    public string? PendingAccessToken { get; set; }
    public string? PendingRefreshToken { get; set; }
    public DateTime? PendingAccessExpires { get; set; }
    public int PendingTenantId { get; set; }
    public string PendingUsername { get; set; } = string.Empty;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostLoginAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            var login = await _api.LoginAsync(Input.Username.Trim(), Input.Password, ct);
            Input.Password = string.Empty;

            if (login.RequiresBranchSelection && login.AllowedBranches.Count > 1 && Input.BranchId is null)
            {
                Branches = login.AllowedBranches;
                NeedsBranchSelection = true;
                PendingAccessToken = login.AccessToken;
                PendingRefreshToken = login.RefreshToken;
                PendingAccessExpires = login.AccessTokenExpiresAt;
                PendingTenantId = login.TenantId;
                PendingUsername = login.UsernameOrFallback(Input.Username);
                CompanyName = login.CompanyName;
                // Keep tokens only in encrypted TempData round-trip via form hidden fields (short lived).
                TempData["pat"] = login.AccessToken;
                TempData["prt"] = login.RefreshToken;
                TempData["pae"] = login.AccessTokenExpiresAt.ToString("O");
                TempData["ptid"] = login.TenantId.ToString();
                TempData["pun"] = PendingUsername;
                TempData["pcn"] = login.CompanyName;
                return Page();
            }

            int? branchId = login.CurrentBranchId ?? login.DefaultBranchId;
            var access = login.AccessToken;
            var refresh = login.RefreshToken;
            var expires = login.AccessTokenExpiresAt;

            if (login.RequiresBranchSelection && Input.BranchId is int selected)
            {
                var selectedBranch = await _api.SelectBranchAsync(access, selected, ct);
                access = selectedBranch.AccessToken;
                expires = selectedBranch.AccessTokenExpiresAt;
                branchId = selectedBranch.CurrentBranchId;
            }

            return await IssueCodeAsync(new CreateLinkCodeRequest
            {
                TenantId = login.TenantId,
                TenantAccountId = 0,
                CompanyName = login.CompanyName,
                Username = login.UsernameOrFallback(Input.Username),
                CurrentBranchId = branchId,
                AccessToken = access,
                RefreshToken = refresh,
                AccessTokenExpiresAt = expires
            }, ct);
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = "بيانات الدخول غير صحيحة.";
            return Page();
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "تعذّر الاتصال بخدمة قيد. حاول لاحقاً.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Link login failed");
            ErrorMessage = "تعذّر إتمام تسجيل الدخول.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSelectBranchAsync(CancellationToken ct)
    {
        var access = TempData["pat"] as string;
        var refresh = TempData["prt"] as string;
        var expiresRaw = TempData["pae"] as string;
        var tenantRaw = TempData["ptid"] as string;
        var username = TempData["pun"] as string ?? Input.Username;
        var company = TempData["pcn"] as string ?? string.Empty;

        if (string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(refresh)
            || !DateTime.TryParse(expiresRaw, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expires)
            || !int.TryParse(tenantRaw, out var tenantId)
            || Input.BranchId is null)
        {
            ErrorMessage = "انتهت جلسة الربط. سجّل الدخول مجدداً.";
            return Page();
        }

        try
        {
            var selected = await _api.SelectBranchAsync(access, Input.BranchId.Value, ct);
            return await IssueCodeAsync(new CreateLinkCodeRequest
            {
                TenantId = tenantId,
                TenantAccountId = 0,
                CompanyName = company,
                Username = username,
                CurrentBranchId = selected.CurrentBranchId,
                AccessToken = selected.AccessToken,
                RefreshToken = refresh,
                AccessTokenExpiresAt = selected.AccessTokenExpiresAt
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Branch selection failed");
            ErrorMessage = "تعذّر اختيار الفرع.";
            return Page();
        }
    }

    private async Task<IActionResult> IssueCodeAsync(CreateLinkCodeRequest request, CancellationToken ct)
    {
        // Best-effort: decode account id from JWT nameidentifier if present.
        request.TenantAccountId = TryReadAccountId(request.AccessToken);

        var result = await _links.CreateLinkCodeAsync(request, ct);
        GeneratedCode = result.PlainCode;
        CodeExpiresAtUtc = result.ExpiresAtUtc;
        CompanyName = result.CompanyName;
        return Page();
    }

    private static int TryReadAccountId(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return 0;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", out var id)
                && int.TryParse(id.GetString(), out var accountId))
                return accountId;
            if (doc.RootElement.TryGetProperty("nameid", out var nameId)
                && int.TryParse(nameId.GetString(), out accountId))
                return accountId;
            if (doc.RootElement.TryGetProperty("sub", out var sub)
                && int.TryParse(sub.GetString(), out accountId))
                return accountId;
        }
        catch
        {
            // ignore
        }
        return 0;
    }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public int? BranchId { get; set; }
    }
}

internal static class LoginResponseExtensions
{
    public static string UsernameOrFallback(this TenantLoginResponse login, string fallback)
        => string.IsNullOrWhiteSpace(fallback) ? login.TenantName : fallback.Trim();
}
