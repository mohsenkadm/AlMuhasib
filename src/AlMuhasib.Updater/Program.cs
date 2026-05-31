using System.Diagnostics;
using System.IO.Compression;

namespace AlMuhasib.Updater;

/// <summary>
/// Applies a downloaded update package while the main app is closed, then restarts it.
/// Database migrations run automatically on the next main-app startup.
/// </summary>
internal static class Program
{
    private const int ProcessWaitMs = 120_000;
    private const int FileRetryCount = 8;
    private static readonly string[] PreservedFileNames =
    [
        "appsettings.json",
        "appsettings.Development.json"
    ];

    public static int Main(string[] args)
    {
        try
        {
            var options = UpdateOptions.Parse(args);
            if (options is null)
            {
                Console.Error.WriteLine(UpdateOptions.Usage);
                return 1;
            }

            WaitForProcess(options.ProcessId);

            var preserved = PreserveUserFiles(options.InstallDirectory);
            var stagingDir = Path.Combine(options.InstallDirectory, "_update_staging");
            if (Directory.Exists(stagingDir))
                Directory.Delete(stagingDir, true);
            Directory.CreateDirectory(stagingDir);

            ZipFile.ExtractToDirectory(options.PackagePath, stagingDir, overwriteFiles: true);
            CopyDirectory(stagingDir, options.InstallDirectory);
            RestorePreservedFiles(options.InstallDirectory, preserved);

            try
            {
                Directory.Delete(stagingDir, true);
                if (File.Exists(options.PackagePath))
                    File.Delete(options.PackagePath);
            }
            catch
            {
                // non-fatal cleanup
            }

            var mainExe = Path.Combine(options.InstallDirectory, options.MainExecutable);
            if (!File.Exists(mainExe))
            {
                Console.Error.WriteLine($"Main executable not found: {mainExe}");
                return 2;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = mainExe,
                WorkingDirectory = options.InstallDirectory,
                UseShellExecute = true
            });

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 99;
        }
    }

    private static void WaitForProcess(int pid)
    {
        if (pid <= 0) return;

        try
        {
            var proc = Process.GetProcessById(pid);
            if (!proc.HasExited)
                proc.WaitForExit(ProcessWaitMs);
        }
        catch (ArgumentException)
        {
            // already exited
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
            if (PreservedFileNames.Contains(fileName, StringComparer.OrdinalIgnoreCase))
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
            string? main = "AlMuhasib.exe";
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
                MainExecutable = main ?? "AlMuhasib.exe",
                ProcessId = pid
            };
        }
    }
}
