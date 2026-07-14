using System.IO;
using System.Text.Json;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Licensing;
using AlMuhasib.Core.Models.License;

namespace AlMuhasib.Infrastructure.Services;

public sealed class DesktopLicenseService : IDesktopLicenseService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _path;
    private readonly string _publicKeySpkiBase64;
    private DesktopLicenseState? _state;

    public DesktopLicenseService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib",
            "desktop-license.json"))
    {
    }

    /// <summary>Test seam for isolated file paths / verification key.</summary>
    public DesktopLicenseService(string licenseFilePath, string? publicKeySpkiBase64 = null)
    {
        _path = licenseFilePath;
        _publicKeySpkiBase64 = string.IsNullOrWhiteSpace(publicKeySpkiBase64)
            ? DesktopLicenseKeys.PublicKeySpkiBase64
            : publicKeySpkiBase64;
        var dir = Path.GetDirectoryName(licenseFilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    public bool IsUsable => GetStatus().IsUsable;

    public DesktopLicenseStatus EnsureInitialized(bool profileIsConfigured)
    {
        if (TryLoad(out var existing) && existing is not null)
        {
            _state = existing;
            NormalizeLoadedState(existing);
            return ToStatus(existing);
        }

        if (profileIsConfigured)
        {
            var installationId = Guid.NewGuid();
            var grandfathered = new DesktopLicenseState
            {
                InstallationId = installationId,
                Mode = DesktopLicenseMode.Grandfathered,
                ActivatedAt = DateTime.UtcNow,
                ActivationPayload = DesktopLicenseIntegrity.CreateGrandfatherToken(installationId),
                LastSeenUtc = DateTime.UtcNow
            };
            grandfathered.IntegrityHash = DesktopLicenseIntegrity.Compute(grandfathered);
            Persist(grandfathered);
            return ToStatus(grandfathered);
        }

        return new DesktopLicenseStatus
        {
            InstallationId = Guid.Empty,
            Mode = DesktopLicenseMode.Trial,
            IsUsable = true,
            IsTrial = false,
            Summary = "بانتظار إعداد النظام"
        };
    }

    public DesktopLicenseStatus StartTrial(int trialDays = IDesktopLicenseService.DefaultTrialDays)
    {
        if (TryLoad(out var existing) && existing is not null)
        {
            NormalizeLoadedState(existing);
            return ToStatus(existing);
        }

        var now = DateTime.UtcNow;
        var days = trialDays <= 0 ? IDesktopLicenseService.DefaultTrialDays : trialDays;
        var state = new DesktopLicenseState
        {
            InstallationId = Guid.NewGuid(),
            Mode = DesktopLicenseMode.Trial,
            TrialStartedAt = now,
            TrialEndsAt = now.AddDays(days),
            LastSeenUtc = now
        };
        state.IntegrityHash = DesktopLicenseIntegrity.Compute(state);
        Persist(state);
        return ToStatus(state);
    }

    public DesktopLicenseStatus GetStatus()
    {
        if (_state is null && !TryLoad(out _state))
        {
            return new DesktopLicenseStatus
            {
                InstallationId = Guid.Empty,
                Mode = DesktopLicenseMode.Trial,
                IsUsable = false,
                IsTrial = true,
                Summary = "لا يوجد ترخيص"
            };
        }

        NormalizeLoadedState(_state!);
        return ToStatus(_state!);
    }

    public bool TryActivate(string activationKey, out string? error)
    {
        var status = GetStatus();
        if (status.InstallationId == Guid.Empty)
        {
            error = "لا يوجد معرّف تنصيب. أكمل إعداد النظام أولاً.";
            return false;
        }

        if (!DesktopActivationCrypto.TryVerifyLifetimeKey(
                activationKey,
                status.InstallationId,
                out error,
                _publicKeySpkiBase64))
            return false;

        var state = _state ?? throw new InvalidOperationException("License state missing.");
        state.Mode = DesktopLicenseMode.Lifetime;
        state.ActivatedAt = DateTime.UtcNow;
        state.ActivationPayload = activationKey.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
        state.LastSeenUtc = DateTime.UtcNow;
        state.IntegrityHash = DesktopLicenseIntegrity.Compute(state);
        Persist(state);
        error = null;
        return true;
    }

    private void NormalizeLoadedState(DesktopLicenseState state)
    {
        var integrityOk = DesktopLicenseIntegrity.Matches(state);
        var lifetimeOk = state.Mode == DesktopLicenseMode.Lifetime &&
                         !string.IsNullOrWhiteSpace(state.ActivationPayload) &&
                         DesktopActivationCrypto.TryVerifyLifetimeKey(
                             state.ActivationPayload!,
                             state.InstallationId,
                             out _,
                             _publicKeySpkiBase64);
        var grandfatherOk = state.Mode == DesktopLicenseMode.Grandfathered &&
                            DesktopLicenseIntegrity.IsValidGrandfatherToken(state.InstallationId, state.ActivationPayload);

        if (!integrityOk)
        {
            if (lifetimeOk)
            {
                state.Mode = DesktopLicenseMode.Lifetime;
                state.IntegrityHash = DesktopLicenseIntegrity.Compute(state);
                Persist(state);
            }
            else if (grandfatherOk)
            {
                state.Mode = DesktopLicenseMode.Grandfathered;
                state.IntegrityHash = DesktopLicenseIntegrity.Compute(state);
                Persist(state);
            }
            else
            {
                // Tamper or corruption of a trial (or forged mode): force expired trial lock.
                state.Mode = DesktopLicenseMode.Trial;
                state.TrialEndsAt = DateTime.UtcNow.AddDays(-1);
                state.ActivationPayload = null;
                state.IntegrityHash = DesktopLicenseIntegrity.Compute(state);
                Persist(state);
            }

            return;
        }

        if (state.Mode == DesktopLicenseMode.Lifetime && !lifetimeOk)
        {
            state.Mode = DesktopLicenseMode.Trial;
            state.TrialEndsAt = DateTime.UtcNow.AddDays(-1);
            state.ActivationPayload = null;
            state.IntegrityHash = DesktopLicenseIntegrity.Compute(state);
            Persist(state);
            return;
        }

        if (state.Mode == DesktopLicenseMode.Grandfathered && !grandfatherOk)
        {
            state.Mode = DesktopLicenseMode.Trial;
            state.TrialEndsAt = DateTime.UtcNow.AddDays(-1);
            state.ActivationPayload = null;
            state.IntegrityHash = DesktopLicenseIntegrity.Compute(state);
            Persist(state);
            return;
        }

        TouchLastSeen(state);
    }

    private void TouchLastSeen(DesktopLicenseState state)
    {
        var now = DateTime.UtcNow;
        if (state.LastSeenUtc > now.AddMinutes(30) && state.Mode == DesktopLicenseMode.Trial)
        {
            state.TrialEndsAt = now.AddDays(-1);
            state.IntegrityHash = DesktopLicenseIntegrity.Compute(state);
            Persist(state);
            return;
        }

        state.LastSeenUtc = now;
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(state, JsonOptions));
            _state = state;
        }
        catch
        {
            // ignore IO failures on touch
        }
    }

    private void Persist(DesktopLicenseState state)
    {
        File.WriteAllText(_path, JsonSerializer.Serialize(state, JsonOptions));
        _state = state;
    }

    private bool TryLoad(out DesktopLicenseState? state)
    {
        state = null;
        if (!File.Exists(_path))
            return false;

        try
        {
            var json = File.ReadAllText(_path);
            state = JsonSerializer.Deserialize<DesktopLicenseState>(json, JsonOptions);
            if (state is null || state.InstallationId == Guid.Empty)
                return false;
            _state = state;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static DesktopLicenseStatus ToStatus(DesktopLicenseState state)
    {
        var now = DateTime.UtcNow;
        var isTrial = state.Mode == DesktopLicenseMode.Trial;
        int? daysRemaining = null;
        var usable = state.Mode is DesktopLicenseMode.Lifetime or DesktopLicenseMode.Grandfathered;

        if (isTrial)
        {
            if (state.TrialEndsAt is { } end)
            {
                var remaining = (int)Math.Ceiling((end.ToUniversalTime() - now).TotalDays);
                daysRemaining = Math.Max(0, remaining);
                usable = end.ToUniversalTime() > now;
            }
            else
            {
                usable = false;
                daysRemaining = 0;
            }
        }

        var summary = state.Mode switch
        {
            DesktopLicenseMode.Lifetime => "مرخّص مدى الحياة",
            DesktopLicenseMode.Grandfathered => "ترخيص دائم (عميل سابق)",
            DesktopLicenseMode.Trial when usable => $"تجريبي — متبقي {daysRemaining} يوماً",
            DesktopLicenseMode.Trial => "انتهت الفترة التجريبية",
            _ => state.Mode.ToString()
        };

        return new DesktopLicenseStatus
        {
            InstallationId = state.InstallationId,
            Mode = state.Mode,
            TrialEndsAt = state.TrialEndsAt,
            DaysRemaining = daysRemaining,
            IsUsable = usable,
            IsTrial = isTrial,
            Summary = summary
        };
    }
}
