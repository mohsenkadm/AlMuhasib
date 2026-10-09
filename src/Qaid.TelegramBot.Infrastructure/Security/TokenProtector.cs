using Microsoft.AspNetCore.DataProtection;
using Qaid.TelegramBot.Application.Abstractions;

namespace Qaid.TelegramBot.Infrastructure.Security;

public sealed class TokenProtector : ITokenProtector
{
    private readonly IDataProtector _protector;

    public TokenProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Qaid.TelegramBot.Tokens.v1");
    }

    public string Protect(string plaintext)
        => _protector.Protect(plaintext ?? string.Empty);

    public string Unprotect(string protectedPayload)
        => _protector.Unprotect(protectedPayload ?? string.Empty);
}
