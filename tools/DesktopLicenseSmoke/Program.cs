using System.Security.Cryptography;
using AlMuhasib.Core.Licensing;
using AlMuhasib.Core.Models.License;
using AlMuhasib.Infrastructure.Services;

// Ephemeral key pair — never commit a real PrivateKeyPkcs8 into source control.
using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
var privateKey = Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey());
var publicKey = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());

var root = Path.Combine(Path.GetTempPath(), "qayd-license-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var failures = 0;

void Assert(bool condition, string name)
{
    Console.WriteLine(condition ? $"PASS {name}" : $"FAIL {name}");
    if (!condition) failures++;
}

// 1) Existing install → Grandfathered
{
    var path = Path.Combine(root, "gf.json");
    var svc = new DesktopLicenseService(path, publicKey);
    var status = svc.EnsureInitialized(profileIsConfigured: true);
    Assert(status.Mode == DesktopLicenseMode.Grandfathered, "grandfather mode");
    Assert(status.IsUsable, "grandfather usable");
    Assert(!status.ShowsTrialBanner, "grandfather no banner");
}

// 2) New trial
{
    var path = Path.Combine(root, "trial.json");
    var svc = new DesktopLicenseService(path, publicKey);
    var status = svc.StartTrial(30);
    Assert(status.Mode == DesktopLicenseMode.Trial, "trial mode");
    Assert(status.IsUsable, "trial usable");
    Assert(status.ShowsTrialBanner, "trial banner");
    Assert(status.DaysRemaining is >= 29 and <= 30, $"trial days={status.DaysRemaining}");
}

// 3) Expired trial locks
{
    var path = Path.Combine(root, "expired.json");
    var svc = new DesktopLicenseService(path, publicKey);
    svc.StartTrial(30);
    var json = File.ReadAllText(path);
    var state = System.Text.Json.JsonSerializer.Deserialize<DesktopLicenseState>(json, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })!;
    state.TrialEndsAt = DateTime.UtcNow.AddDays(-1);
    state.IntegrityHash = DesktopLicenseIntegrity.Compute(state);
    File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
    var svc2 = new DesktopLicenseService(path, publicKey);
    var expired = svc2.EnsureInitialized(false);
    Assert(!expired.IsUsable, "expired not usable");
    Assert(expired.IsTrial, "expired is trial");
}

// 4) Activate lifetime with ephemeral keys
{
    var path = Path.Combine(root, "activate.json");
    var svc = new DesktopLicenseService(path, publicKey);
    var status = svc.StartTrial(1);
    var key = DesktopActivationCrypto.CreateLifetimeKey(status.InstallationId, privateKey);
    Assert(
        DesktopActivationCrypto.TryVerifyLifetimeKey(key, status.InstallationId, out _, publicKey),
        "crypto verify");
    Assert(svc.TryActivate(key, out var err), $"activate ok ({err})");
    var after = svc.GetStatus();
    Assert(after.Mode == DesktopLicenseMode.Lifetime, "lifetime mode");
    Assert(after.IsUsable, "lifetime usable");
    Assert(!after.ShowsTrialBanner, "lifetime no banner");
}

// 5) Wrong installation id key rejected
{
    var path = Path.Combine(root, "badkey.json");
    var svc = new DesktopLicenseService(path, publicKey);
    svc.StartTrial(1);
    var otherKey = DesktopActivationCrypto.CreateLifetimeKey(Guid.NewGuid(), privateKey);
    Assert(!svc.TryActivate(otherKey, out _), "wrong installation rejected");
    Assert(svc.GetStatus().Mode == DesktopLicenseMode.Trial, "still trial after bad key");
}

// 6) Tampered trial dates become unusable
{
    var path = Path.Combine(root, "tamper.json");
    var svc = new DesktopLicenseService(path, publicKey);
    svc.StartTrial(1);
    var json = File.ReadAllText(path);
    var state = System.Text.Json.JsonSerializer.Deserialize<DesktopLicenseState>(json, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })!;
    state.TrialEndsAt = DateTime.UtcNow.AddYears(10);
    File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
    var svc2 = new DesktopLicenseService(path, publicKey);
    var status = svc2.EnsureInitialized(false);
    Assert(!status.IsUsable, "tampered trial locked");
}

// 7) Forged Grandfathered mode without token locks
{
    var path = Path.Combine(root, "forged-gf.json");
    var state = new DesktopLicenseState
    {
        InstallationId = Guid.NewGuid(),
        Mode = DesktopLicenseMode.Grandfathered,
        ActivatedAt = DateTime.UtcNow,
        ActivationPayload = "fake",
        LastSeenUtc = DateTime.UtcNow
    };
    state.IntegrityHash = DesktopLicenseIntegrity.Compute(state);
    File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }));
    var svc = new DesktopLicenseService(path, publicKey);
    var status = svc.EnsureInitialized(false);
    Assert(!status.IsUsable, "forged grandfather locked");
}

Directory.Delete(root, true);
Console.WriteLine(failures == 0 ? "ALL PASSED" : $"FAILURES={failures}");
return failures == 0 ? 0 : 1;
