using System.Reflection;

namespace AlMuhasib.UI.Helpers;

/// <summary>Application display name and version.</summary>
public static class AppInfo
{
    public static string AppNameAr => ProductBranding.AppNameAr;
    public static string AppNameEn => ProductBranding.AppNameEn;
    public const string TaglineAr = "حلول محاسبة وتجارة متكاملة";

    public static string Version
    {
        get
        {
            var asm = Assembly.GetExecutingAssembly();
            var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informational))
            {
                var plus = informational.IndexOf('+', StringComparison.Ordinal);
                return plus > 0 ? informational[..plus] : informational;
            }

            return asm.GetName().Version?.ToString(3) ?? "1.0.0";
        }
    }

    public static string VersionLabel => $"الإصدار {Version}";
}
