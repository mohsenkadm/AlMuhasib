using System.Windows;
using System.Windows.Media;

namespace AlMuhasib.Shared.Services;

/// <summary>
/// Visual theme knobs for the three A4 invoice print templates.
/// Detailed layouts will be finalized later; ids stay stable: Classic / Compact / Modern.
/// </summary>
public sealed class InvoiceA4TemplateTheme
{
    public string Id { get; init; } = "Classic";
    public Color Primary { get; init; }
    public Color Dark { get; init; }
    public Color Accent { get; init; }
    public Color LightBg { get; init; }
    public Color Border { get; init; }
    public Thickness PagePadding { get; init; }
    public Thickness CompactPagePadding { get; init; }
    public bool ForceCompactMetrics { get; init; }
    public bool UseSolidBanner { get; init; } = true;
    public bool ShowAccentLine { get; init; } = true;
    public double TitleFontSize { get; init; } = 24;
    public double CompactTitleFontSize { get; init; } = 19;

    public static InvoiceA4TemplateTheme Resolve(string? templateId) =>
        (templateId ?? "Classic").Trim() switch
        {
            "Compact" => Compact,
            "Modern" => Modern,
            _ => Classic
        };

    public static InvoiceA4TemplateTheme Classic { get; } = new()
    {
        Id = "Classic",
        Primary = Color.FromRgb(0x15, 0x65, 0xC0),
        Dark = Color.FromRgb(0x0D, 0x47, 0xA1),
        Accent = Color.FromRgb(0xE6, 0x51, 0x00),
        LightBg = Color.FromRgb(0xF5, 0xF7, 0xFA),
        Border = Color.FromRgb(0xE0, 0xE0, 0xE0),
        PagePadding = new Thickness(32, 10, 32, 24),
        CompactPagePadding = new Thickness(22, 10, 22, 14),
        ForceCompactMetrics = false,
        UseSolidBanner = true,
        ShowAccentLine = true,
        TitleFontSize = 24,
        CompactTitleFontSize = 19
    };

    public static InvoiceA4TemplateTheme Compact { get; } = new()
    {
        Id = "Compact",
        Primary = Color.FromRgb(0x00, 0x79, 0x6B),
        Dark = Color.FromRgb(0x00, 0x4D, 0x40),
        Accent = Color.FromRgb(0x00, 0x89, 0x7B),
        LightBg = Color.FromRgb(0xE0, 0xF2, 0xF1),
        Border = Color.FromRgb(0xB2, 0xDF, 0xDB),
        PagePadding = new Thickness(20, 10, 20, 12),
        CompactPagePadding = new Thickness(16, 10, 16, 10),
        ForceCompactMetrics = true,
        UseSolidBanner = true,
        ShowAccentLine = true,
        TitleFontSize = 18,
        CompactTitleFontSize = 16
    };

    public static InvoiceA4TemplateTheme Modern { get; } = new()
    {
        Id = "Modern",
        Primary = Color.FromRgb(0x26, 0x32, 0x38),
        Dark = Color.FromRgb(0x1C, 0x25, 0x29),
        Accent = Color.FromRgb(0x00, 0xAC, 0xC1),
        LightBg = Color.FromRgb(0xFA, 0xFA, 0xFA),
        Border = Color.FromRgb(0xCF, 0xD8, 0xDC),
        PagePadding = new Thickness(36, 10, 36, 28),
        CompactPagePadding = new Thickness(28, 10, 28, 18),
        ForceCompactMetrics = false,
        UseSolidBanner = false,
        ShowAccentLine = true,
        TitleFontSize = 22,
        CompactTitleFontSize = 18
    };
}
