using AlMuhasib.Core.Models;

namespace AlMuhasib.Core;

/// <summary>In-memory branding snapshot for print layout (refreshed on startup and after save).</summary>
public static class PrintBrandingProvider
{
    public static PrintBrandingSnapshot Current { get; private set; } = PrintBrandingSnapshot.Empty;

    public static void Update(PrintBrandingSnapshot snapshot) =>
        Current = snapshot ?? PrintBrandingSnapshot.Empty;
}
