using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace AlMuhasib.UI.Services;

/// <summary>
/// Ensures the bundled Cairo Arabic font is available in-process and (when possible)
/// registered for the current Windows user after install or auto-update.
/// </summary>
public static class AppFontBootstrap
{
    private const uint FrPrivate = 0x10;

    private static readonly (string FileName, string RegistryValue)[] FontFiles =
    [
        ("Cairo-Regular.ttf", "Cairo (TrueType)"),
        ("Cairo-Medium.ttf", "Cairo Medium (TrueType)"),
        ("Cairo-SemiBold.ttf", "Cairo SemiBold (TrueType)"),
        ("Cairo-Bold.ttf", "Cairo Bold (TrueType)")
    ];

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int AddFontResourceExW(string lpszFilename, uint fl, IntPtr pdv);

    public static string? FontsDirectory { get; private set; }

    public static void Apply()
    {
        try
        {
            var fontsDir = ResolveFontsDirectory();
            if (fontsDir is null)
                return;

            FontsDirectory = fontsDir;
            RegisterPrivateFonts(fontsDir);
            TryInstallForCurrentUser(fontsDir);
            ApplyWpfFontFamily(fontsDir);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppFontBootstrap] {ex.Message}");
        }
    }

    public static string? ResolveFontsDirectory()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Assets", "Fonts"),
            Path.Combine(baseDir, "Fonts")
        };

        foreach (var dir in candidates)
        {
            if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "Cairo-Regular.ttf")))
                return dir;
        }

        return null;
    }

    private static void RegisterPrivateFonts(string fontsDir)
    {
        foreach (var (fileName, _) in FontFiles)
        {
            var path = Path.Combine(fontsDir, fileName);
            if (!File.Exists(path))
                continue;

            // Process-private registration: works without elevation and covers WPF + GDI.
            _ = AddFontResourceExW(path, FrPrivate, IntPtr.Zero);
        }
    }

    /// <summary>
    /// Per-user font install (Windows 10+) so Cairo survives after updates without admin rights.
    /// </summary>
    private static void TryInstallForCurrentUser(string fontsDir)
    {
        var userFontsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "Windows", "Fonts");
        Directory.CreateDirectory(userFontsDir);

        using var fontsKey = Registry.CurrentUser.CreateSubKey(
            @"Software\Microsoft\Windows NT\CurrentVersion\Fonts", writable: true);

        foreach (var (fileName, registryValue) in FontFiles)
        {
            var source = Path.Combine(fontsDir, fileName);
            if (!File.Exists(source))
                continue;

            var dest = Path.Combine(userFontsDir, fileName);
            try
            {
                var needsCopy = !File.Exists(dest)
                    || new FileInfo(source).Length != new FileInfo(dest).Length
                    || File.GetLastWriteTimeUtc(source) > File.GetLastWriteTimeUtc(dest);

                if (needsCopy)
                    File.Copy(source, dest, overwrite: true);

                fontsKey?.SetValue(registryValue, dest);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppFontBootstrap] install {fileName}: {ex.Message}");
            }
        }
    }

    private static void ApplyWpfFontFamily(string fontsDir)
    {
        if (Application.Current?.Resources is null)
            return;

        // Directory-based family so Regular/Medium/SemiBold/Bold resolve by FontWeight.
        var baseUri = new Uri(
            Path.GetFullPath(fontsDir).TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar,
            UriKind.Absolute);
        var family = new FontFamily(baseUri, "./#Cairo");

        Application.Current.Resources["AppFont"] = family;
        Application.Current.Resources["ArabicFont"] = family;
    }
}
