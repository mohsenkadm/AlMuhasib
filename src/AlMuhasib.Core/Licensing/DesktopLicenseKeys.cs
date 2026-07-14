namespace AlMuhasib.Core.Licensing;

/// <summary>
/// Public verification key is embedded in the client. The PKCS#8 private key
/// must only live in Admin environment configuration (DesktopLicense:PrivateKeyPkcs8
/// via user-secrets / environment variable) — never commit the private key.
/// </summary>
public static class DesktopLicenseKeys
{
    /// <summary>SubjectPublicKeyInfo (DER) base64 — ECDSA P-256.</summary>
    public const string PublicKeySpkiBase64 =
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEqkR8mrduD/EWFeHGYYICyxlwWC6JUdxLsxOX8VMGlP4vhM36LahUvFU0UiSa6l+u0bF44u9zA4OCigBTeBn53A==";

    /// <summary>Soft pepper for local file integrity (not a substitute for signed activation).</summary>
    public const string IntegrityPepper = "Qayd-Desktop-License-Integrity-v1";
}
