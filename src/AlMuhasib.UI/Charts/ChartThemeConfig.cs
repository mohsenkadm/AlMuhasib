using AlMuhasib.Core.Interfaces.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AlMuhasib.UI.Charts;

/// <summary>
/// Global LiveCharts2 theme configuration for AlMuhasib.
/// Call <see cref="Apply"/> once at app startup.
/// </summary>
public static class ChartThemeConfig
{
    // ── Color Palette ───────────────────────────────────────
    public static readonly SKColor PrimaryBlue   = SKColor.Parse("#1565C0");
    public static readonly SKColor AccentCyan    = SKColor.Parse("#00ACC1");
    public static readonly SKColor SuccessGreen  = SKColor.Parse("#2E7D32");
    public static readonly SKColor DangerRed     = SKColor.Parse("#C62828");
    public static readonly SKColor Purple        = SKColor.Parse("#6A1B9A");
    public static readonly SKColor Teal          = SKColor.Parse("#00838F");
    public static readonly SKColor DeepIndigo     = SKColor.Parse("#7E57C2");
    public static readonly SKColor Indigo        = SKColor.Parse("#283593");
    public static readonly SKColor LightGreen    = SKColor.Parse("#558B2F");
    public static readonly SKColor Pink          = SKColor.Parse("#AD1457");

    public static readonly SKColor[] Palette =
    [
        PrimaryBlue, AccentCyan, SuccessGreen, DangerRed, Purple,
        Teal, DeepIndigo, Indigo, LightGreen, Pink
    ];

    // ── Theme-aware chart chrome (updated via ApplyTheme) ───
    public static SKColor GridLineColor { get; private set; } = SKColor.Parse("#F0F0F0");
    public static SKColor LabelColor { get; private set; } = SKColor.Parse("#757575");
    public static SKColor TooltipBg { get; private set; } = SKColors.White;
    public static SKColor GeometryFillColor { get; private set; } = SKColors.White;

    public static void ApplyTheme(bool isDark)
    {
        GridLineColor = SKColor.Parse(isDark ? "#2D3544" : "#F0F0F0");
        LabelColor = SKColor.Parse(isDark ? "#B0BEC5" : "#757575");
        TooltipBg = SKColor.Parse(isDark ? "#1E2430" : "#FFFFFF");
        GeometryFillColor = SKColor.Parse(isDark ? "#1E2430" : "#FFFFFF");
    }
    public const string FontFamily = "Segoe UI, Tahoma, Arial";
    public const float LabelSize   = 11f;
    public const float LegendSize  = 12f;

    public static SKTypeface ArabicTypeface { get; } =
        SKTypeface.FromFamilyName("Segoe UI")
        ?? SKTypeface.FromFamilyName("Tahoma")
        ?? SKTypeface.Default;

    public static SolidColorPaint CreateLabelPaint(SKColor? color = null) => new(color ?? LabelColor)
    {
        SKTypeface = ArabicTypeface
    };

    public static string FormatAmount(double value, string? suffix = "د.ع") =>
        suffix is null ? value.ToString("N0") : $"{value:N0} {suffix}";

    public static void Apply()
    {
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
        SeparatorsPaint = new SolidColorPaint { Color = GridLineColor, StrokeThickness = 1 },
        LabelsRotation = rotation,
        IsInverted = false,
        Padding = new LiveChartsCore.Drawing.Padding(6)
    };

    /// <summary>Creates a styled Y-axis with IQD currency formatter.</summary>
    public static Axis CreateYAxis(string? suffix = "د.ع") => new()
    {
        Labeler = v => FormatAmount(v, suffix),
        TextSize = LabelSize,
        LabelsPaint = CreateLabelPaint(),
        SeparatorsPaint = new SolidColorPaint { Color = GridLineColor, StrokeThickness = 1 },
        MinLimit = 0
    };

    // ── Series factories ────────────────────────────────────

    /// <summary>Styled ColumnSeries (bar chart) with rounded corners and gradient fill.</summary>
    public static ColumnSeries<decimal> Column(decimal[] values, string name, int colorIndex = 0) => new()
    {
        Values = values,
        Name = name,
        Fill = PalettePaint(colorIndex),
        Stroke = null,
        Rx = 4,
        Ry = 4,
        MaxBarWidth = 40,
        Padding = 8,
        AnimationsSpeed = TimeSpan.FromMilliseconds(800),
        EasingFunction = LiveChartsCore.EasingFunctions.BounceOut
    };

    /// <summary>Styled LineSeries with dot markers and gradient fill.</summary>
    public static LineSeries<decimal> Line(decimal[] values, string name, int colorIndex = 0) => new()
    {
        Values = values,
        Name = name,
        Stroke = PaletteStrokePaint(colorIndex, 3f),
        GeometryStroke = PaletteStrokePaint(colorIndex, 2f),
        GeometryFill = new SolidColorPaint(GeometryFillColor),
        GeometrySize = 8,
        Fill = PalettePaint(colorIndex, (byte)50),
        LineSmoothness = 0.65,
        AnimationsSpeed = TimeSpan.FromMilliseconds(800),
        EasingFunction = LiveChartsCore.EasingFunctions.QuadraticOut
    };

    /// <summary>Styled PieSeries slice.</summary>
    public static PieSeries<decimal> Pie(decimal value, string name, int colorIndex, bool isDoughnut = true) => new()
    {
        Values = [value],
        Name = name,
        Fill = PalettePaint(colorIndex),
        Stroke = null,
        InnerRadius = isDoughnut ? 60 : 0,
        HoverPushout = 3,
        AnimationsSpeed = TimeSpan.FromMilliseconds(800),
        EasingFunction = LiveChartsCore.EasingFunctions.QuadraticOut,
        DataLabelsSize = 0,
        DataLabelsPaint = null
    };

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
