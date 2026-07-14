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

var jsonOpts = new System.Text.Json.JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
};

// 1) Legacy install → Grandfathered
{
    var path = Path.Combine(root, "gf.json");
    var svc = new DesktopLicenseService(path, publicKey);
    var legacySelectedAt = DesktopLicenseKeys.FeatureIntroducedUtc.AddDays(-30);
    var status = svc.EnsureInitialized(profileIsConfigured: true, legacySelectedAt);
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
    var state = System.Text.Json.JsonSerializer.Deserialize<DesktopLicenseState>(File.ReadAllText(path), jsonOpts)!;
    state.TrialEndsAt = DateTime.UtcNow.AddDays(-1);
    state.IntegrityHash = DesktopLicenseIntegrity.Compute(state);
    File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(state, jsonOpts));
    var svc2 = new DesktopLicenseService(path, publicKey);
    var expired = svc2.EnsureInitialized(true, DateTime.UtcNow);
    Assert(!expired.IsUsable, "expired not usable");
    Assert(expired.IsTrial, "expired is trial");
}

// 4) Activate lifetime
{
    var path = Path.Combine(root, "activate.json");
    var svc = new DesktopLicenseService(path, publicKey);
    var status = svc.StartTrial(1);
    var key = DesktopActivationCrypto.CreateLifetimeKey(status.InstallationId, privateKey);
    Assert(DesktopActivationCrypto.TryVerifyLifetimeKey(key, status.InstallationId, out _, publicKey), "crypto verify");
    Assert(svc.TryActivate(key, out var err), $"activate ok ({err})");
    var after = svc.GetStatus();
    Assert(after.Mode == DesktopLicenseMode.Lifetime, "lifetime mode");
    Assert(after.IsUsable, "lifetime usable");
}

// 5) Wrong installation id rejected
{
    var path = Path.Combine(root, "badkey.json");
    var svc = new DesktopLicenseService(path, publicKey);
    svc.StartTrial(1);
    var otherKey = DesktopActivationCrypto.CreateLifetimeKey(Guid.NewGuid(), privateKey);
    Assert(!svc.TryActivate(otherKey, out _), "wrong installation rejected");
    Assert(svc.GetStatus().Mode == DesktopLicenseMode.Trial, "still trial after bad key");
}

// 6) Tampered trial without valid integrity → locked
{
    var path = Path.Combine(root, "tamper.json");
    var svc = new DesktopLicenseService(path, publicKey);
    svc.StartTrial(1);
    var state = System.Text.Json.JsonSerializer.Deserialize<DesktopLicenseState>(File.ReadAllText(path), jsonOpts)!;
    state.TrialEndsAt = DateTime.UtcNow.AddYears(10);
    File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(state, jsonOpts));
    var svc2 = new DesktopLicenseService(path, publicKey);
    Assert(!svc2.EnsureInitialized(true, DateTime.UtcNow).IsUsable, "tampered trial locked");
}

// 7) Forged grandfather string → locked
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
    File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(state, jsonOpts));
    var svc = new DesktopLicenseService(path, publicKey);
    Assert(!svc.EnsureInitialized(true, DateTime.UtcNow).IsUsable, "forged grandfather locked");
}

// 8) CRITICAL: deleting license after new install must NOT grandfather
{
    var path = Path.Combine(root, "deleted.json");
    var svc = new DesktopLicenseService(path, publicKey);
    svc.StartTrial(30);
    File.Delete(path);
    var svc2 = new DesktopLicenseService(path, publicKey);
    var afterDelete = svc2.EnsureInitialized(true, DateTime.UtcNow);
    Assert(!afterDelete.IsUsable, "delete-file does not unlock");
    Assert(afterDelete.Mode == DesktopLicenseMode.Trial, "delete-file becomes expired trial");
    Assert(afterDelete.Mode != DesktopLicenseMode.Grandfathered, "delete-file not grandfathered");
}

// 9) CRITICAL: corrupt file fails closed (not grandfather)
{
    var path = Path.Combine(root, "corrupt.json");
    File.WriteAllText(path, "{ not-json");
    var svc = new DesktopLicenseService(path, publicKey);
    var status = svc.EnsureInitialized(true, DesktopLicenseKeys.FeatureIntroducedUtc.AddDays(-100));
    Assert(!status.IsUsable, "corrupt fails closed");
    Assert(status.Mode != DesktopLicenseMode.Grandfathered, "corrupt not grandfathered");
}

// 10) RefreshFromDisk sees expiry written externally
{
    var path = Path.Combine(root, "refresh.json");
    var svc = new DesktopLicenseService(path, publicKey);
    svc.StartTrial(30);
    Assert(svc.IsUsable, "refresh pre usable");
    var state = System.Text.Json.JsonSerializer.Deserialize<DesktopLicenseState>(File.ReadAllText(path), jsonOpts)!;
    state.TrialEndsAt = DateTime.UtcNow.AddDays(-1);
    state.IntegrityHash = DesktopLicenseIntegrity.Compute(state);
    File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(state, jsonOpts));
    var refreshed = svc.RefreshFromDisk();
    Assert(!refreshed.IsUsable, "refresh sees expiry");
}

Directory.Delete(root, recursive: true);
Console.WriteLine(failures == 0 ? "ALL PASSED" : $"FAILURES={failures}");
return failures == 0 ? 0 : 1;
