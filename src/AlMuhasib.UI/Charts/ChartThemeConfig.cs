using System.IO;
using System.Windows.Media;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;

namespace AlMuhasib.UI.Charts;

/// <summary>
/// Global LiveCharts2 theme configuration for AlMuhasib.
/// Call <see cref="Apply"/> once at app startup.
/// </summary>
public static class ChartThemeConfig
{
    // ── Color Palette (modern, balanced) ────────────────────
    public static readonly SKColor PrimaryBlue   = SKColor.Parse("#2563EB");
    public static readonly SKColor AccentCyan    = SKColor.Parse("#06B6D4");
    public static readonly SKColor SuccessGreen  = SKColor.Parse("#10B981");
    public static readonly SKColor DangerRed     = SKColor.Parse("#F43F5E");
    public static readonly SKColor Purple        = SKColor.Parse("#8B5CF6");
    public static readonly SKColor Teal          = SKColor.Parse("#14B8A6");
    public static readonly SKColor DeepIndigo    = SKColor.Parse("#6366F1");
    public static readonly SKColor Indigo        = SKColor.Parse("#4F46E5");
    public static readonly SKColor LightGreen    = SKColor.Parse("#84CC16");
    public static readonly SKColor Pink          = SKColor.Parse("#EC4899");

    public static readonly SKColor[] Palette =
    [
        PrimaryBlue, AccentCyan, SuccessGreen, Purple, Teal,
        DangerRed, DeepIndigo, LightGreen, Pink, Indigo
    ];

    // ── Theme-aware chart chrome (updated via ApplyTheme) ───
    public static SKColor GridLineColor { get; private set; } = SKColor.Parse("#E8EEF6");
    public static SKColor LabelColor { get; private set; } = SKColor.Parse("#64748B");
    public static SKColor ChartSurfaceColor { get; private set; } = SKColor.Parse("#FFFFFF");
    public static SKColor TooltipBg { get; private set; } = SKColors.White;
    public static SKColor GeometryFillColor { get; private set; } = SKColors.White;

    public static SolidColorPaint TooltipBackgroundPaint { get; private set; } = new(SKColors.White);
    public static SolidColorPaint TooltipTextPaint { get; private set; } = new(SKColor.Parse("#212121"));
    public static SolidColorPaint LegendTextPaint { get; private set; } = new(SKColor.Parse("#757575"));

    private static bool _isDark;

    public static void ApplyTheme(bool isDark)
    {
        _isDark = isDark;
        GridLineColor = SKColor.Parse(isDark ? "#2A3558" : "#E8EEF6");
        LabelColor = SKColor.Parse(isDark ? "#94A3B8" : "#64748B");
        // Match card surface so doughnut gaps / markers blend cleanly
        ChartSurfaceColor = SKColor.Parse(isDark ? "#121B42" : "#FFFFFF");
        TooltipBg = SKColor.Parse(isDark ? "#0F172A" : "#FFFFFF");
        GeometryFillColor = ChartSurfaceColor;
        EnsurePaints();
    }

    /// <summary>Soft vertical fade used under area/line charts.</summary>
    public static LinearGradientPaint CreateAreaFill(SKColor accent, byte topAlpha, byte bottomAlpha = 8)
        => new(
            accent.WithAlpha(topAlpha),
            accent.WithAlpha(bottomAlpha),
            new SKPoint(0.5f, 0f),
            new SKPoint(0.5f, 1f));

    public static SolidColorPaint CreateGridPaint() => new(GridLineColor)
    {
        StrokeThickness = 1,
        PathEffect = new DashEffect([4, 6])
    };

    public static DrawMarginFrame CreateDrawMarginFrame() => new()
    {
        Fill = null,
        Stroke = new SolidColorPaint(GridLineColor) { StrokeThickness = 1 }
    };

    public static void EnsurePaints()
    {
        TooltipBackgroundPaint = new SolidColorPaint(TooltipBg);
        TooltipTextPaint = new SolidColorPaint(SKColor.Parse(_isDark ? "#FFFFFF" : "#212121"))
        {
            SKTypeface = ArabicTypeface
        };
        LegendTextPaint = CreateLabelPaint();
    }
    public const string FontFamily = "Cairo, Segoe UI, Tahoma, Arial";
    public const float LabelSize   = 11f;
    public const float LegendSize  = 12f;

