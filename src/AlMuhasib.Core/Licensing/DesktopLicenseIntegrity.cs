using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AlMuhasib.Core.Models.License;

namespace AlMuhasib.Core.Licensing;

public static class DesktopLicenseIntegrity
{
    public static string Compute(DesktopLicenseState state)
    {
        var trialEnd = state.TrialEndsAt?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? "";
        var activated = state.ActivatedAt?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? "";
        var material =
            $"{state.InstallationId:D}|{(int)state.Mode}|{trialEnd}|{activated}|{state.ActivationPayload}|{DesktopLicenseKeys.IntegrityPepper}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return Convert.ToHexString(hash);
    }

    public static bool Matches(DesktopLicenseState state) =>
        string.Equals(Compute(state), state.IntegrityHash, StringComparison.OrdinalIgnoreCase);

    public static string CreateGrandfatherToken(Guid installationId)
    {
        var key = Encoding.UTF8.GetBytes(DesktopLicenseKeys.IntegrityPepper);
        var data = Encoding.UTF8.GetBytes($"gf:{installationId:D}");
        return "gf:" + Convert.ToHexString(HMACSHA256.HashData(key, data));
    }

    public static bool IsValidGrandfatherToken(Guid installationId, string? payload) =>
        !string.IsNullOrEmpty(payload) &&
        string.Equals(payload, CreateGrandfatherToken(installationId), StringComparison.OrdinalIgnoreCase);
}
