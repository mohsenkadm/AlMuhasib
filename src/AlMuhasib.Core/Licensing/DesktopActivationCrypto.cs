using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AlMuhasib.Core.Licensing;

public static class DesktopActivationCrypto
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string CreateLifetimeKey(Guid installationId, string privateKeyPkcs8Base64)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPkcs8Base64))
            throw new ArgumentException("Private key is required.", nameof(privateKeyPkcs8Base64));

        var payload = new DesktopActivationPayload
        {
            InstallationId = installationId,
            Mode = "Lifetime",
            IssuedAtUtc = DateTime.UtcNow
        };

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var payloadPart = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyPkcs8Base64.Trim()), out _);
        var signature = ecdsa.SignData(Encoding.UTF8.GetBytes(payloadPart), HashAlgorithmName.SHA256);
        return $"{payloadPart}.{Base64UrlEncode(signature)}";
    }

    public static bool TryVerifyLifetimeKey(
        string activationKey,
        Guid expectedInstallationId,
        out string? error,
        string? publicKeySpkiBase64 = null)
    {
        error = null;
        activationKey = (activationKey ?? string.Empty).Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
        if (string.IsNullOrWhiteSpace(activationKey))
        {
            error = "أدخل مفتاح التفعيل.";
            return false;
        }

        var parts = activationKey.Split('.', 2);
        if (parts.Length != 2)
        {
            error = "صيغة مفتاح التفعيل غير صحيحة.";
            return false;
        }

        byte[] payloadBytes;
        byte[] signatureBytes;
        try
        {
            payloadBytes = Base64UrlDecode(parts[0]);
            signatureBytes = Base64UrlDecode(parts[1]);
        }
        catch
        {
            error = "صيغة مفتاح التفعيل غير صحيحة.";
            return false;
        }

        var publicKey = string.IsNullOrWhiteSpace(publicKeySpkiBase64)
            ? DesktopLicenseKeys.PublicKeySpkiBase64
            : publicKeySpkiBase64.Trim();

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
        if (!ecdsa.VerifyData(Encoding.UTF8.GetBytes(parts[0]), signatureBytes, HashAlgorithmName.SHA256))
        {
            error = "توقيع المفتاح غير صالح.";
            return false;
        }

        DesktopActivationPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<DesktopActivationPayload>(payloadBytes, JsonOptions);
        }
        catch
        {
            error = "محتوى المفتاح تالف.";
            return false;
        }

        if (payload is null || !string.Equals(payload.Mode, "Lifetime", StringComparison.OrdinalIgnoreCase))
        {
            error = "نوع المفتاح غير مدعوم.";
            return false;
        }

        if (payload.InstallationId != expectedInstallationId)
        {
            error = "المفتاح لا يطابق معرّف هذا التنصيب.";
            return false;
        }

        return true;
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string input)
    {
        var padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }
}
