using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Updates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AlMuhasib.UI.Services;

public sealed class AppUpdateService : IAppUpdateService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly AppUpdateOptions _options;
    private readonly string _statePath;
    private readonly string _downloadFolder;

    public AppUpdateService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient(nameof(AppUpdateService));
        _http.Timeout = TimeSpan.FromMinutes(Math.Clamp(
            configuration.GetValue($"{AppUpdateOptions.SectionName}:DownloadTimeoutMinutes", 30), 5, 120));

        _options = configuration.GetSection(AppUpdateOptions.SectionName).Get<AppUpdateOptions>()
                   ?? new AppUpdateOptions();

        var dataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AlMuhasib");
        Directory.CreateDirectory(dataRoot);
        _statePath = Path.Combine(dataRoot, "update-check.json");
        _downloadFolder = Path.Combine(dataRoot, "updates");
        Directory.CreateDirectory(_downloadFolder);
    }

    public Version GetCurrentVersion()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return asm.GetName().Version ?? new Version(1, 0, 0, 0);
    }

    public async Task<bool> IsOnlineAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ManifestUrl))
            return false;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, _options.ManifestUrl);
            using var response = await _http.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AppUpdateCheckResult> CheckForUpdateAsync(
        bool ignoreInterval = false,
        CancellationToken cancellationToken = default)
    {
        var current = GetCurrentVersion();

        if (!_options.Enabled)
            return new AppUpdateCheckResult { CurrentVersion = current, SkippedBecauseDisabled = true };

        if (string.IsNullOrWhiteSpace(_options.ManifestUrl))
            return AppUpdateCheckResult.NoUpdate(current);

        if (!ignoreInterval && !ShouldCheckNow())
            return new AppUpdateCheckResult { CurrentVersion = current, SkippedBecauseRecentCheck = true };

        if (!await IsOnlineAsync(cancellationToken))
            return new AppUpdateCheckResult { CurrentVersion = current, SkippedBecauseOffline = true };

        try
        {
            await using var stream = await _http.GetStreamAsync(_options.ManifestUrl, cancellationToken);
            var manifest = await JsonSerializer.DeserializeAsync<AppUpdateManifest>(stream, JsonOptions, cancellationToken);
            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version))
                return AppUpdateCheckResult.NoUpdate(current);

            if (!Version.TryParse(manifest.Version.Trim(), out var available))
                return AppUpdateCheckResult.NoUpdate(current);

            RecordCheckTime();

            if (available <= current)
                return AppUpdateCheckResult.NoUpdate(current);

            if (!string.IsNullOrWhiteSpace(manifest.MinSupportedVersion)
                && Version.TryParse(manifest.MinSupportedVersion.Trim(), out var minSupported)
                && current < minSupported)
            {
                manifest.IsMandatory = true;
            }

            return AppUpdateCheckResult.Available(current, available, manifest);
        }
        catch (Exception ex)
        {
            return new AppUpdateCheckResult
            {
                CurrentVersion = current,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task ApplyUpdateAsync(
        AppUpdateManifest manifest,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(manifest.DownloadUrl))
            throw new InvalidOperationException("رابط التحميل غير موجود في ملف التحديث.");

        var versionLabel = manifest.Version.Replace('.', '_');
        var packagePath = Path.Combine(_downloadFolder, $"AlMuhasib-{versionLabel}.zip");

        progress?.Report("جاري تنزيل التحديث...");
        await DownloadFileAsync(manifest.DownloadUrl, packagePath, manifest.SizeBytes, progress, cancellationToken);

        progress?.Report("جاري التحقق من سلامة الملف...");
        await VerifySha256Async(packagePath, manifest.Sha256, cancellationToken);

        var installDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var updaterPath = Path.Combine(installDir, "AlMuhasib.Updater.exe");
        if (!File.Exists(updaterPath))
            throw new FileNotFoundException("لم يتم العثور على AlMuhasib.Updater.exe بجانب التطبيق.", updaterPath);

        var pid = Process.GetCurrentProcess().Id;
        var args =
            $"--install-dir \"{installDir}\" --package \"{packagePath}\" --pid {pid} --main AlMuhasib.exe";

        progress?.Report("جاري تطبيق التحديث وإعادة التشغيل...");

        Process.Start(new ProcessStartInfo
        {
            FileName = updaterPath,
            Arguments = args,
            WorkingDirectory = installDir,
            UseShellExecute = true
        });
    }

    private async Task DownloadFileAsync(
        string url,
        string destination,
        long expectedSize,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        if (File.Exists(destination))
            File.Delete(destination);

        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = expectedSize > 0 ? expectedSize : response.Content.Headers.ContentLength ?? 0;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(destination);

        var buffer = new byte[81920];
        long read = 0;
        int count;
        while ((count = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
            read += count;
            if (total > 0)
            {
                var pct = (int)(read * 100 / total);
                progress?.Report($"جاري التنزيل... {pct}%");
            }
        }
    }

    private static async Task VerifySha256Async(string filePath, string expectedHex, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(expectedHex))
            return;

        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        var actual = Convert.ToHexString(hash);
        if (!actual.Equals(expectedHex.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("فشل التحقق من سلامة ملف التحديث (SHA256 غير متطابق).");
    }

    private bool ShouldCheckNow()
    {
        if (_options.CheckIntervalHours <= 0)
            return true;

        if (!File.Exists(_statePath))
            return true;

        try
        {
            var json = File.ReadAllText(_statePath);
            var state = JsonSerializer.Deserialize<UpdateCheckState>(json);
            if (state?.LastCheckUtc is null)
                return true;

            return DateTime.UtcNow - state.LastCheckUtc.Value >= TimeSpan.FromHours(_options.CheckIntervalHours);
        }
        catch
        {
            return true;
        }
    }

    private void RecordCheckTime()
    {
        try
        {
            var state = new UpdateCheckState { LastCheckUtc = DateTime.UtcNow };
            File.WriteAllText(_statePath, JsonSerializer.Serialize(state));
        }
        catch
        {
            // ignore
        }
    }

    private sealed class UpdateCheckState
    {
        public DateTime? LastCheckUtc { get; set; }
    }
}
