using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace AlMuhasib.Updater;

/// <summary>
/// Applies a downloaded update package while the main app is closed, then restarts it.
/// Database migrations run automatically on the next main-app startup.
/// </summary>
internal static class Program
{
    private const int ProcessWaitMs = 180_000;
    private const int FileUnlockWaitMs = 60_000;
    private const int FileRetryCount = 20;
    private const string MainExecutableName = "AlMuhasib.exe";

    private static readonly string[] PreservedFileNames =
    [
        "appsettings.json",
        "appsettings.Development.json"
    ];

    public static int Main(string[] args)
    {
        var logPath = string.Empty;
        try
        {
            var options = UpdateOptions.Parse(args);
            if (options is null)
            {
                Console.Error.WriteLine(UpdateOptions.Usage);
                return 1;
            }

            logPath = Path.Combine(options.InstallDirectory, "update.log");
            Log(logPath, $"Starting update. Package={options.PackagePath} PID={options.ProcessId}");

            WaitForProcess(options.ProcessId);
            WaitForApplicationProcessesToExit(options.InstallDirectory, Process.GetCurrentProcess().Id);
            WaitForFileUnlock(Path.Combine(options.InstallDirectory, MainExecutableName), FileUnlockWaitMs);

            var preserved = PreserveUserFiles(options.InstallDirectory);
            var stagingDir = Path.Combine(options.InstallDirectory, "_update_staging");
            if (Directory.Exists(stagingDir))
                Directory.Delete(stagingDir, true);
            Directory.CreateDirectory(stagingDir);

            ZipFile.ExtractToDirectory(options.PackagePath, stagingDir, overwriteFiles: true);
            var payloadRoot = ResolvePayloadRoot(stagingDir);
            Log(logPath, $"Payload root: {payloadRoot}");

            if (!File.Exists(Path.Combine(payloadRoot, MainExecutableName)))
                throw new FileNotFoundException($"الحزمة لا تحتوي على {MainExecutableName}", MainExecutableName);

            CopyDirectory(payloadRoot, options.InstallDirectory);
            RestorePreservedFiles(options.InstallDirectory, preserved);

            try
            {
                Directory.Delete(stagingDir, true);
                if (File.Exists(options.PackagePath))
                    File.Delete(options.PackagePath);
            }
            catch (Exception ex)
            {
                Log(logPath, $"Cleanup warning: {ex.Message}");
            }

            var mainExe = Path.Combine(options.InstallDirectory, options.MainExecutable);
            if (!File.Exists(mainExe))
                throw new FileNotFoundException($"Main executable not found after update: {mainExe}", mainExe);

            Log(logPath, "Update applied successfully. Restarting main application.");
            StartMainApplication(mainExe, options.InstallDirectory, logPath);
            return 0;
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(logPath))
                Log(logPath, $"FAILED: {ex}");

            TryShowError("فشل تطبيق التحديث", ex.Message, logPath);
            Console.Error.WriteLine(ex);
            return 99;
        }
    }

    private static void StartMainApplication(string mainExe, string installDir, string logPath)
    {
        Exception? lastError = null;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                if (attempt > 0)
                    Thread.Sleep(750 * attempt);

                var startInfo = new ProcessStartInfo
                {
                    FileName = mainExe,
                    WorkingDirectory = installDir,
                    UseShellExecute = true
                };

                var proc = Process.Start(startInfo);
                if (proc is not null)
                {
                    Log(logPath, $"Main application started (PID {proc.Id}).");
                    return;
                }

                lastError = new InvalidOperationException("Process.Start returned null.");
            }
            catch (Exception ex)
            {
                lastError = ex;
                Log(logPath, $"Restart attempt {attempt + 1} failed: {ex.Message}");
            }
        }

        throw new InvalidOperationException("تعذّر إعادة تشغيل البرنامج بعد التحديث.", lastError);
    }

    /// <summary>
    /// If the zip contains a single root folder (common packaging mistake), use that folder as payload.
    /// </summary>
    private static string ResolvePayloadRoot(string stagingDir)
    {
        if (File.Exists(Path.Combine(stagingDir, MainExecutableName)))
            return stagingDir;

        var subDirs = Directory.GetDirectories(stagingDir);
        if (subDirs.Length == 1 && File.Exists(Path.Combine(subDirs[0], MainExecutableName)))
            return subDirs[0];

        foreach (var subDir in subDirs)
        {
            if (File.Exists(Path.Combine(subDir, MainExecutableName)))
                return subDir;
        }

        return stagingDir;
    }

    private static void WaitForProcess(int pid)
    {
        if (pid <= 0) return;

        try
        {
            var proc = Process.GetProcessById(pid);
            if (!proc.HasExited)
            {
                LogToConsole($"Waiting for process {pid} to exit...");
                proc.WaitForExit(ProcessWaitMs);
            }
        }
        catch (ArgumentException)
        {
            // already exited
        }
    }

    private static void WaitForApplicationProcessesToExit(string installDir, int updaterPid)
    {
        var mainExePath = Path.Combine(installDir, MainExecutableName);
        var elapsed = 0;

        while (elapsed < ProcessWaitMs)
        {
            var running = Process.GetProcessesByName("AlMuhasib")
                .Where(p =>
                {
                    if (p.Id == updaterPid) return false;
                    try
                    {
                        return string.Equals(p.MainModule?.FileName, mainExePath, StringComparison.OrdinalIgnoreCase);
                    }
                    catch
                    {
                        return true;
                    }
                })
                .ToList();

            if (running.Count == 0)
                return;

            foreach (var proc in running)
            {
                try
                {
                    if (!proc.HasExited)
                        proc.WaitForExit(Math.Max(1000, ProcessWaitMs - elapsed));
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    proc.Dispose();
                }
            }

            Thread.Sleep(500);
            elapsed += 500;
        }
    }

    private static void WaitForFileUnlock(string filePath, int maxWaitMs)
    {
        if (!File.Exists(filePath))
            return;

        var elapsed = 0;
        while (elapsed < maxWaitMs)
        {
            if (CanWriteFile(filePath))
                return;

            Thread.Sleep(250);
            elapsed += 250;
        }
    }

    private static bool CanWriteFile(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static Dictionary<string, byte[]> PreserveUserFiles(string installDir)
    {
        var map = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in PreservedFileNames)
        {
            var path = Path.Combine(installDir, name);
            if (File.Exists(path))
                map[name] = File.ReadAllBytes(path);
        }
        return map;
    }

    private static void RestorePreservedFiles(string installDir, Dictionary<string, byte[]> preserved)
    {
        foreach (var (name, bytes) in preserved)
            File.WriteAllBytes(Path.Combine(installDir, name), bytes);
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        foreach (var dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, dir);
            Directory.CreateDirectory(Path.Combine(targetDir, relative));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var fileName = Path.GetFileName(file);
            if (PreservedFileNames.Any(n => n.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                continue;

            var dest = Path.Combine(targetDir, relative);
            var destDir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);

            CopyFileWithRetry(file, dest);
        }
    }

    private static void CopyFileWithRetry(string source, string dest)
    {
        for (var i = 0; i < FileRetryCount; i++)
        {
            try
            {
                File.Copy(source, dest, overwrite: true);
                return;
            }
            catch (IOException) when (i < FileRetryCount - 1)
            {
                Thread.Sleep(400 * (i + 1));
            }
        }

        File.Copy(source, dest, overwrite: true);
    }

    private static void Log(string logPath, string message)
    {
        try
        {
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(logPath, line, Encoding.UTF8);
        }
        catch
        {
            // ignore
        }
    }

    private static void LogToConsole(string message) =>
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");

    private static void TryShowError(string title, string message, string logPath)
    {
        try
        {
            var text = string.IsNullOrWhiteSpace(logPath)
                ? message
                : $"{message}{Environment.NewLine}{Environment.NewLine}راجع ملف السجل:{Environment.NewLine}{logPath}";

            NativeMessageBox.MessageBox(default, text, title, 0x00000010); // MB_ICONERROR
        }
        catch
        {
            // ignore
        }
    }

    private sealed class UpdateOptions
    {
        public required string InstallDirectory { get; init; }
        public required string PackagePath { get; init; }
        public required string MainExecutable { get; init; }
        public int ProcessId { get; init; }

        public static string Usage =>
            """
            AlMuhasib Updater
            Usage:
              AlMuhasib.Updater.exe --install-dir <path> --package <zip> --pid <processId> [--main <exeName>]
            """;

        public static UpdateOptions? Parse(string[] args)
        {
            string? installDir = null;
            string? package = null;
            string? main = MainExecutableName;
            var pid = 0;

            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--install-dir" when i + 1 < args.Length:
                        installDir = args[++i];
                        break;
                    case "--package" when i + 1 < args.Length:
                        package = args[++i];
                        break;
                    case "--pid" when i + 1 < args.Length:
                        _ = int.TryParse(args[++i], out pid);
                        break;
                    case "--main" when i + 1 < args.Length:
                        main = args[++i];
                        break;
                }
            }

            if (string.IsNullOrWhiteSpace(installDir) || string.IsNullOrWhiteSpace(package) || !File.Exists(package))
                return null;

            return new UpdateOptions
            {
                InstallDirectory = Path.GetFullPath(installDir),
                PackagePath = Path.GetFullPath(package),
                MainExecutable = main ?? MainExecutableName,
                ProcessId = pid
            };
        }
    }

    private static class NativeMessageBox
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(nint hWnd, string text, string caption, uint type);
    }
}
