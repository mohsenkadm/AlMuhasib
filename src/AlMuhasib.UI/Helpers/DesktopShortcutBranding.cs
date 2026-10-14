using System.IO;
using System.Runtime.InteropServices;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// Updates desktop / Start Menu shortcuts after Duhok (Express) branding is activated on first run.
/// No-op for existing customers and non-Duhok installs.
/// </summary>
public static class DesktopShortcutBranding
{
    public static void ApplyExpressShortcutsIfNeeded(bool isDuhokBranch)
    {
        if (!isDuhokBranch)
            return;

        try
        {
            var exePath = Environment.ProcessPath
                ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AlMuhasib.exe");
            if (!File.Exists(exePath))
                return;

            var iconPath = EnsureExpressIconOnDisk();
            var workingDir = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;
            var displayName = ProductBranding.ExpressNameAr;

            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var desktopNew = Path.Combine(desktopDir, displayName + ".lnk");
            CreateShortcut(desktopNew, exePath, workingDir, iconPath, displayName);
            TryDeleteFile(Path.Combine(desktopDir, "قيد.lnk"));

            var programs = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            var startMenuDir = Path.Combine(programs, displayName);
            Directory.CreateDirectory(startMenuDir);
            var startMenuNew = Path.Combine(startMenuDir, displayName + ".lnk");
            CreateShortcut(startMenuNew, exePath, workingDir, iconPath, displayName);

            // Remove legacy Qayd Start Menu shortcuts / empty group folders only.
            foreach (var oldPath in new[]
                     {
                         Path.Combine(programs, "قيد", "قيد.lnk"),
                         Path.Combine(programs, "Qayd", "قيد.lnk"),
                         Path.Combine(programs, "قيد.lnk")
                     })
            {
                TryDeleteFile(oldPath);
                var oldDir = Path.GetDirectoryName(oldPath);
                if (!string.IsNullOrWhiteSpace(oldDir)
                    && !string.Equals(oldDir, programs, StringComparison.OrdinalIgnoreCase))
                {
                    TryDeleteEmptyDirectory(oldDir);
                }
            }
        }
        catch
        {
            // Shortcut updates must never block first-run setup.
        }
    }

    /// <summary>
    /// Prefer a writable copy beside the exe; fall back to Assets\Brand when Program Files blocks copy.
    /// Returns null when no icon file is available (shortcut is created without IconLocation).
    /// </summary>
    private static string? EnsureExpressIconOnDisk()
    {
        var dest = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "express-icon.ico");
        if (File.Exists(dest))
            return dest;

        var embedded = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Brand", "express-icon.ico");
        if (File.Exists(embedded))
        {
            try
            {
                File.Copy(embedded, dest, overwrite: true);
                if (File.Exists(dest))
                    return dest;
            }
            catch
            {
                // Program Files / ACL may block writing beside the exe.
            }

            return embedded;
        }

        return File.Exists(dest) ? dest : null;
    }

    private static void CreateShortcut(
        string shortcutPath,
        string exePath,
        string workingDir,
        string? iconPath,
        string description)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType is null)
            return;

        dynamic shell = Activator.CreateInstance(shellType)!;
        try
        {
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = workingDir;
            shortcut.Description = description;
            if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
                shortcut.IconLocation = iconPath;
            shortcut.Save();
            Marshal.FinalReleaseComObject(shortcut);
        }
        finally
        {
            Marshal.FinalReleaseComObject(shell);
        }
    }

    private static void TryDeleteFile(string path)
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

    private static void TryDeleteEmptyDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path) && !Directory.EnumerateFileSystemEntries(path).Any())
                Directory.Delete(path);
        }
        catch
        {
            // ignore
        }
    }
}