    public static SKTypeface ArabicTypeface { get; private set; } = ResolveArabicTypeface();

    private static SKTypeface ResolveArabicTypeface()
    {
        try
        {
            var fontsDir = AppFontBootstrap.FontsDirectory ?? AppFontBootstrap.ResolveFontsDirectory();
            if (!string.IsNullOrEmpty(fontsDir))
            {
                var regular = Path.Combine(fontsDir, "Cairo-Regular.ttf");
                if (File.Exists(regular))
                {
                    var fromFile = SKTypeface.FromFile(regular);
                    if (fromFile is not null)
                        return fromFile;
                }
            }
        }
        catch
        {
            // fall through
        }

        return SKTypeface.FromFamilyName("Cairo")
            ?? SKTypeface.FromFamilyName("Segoe UI")
            ?? SKTypeface.FromFamilyName("Tahoma")
            ?? SKTypeface.Default;
    }

    /// <summary>Reload typeface after <see cref="AppFontBootstrap.Apply"/> so charts use Cairo.</summary>
    public static void RefreshArabicTypeface()
    {
        ArabicTypeface = ResolveArabicTypeface();
        EnsurePaints();
    }

    public static SolidColorPaint CreateLabelPaint(SKColor? color = null) => new(color ?? LabelColor)
    {
        SKTypeface = ArabicTypeface
    };

    public static string FormatAmount(double value, string? suffix = "د.ع") =>
        suffix is null ? value.ToString("N0") : $"{value:N0} {suffix}";

    public static void Apply()
    {
        RefreshArabicTypeface();

        LiveCharts.Configure(settings =>
        {
            settings
                .AddSkiaSharp()
                .AddDefaultMappers();
        });
    }

    // ── Helper: build a SolidColorPaint from palette index ──
    public static SolidColorPaint PalettePaint(int index, byte? alpha = null)
    {
        var c = Palette[index % Palette.Length];
        if (alpha.HasValue) c = c.WithAlpha(alpha.Value);
        return new SolidColorPaint(c);
    }

    public static SolidColorPaint PaletteStrokePaint(int index, float strokeWidth)
        => new(Palette[index % Palette.Length], strokeWidth);

    // ── Chart axis factories ────────────────────────────────

    /// <summary>Creates a styled X-axis with labels.</summary>
    public static Axis CreateXAxis(string[]? labels = null, float rotation = 0) => new()
    {
        Labels = labels,
        TextSize = LabelSize,
        LabelsPaint = CreateLabelPaint(),
        SeparatorsPaint = null,
        LabelsRotation = rotation,
        IsInverted = false,
        Padding = new LiveChartsCore.Drawing.Padding(4, 8, 4, 0)
    };

    /// <summary>Creates a styled Y-axis with IQD currency formatter.</summary>
    public static Axis CreateYAxis(string? suffix = "د.ع") => new()
    {
        Labeler = v => FormatAmount(v, suffix),
        TextSize = LabelSize,
        LabelsPaint = CreateLabelPaint(),
        SeparatorsPaint = CreateGridPaint(),
        MinLimit = 0,
        Padding = new LiveChartsCore.Drawing.Padding(0, 0, 8, 0)
    };

    // ── Series factories ────────────────────────────────────

    /// <summary>Styled ColumnSeries (bar chart) with rounded corners and soft gradient fill.</summary>
    public static ColumnSeries<decimal> Column(decimal[] values, string name, int colorIndex = 0)
    {
        var color = Palette[colorIndex % Palette.Length];
        return new ColumnSeries<decimal>
        {
            Values = values,
            Name = name,
            Fill = CreateAreaFill(color, (byte)(_isDark ? 200 : 230), (byte)(_isDark ? 90 : 120)),
            Stroke = null,
            Rx = 8,
            Ry = 8,
            MaxBarWidth = 36,
            Padding = 10,
            AnimationsSpeed = TimeSpan.FromMilliseconds(700),
            EasingFunction = LiveChartsCore.EasingFunctions.CubicOut
        };
    }

