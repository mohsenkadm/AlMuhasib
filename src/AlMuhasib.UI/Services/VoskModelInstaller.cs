using System.IO;
using System.IO.Compression;
using System.Net.Http;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.UI.Services;

internal static class VoskModelInstaller
{
    public const string ModelFolderName = "vosk-model-small-ar-0.3";
    private const string ModelZipName = "vosk-model-small-ar-0.3.zip";
    private const string DownloadUrl = "https://alphacephei.com/vosk/models/vosk-model-small-ar-0.3.zip";

    public static string ModelsRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Qayd", "speech-models");

    public static string ModelPath => Path.Combine(ModelsRoot, ModelFolderName);

    public static bool IsModelReady() =>
        Directory.Exists(ModelPath)
        && File.Exists(Path.Combine(ModelPath, "am", "final.mdl"));

    public static async Task<bool> EnsureModelAsync(IProgress<SpeechPackInstallProgress>? progress, CancellationToken cancellationToken)
    {
        if (IsModelReady())
        {
            Report(progress, "نموذج التعرف الصوتي العربي جاهز.", 100, "اكتمل");
            return true;
        }

        Directory.CreateDirectory(ModelsRoot);
        var zipPath = Path.Combine(ModelsRoot, ModelZipName);
        var tempExtract = Path.Combine(ModelsRoot, $"extract-{Guid.NewGuid():N}");

        try
        {
            Report(progress, "جاري تنزيل نموذج التعرف الصوتي العربي (~45 م.ب)...", 8, "الخطوة 1 من 3");
            if (!await DownloadModelAsync(zipPath, progress, cancellationToken))
            {
                Report(progress, "تعذر تنزيل نموذج التعرف الصوتي. تحقق من اتصال الإنترنت.", 100, "تعذر التثبيت");
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            Report(progress, "جاري فك ضغط النموذج...", 88, "الخطوة 2 من 3");
            Directory.CreateDirectory(tempExtract);
            ZipFile.ExtractToDirectory(zipPath, tempExtract, overwriteFiles: true);

            var extractedModel = Path.Combine(tempExtract, ModelFolderName);
            if (!Directory.Exists(extractedModel))
            {
                var candidate = Directory.GetDirectories(tempExtract).FirstOrDefault();
                extractedModel = candidate ?? tempExtract;
            }

            if (Directory.Exists(ModelPath))
                Directory.Delete(ModelPath, recursive: true);

            Directory.Move(extractedModel, ModelPath);

            Report(progress, "جاري التحقق من النموذج...", 96, "الخطوة 3 من 3");
            if (!IsModelReady())
            {
                Report(progress, "تعذر التحقق من نموذج التعرف الصوتي بعد التنزيل.", 100, "تعذر التثبيت");
                return false;
            }

            Report(progress, "تم تجهيز نموذج التعرف الصوتي العربي بنجاح.", 100, "اكتمل");
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Report(progress, $"تعذر تجهيز نموذج التعرف الصوتي: {ex.Message}", 100, "تعذر التثبيت");
            return false;
        }
        finally
        {
            TryDelete(zipPath);
            TryDeleteDirectory(tempExtract);
        }
    }

    public static void Reset()
    {
        TryDeleteDirectory(ModelPath);
        TryDelete(Path.Combine(ModelsRoot, ModelZipName));
    }

    private static async Task<bool> DownloadModelAsync(string zipPath, IProgress<SpeechPackInstallProgress>? progress, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        using var response = await client.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? 0;
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var output = File.Create(zipPath);

        var buffer = new byte[1024 * 128];
        long downloaded = 0;
        int read;

        while ((read = await input.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            downloaded += read;

            if (total > 0)
            {
                var pct = 8 + downloaded * 78.0 / total;
                var mb = downloaded / (1024.0 * 1024.0);
                var totalMb = total / (1024.0 * 1024.0);
                Report(progress, $"جاري التنزيل... {mb:0.0} / {totalMb:0.0} م.ب", pct, "الخطوة 1 من 3");
            }
            else
            {
                var mb = downloaded / (1024.0 * 1024.0);
                Report(progress, $"جاري التنزيل... {mb:0.0} م.ب", 45, "الخطوة 1 من 3");
            }
        }

        return File.Exists(zipPath) && new FileInfo(zipPath).Length > 1024 * 1024;
    }

    private static void Report(IProgress<SpeechPackInstallProgress>? progress, string message, double percent, string? step = null) =>
        progress?.Report(new SpeechPackInstallProgress
        {
            Message = message,
            Percent = Math.Clamp(percent, 0, 100),
            StepLabel = step
        });

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignore
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // ignore
        }
    }
}
