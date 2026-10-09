using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qaid.TelegramBot.Application.Abstractions;
using Qaid.TelegramBot.Application.Models;
using Qaid.TelegramBot.Application.Options;
using Qaid.TelegramBot.Infrastructure.Data;

namespace Qaid.TelegramBot.Infrastructure.Services;

public sealed class LinkService : ILinkService
{
    private readonly BotDbContext _db;
    private readonly ITokenProtector _protector;
    private readonly LinkOptions _options;

    public LinkService(BotDbContext db, ITokenProtector protector, IOptions<LinkOptions> options)
    {
        _db = db;
        _protector = protector;
        _options = options.Value;
    }

    public async Task<CreateLinkCodeResult> CreateLinkCodeAsync(CreateLinkCodeRequest request, CancellationToken ct)
    {
        var plain = GenerateCode(_options.CodeLength);
        var entity = new LinkCodeEntity
        {
            Id = Guid.NewGuid(),
            CodeHash = HashCode(plain),
            TenantId = request.TenantId,
            TenantAccountId = request.TenantAccountId,
            CompanyName = request.CompanyName,
            Username = request.Username,
            CurrentBranchId = request.CurrentBranchId,
            ProtectedAccessToken = _protector.Protect(request.AccessToken),
            ProtectedRefreshToken = _protector.Protect(request.RefreshToken),
            AccessTokenExpiresAt = request.AccessTokenExpiresAt,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_options.CodeLifetimeMinutes)
        };

        _db.LinkCodes.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new CreateLinkCodeResult
        {
            PlainCode = plain,
            ExpiresAtUtc = entity.ExpiresAtUtc,
            CompanyName = entity.CompanyName
        };
    }

    public async Task<(bool Ok, string Message, TelegramLinkInfo? Link)> ConsumeLinkCodeAsync(
        string plainCode, long telegramUserId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(plainCode))
            return (false, "رمز الربط غير صالح.", null);

        var hash = HashCode(plainCode.Trim());
        var entity = await _db.LinkCodes.FirstOrDefaultAsync(x => x.CodeHash == hash, ct);
        if (entity is null)
            return (false, "رمز الربط غير صحيح.", null);

        if (entity.UsedAtUtc is not null)
            return (false, "تم استخدام رمز الربط مسبقاً.", null);

        if (entity.ExpiresAtUtc <= DateTime.UtcNow)
            return (false, "انتهت صلاحية رمز الربط. سجّل الدخول من صفحة الربط مجدداً.", null);

        entity.UsedAtUtc = DateTime.UtcNow;
        entity.UsedByTelegramUserId = telegramUserId;

        var existing = await _db.TelegramLinks.FindAsync([telegramUserId], ct);
        if (existing is null)
        {
            existing = new TelegramLinkEntity { TelegramUserId = telegramUserId };
            _db.TelegramLinks.Add(existing);
        }

        existing.TenantId = entity.TenantId;
        existing.TenantAccountId = entity.TenantAccountId;
        existing.CompanyName = entity.CompanyName;
        existing.Username = entity.Username;
        existing.CurrentBranchId = entity.CurrentBranchId;
        existing.ProtectedAccessToken = entity.ProtectedAccessToken;
        existing.ProtectedRefreshToken = entity.ProtectedRefreshToken;
        existing.AccessTokenExpiresAt = entity.AccessTokenExpiresAt;
        existing.IsActive = true;
        existing.LinkedAtUtc = DateTime.UtcNow;
        existing.UnlinkedAtUtc = null;

        await _db.SaveChangesAsync(ct);
        return (true, "تم الربط.", Map(existing));
    }

    public async Task<TelegramLinkInfo?> GetActiveLinkAsync(long telegramUserId, CancellationToken ct)
    {
        var entity = await _db.TelegramLinks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId && x.IsActive, ct);
        return entity is null ? null : Map(entity);
    }

    public async Task UpdateTokensAsync(long telegramUserId, StoredSessionTokens tokens, CancellationToken ct)
    {
        var entity = await _db.TelegramLinks.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId && x.IsActive, ct);
        if (entity is null)
            return;

        entity.ProtectedAccessToken = _protector.Protect(tokens.AccessToken);
        entity.ProtectedRefreshToken = _protector.Protect(tokens.RefreshToken);
        entity.AccessTokenExpiresAt = tokens.AccessTokenExpiresAt;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> UnlinkAsync(long telegramUserId, CancellationToken ct)
    {
        var entity = await _db.TelegramLinks.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId, ct);
        if (entity is null)
            return false;

        entity.IsActive = false;
        entity.UnlinkedAtUtc = DateTime.UtcNow;
        entity.ProtectedAccessToken = string.Empty;
        entity.ProtectedRefreshToken = string.Empty;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task MarkSessionInvalidAsync(long telegramUserId, CancellationToken ct)
    {
        var entity = await _db.TelegramLinks.FirstOrDefaultAsync(x => x.TelegramUserId == telegramUserId && x.IsActive, ct);
        if (entity is null)
            return;

        entity.IsActive = false;
        entity.ProtectedAccessToken = string.Empty;
        entity.ProtectedRefreshToken = string.Empty;
        entity.UnlinkedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private TelegramLinkInfo Map(TelegramLinkEntity entity)
    {
        string access = string.Empty;
        string refresh = string.Empty;
        if (!string.IsNullOrEmpty(entity.ProtectedAccessToken))
        {
            try { access = _protector.Unprotect(entity.ProtectedAccessToken); }
            catch { /* session must re-link */ }
        }
        if (!string.IsNullOrEmpty(entity.ProtectedRefreshToken))
        {
            try { refresh = _protector.Unprotect(entity.ProtectedRefreshToken); }
            catch { /* session must re-link */ }
        }

        return new TelegramLinkInfo
        {
            TelegramUserId = entity.TelegramUserId,
            TenantId = entity.TenantId,
            TenantAccountId = entity.TenantAccountId,
            CompanyName = entity.CompanyName,
            Username = entity.Username,
            CurrentBranchId = entity.CurrentBranchId,
            IsActive = entity.IsActive,
            Tokens = new StoredSessionTokens
            {
                AccessToken = access,
                RefreshToken = refresh,
                AccessTokenExpiresAt = entity.AccessTokenExpiresAt
            }
        };
    }

    internal static string HashCode(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain.Trim().ToUpperInvariant()));
        return Convert.ToHexString(bytes);
    }

    private static string GenerateCode(int length)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var chars = new char[length];
        var bytes = RandomNumberGenerator.GetBytes(length);
        for (var i = 0; i < length; i++)
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        return new string(chars);
    }
}
