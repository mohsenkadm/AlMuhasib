using Microsoft.Extensions.Logging;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Models;

namespace Qaid.TelegramBot.Application.Services;

public sealed class SessionTokenService : ISessionTokenService
{
    private readonly IQaidApiClient _api;
    private readonly ILinkService _links;
    private readonly ILogger<SessionTokenService> _logger;

    public SessionTokenService(IQaidApiClient api, ILinkService links, ILogger<SessionTokenService> logger)
    {
        _api = api;
        _links = links;
        _logger = logger;
    }

    public async Task<(bool Ok, string? AccessToken, string ErrorMessage)> GetValidAccessTokenAsync(
        TelegramLinkInfo link, CancellationToken ct)
    {
        if (!link.IsActive)
            return (false, null, ArabicSessionMessages.NotLinked);

        var tokens = link.Tokens;
        if (!string.IsNullOrWhiteSpace(tokens.AccessToken)
            && tokens.AccessTokenExpiresAt > DateTime.UtcNow.AddMinutes(1))
        {
            return (true, tokens.AccessToken, string.Empty);
        }

        if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
        {
            await _links.MarkSessionInvalidAsync(link.TelegramUserId, ct);
            return (false, null, ArabicSessionMessages.Expired);
        }

        try
        {
            var refreshed = await _api.RefreshAsync(tokens.RefreshToken, ct);
            var updated = new StoredSessionTokens
            {
                AccessToken = refreshed.AccessToken,
                RefreshToken = string.IsNullOrWhiteSpace(refreshed.RefreshToken)
                    ? tokens.RefreshToken
                    : refreshed.RefreshToken,
                AccessTokenExpiresAt = refreshed.AccessTokenExpiresAt
            };
            await _links.UpdateTokensAsync(link.TelegramUserId, updated, ct);
            return (true, updated.AccessToken, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token refresh failed for telegram user {UserId}", link.TelegramUserId);
            await _links.MarkSessionInvalidAsync(link.TelegramUserId, ct);
            return (false, null, ArabicSessionMessages.Expired);
        }
    }
}

public static class ArabicSessionMessages
{
    public const string NotLinked = "الحساب غير مربوط.";
    public const string Expired = "انتهت الجلسة. أعد الربط عبر /link.";
}