    /// <summary>Styled LineSeries with soft gradient area and refined markers.</summary>
    public static LineSeries<decimal> Line(decimal[] values, string name, int colorIndex = 0)
    {
        var color = Palette[colorIndex % Palette.Length];
        return new LineSeries<decimal>
        {
            Values = values,
            Name = name,
            Stroke = new SolidColorPaint(color, 2.8f),
            GeometryStroke = new SolidColorPaint(color, 2f),
            GeometryFill = new SolidColorPaint(GeometryFillColor),
            GeometrySize = 6,
            Fill = CreateAreaFill(color, (byte)(_isDark ? 95 : 55), 6),
            LineSmoothness = 0.78,
            AnimationsSpeed = TimeSpan.FromMilliseconds(700),
            EasingFunction = LiveChartsCore.EasingFunctions.CubicOut
        };
    }

    /// <summary>Compact sparkline for KPI cards (edge fade, no markers).</summary>
    public static LineSeries<decimal> Sparkline(decimal[] values, SKColor accent) => new()
    {
        Values = values,
        Name = string.Empty,
        Stroke = new SolidColorPaint(accent, 2.4f),
        GeometrySize = 0,
        GeometryStroke = null,
        GeometryFill = null,
        Fill = CreateAreaFill(accent, (byte)(_isDark ? 90 : 58), 4),
        LineSmoothness = 0.82,
        AnimationsSpeed = TimeSpan.FromMilliseconds(500),
        EasingFunction = LiveChartsCore.EasingFunctions.CubicOut,
        IsHoverable = false
    };

    /// <summary>Hidden axes for sparkline charts inside KPI cards.</summary>
    public static Axis CreateSparklineAxis(bool isY = false) => new()
    {
        IsVisible = false,
        LabelsPaint = null,
        SeparatorsPaint = null,
        Padding = new LiveChartsCore.Drawing.Padding(isY ? 2 : 0, isY ? 4 : 2, isY ? 2 : 0, isY ? 2 : 0)
    };

    public static SKColor BrushToSkColor(Brush? brush, SKColor fallback)
    {
        if (brush is SolidColorBrush solid)
        {
            var c = solid.Color;
            return new SKColor(c.R, c.G, c.B, c.A);
        }
        return fallback;
    }

    /// <summary>Styled PieSeries slice with soft ring separation.</summary>
    public static PieSeries<decimal> Pie(decimal value, string name, int colorIndex, bool isDoughnut = true)
    {
        var color = Palette[colorIndex % Palette.Length];
        return new PieSeries<decimal>
        {
            Values = [value],
            Name = name,
            Fill = new RadialGradientPaint(
                color.WithAlpha(255),
                color.WithAlpha((byte)(_isDark ? 180 : 210))),
            Stroke = new SolidColorPaint(ChartSurfaceColor) { StrokeThickness = 3.5f },
            InnerRadius = isDoughnut ? 72 : 0,
            MaxRadialColumnWidth = 40,
            HoverPushout = 10,
            AnimationsSpeed = TimeSpan.FromMilliseconds(700),
            EasingFunction = LiveChartsCore.EasingFunctions.CubicOut,
            DataLabelsSize = 0,
            DataLabelsPaint = null
        };
    }

    /// <summary>
    /// Builds a doughnut pie series from NameAmountPoint list.
    /// Groups beyond <paramref name="maxSlices"/> into "أخرى".
    /// </summary>
    public static ISeries[] PieFromNameAmount(IList<NameAmountPoint> data, bool isDoughnut = true, int maxSlices = 8)
    {
        if (data.Count == 0) return [];

        var ordered = data.OrderByDescending(d => d.Amount).ToList();
        var slices = new List<(string Name, decimal Amount)>();

        for (int i = 0; i < ordered.Count && i < maxSlices; i++)
            slices.Add((ordered[i].Name, ordered[i].Amount));

        if (ordered.Count > maxSlices)
        {
            var rest = ordered.Skip(maxSlices).Sum(d => d.Amount);
            slices.Add(("أخرى", rest));
        }

        return slices.Select((s, i) => (ISeries)Pie(s.Amount, s.Name, i, isDoughnut)).ToArray();
    }
}
