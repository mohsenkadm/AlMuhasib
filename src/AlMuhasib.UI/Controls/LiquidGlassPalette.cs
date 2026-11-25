using System.Windows.Media;

namespace AlMuhasib.UI.Controls;

/// <summary>السطح الذي يوضع عليه الزجاج، ويحدد أي لوحة ألوان تُستخدم.</summary>
public enum LiquidGlassTone
{
    /// <summary>فوق خلفية الصفحة (أزرار الشريط تحت التبويبات).</summary>
    Standard,

    /// <summary>فوق البطاقة الزرقاء في الصفحة الرئيسية.</summary>
    OnBlue,

    /// <summary>سطح عائم فوق محتوى متغيّر (إشعار الأسفل).</summary>
    Notification,

    /// <summary>شريط رأس الجداول: زجاج صافٍ شفاف فوق طبقة جوّية مموّهة.</summary>
    Header
}

/// <summary>
/// ألوان طبقات الزجاج السائل. كل الألوان تحمل شفافيتها داخلها (ARGB).
/// </summary>
public sealed class LiquidGlassPalette
{
    /// <summary>تلوين الجسم. شبه صافٍ: الزجاج يُقرأ من الحافة لا من التعبئة.</summary>
    public Color Tint { get; init; }
    public Color TintHover { get; init; }
    public Color TintPressed { get; init; }
    public Color TintActive { get; init; }

    /// <summary>الخط الخارجي الرفيع الذي يفصل الزجاج عن الخلفية.</summary>
    public Color OuterStroke { get; init; }
    public Color OuterStrokeHover { get; init; }
    public Color FocusStroke { get; init; }

    /// <summary>الحلقة اللامعة داخل الخط الخارجي مباشرة: خفيفة وطبيعية وليست معدنية.</summary>
    public Color SpecularStart { get; init; }
    public Color SpecularMid { get; init; }
    public Color SpecularEnd { get; init; }

    /// <summary>تظليل داخلي يذوب نحو الوسط ويعطي إحساس السماكة.</summary>
    public Color InnerShade { get; init; }
    public double InnerShadeWidth { get; init; } = 4;

    /// <summary>ضوء ناعم داخل الحافة العلوية.</summary>
    public Color TopLight { get; init; }

    public Color ShadowColor { get; init; }
    public double ShadowOpacity { get; init; }
    public double ShadowBlur { get; init; }
    public double ShadowDepth { get; init; }

    /// <summary>نصف قطر تمويه الخلفية الحقيقية عند توفر مصدر لها.</summary>
    public double BackdropBlur { get; init; } = 14;
}

/// <summary>
/// المصدر الوحيد لألوان الزجاج في الوضعين. <c>ThemeService</c> ينشر هذه اللوحات كموارد،
/// و<see cref="LiquidGlassChrome"/> يرجع إلى النسخة الفاتحة إن لم يجد المورد.
/// </summary>
public static class LiquidGlassPalettes
{
    public static LiquidGlassPalette Create(LiquidGlassTone tone, bool isDark, bool isGold) => tone switch
    {
        LiquidGlassTone.OnBlue => isDark ? OnBlueDark() : OnBlueLight(),
        LiquidGlassTone.Notification => isDark ? NotificationDark(isGold) : NotificationLight(isGold),
        LiquidGlassTone.Header => isDark ? HeaderDark(isGold) : HeaderLight(isGold),
        _ => isDark ? StandardDark(isGold) : StandardLight(isGold)
    };

    // فوق صفحة فاتحة: صقيع بارد بحافة مقروءة وظل أوضح — يظهر زجاجياً فوق الأبيض والأزرق الفاتح.
    private static LiquidGlassPalette StandardLight(bool isGold) => new()
    {
        Tint = C(isGold ? "#48FFFBF2" : "#48F2F7FC"),
        TintHover = C(isGold ? "#66FFF8EC" : "#66FFFFFF"),
        TintPressed = C(isGold ? "#1AA08040" : "#1A3C5878"),
        TintActive = C(isGold ? "#38C9A24A" : "#385B8FD4"),
        OuterStroke = C(isGold ? "#99FFFFFF" : "#A6FFFFFF"),
        OuterStrokeHover = C(isGold ? "#B3FFFFFF" : "#CCFFFFFF"),
        FocusStroke = C(isGold ? "#CCB08A3A" : "#CC5B8FD4"),
        SpecularStart = C("#B3FFFFFF"),
        SpecularMid = C("#33FFFFFF"),
        SpecularEnd = C("#73FFFFFF"),
        InnerShade = C(isGold ? "#286A5020" : "#2A4A6688"),
        InnerShadeWidth = 4.5,
        TopLight = C("#73FFFFFF"),
        ShadowColor = C(isGold ? "#5A4418" : "#2C4058"),
        ShadowOpacity = 0.22,
        ShadowBlur = 12,
        ShadowDepth = 1.5
    };

    // فوق صفحة داكنة: تلوين أبيض خفيف وحافة بيضاء شفافة بدل خط أسود معدني.
    private static LiquidGlassPalette StandardDark(bool isGold) => new()
    {
        Tint = C("#12FFFFFF"),
        TintHover = C("#22FFFFFF"),
        TintPressed = C("#08FFFFFF"),
        TintActive = C(isGold ? "#2EC9A24A" : "#2E5B8FD4"),
        OuterStroke = C("#40FFFFFF"),
        OuterStrokeHover = C(isGold ? "#66F0D48A" : "#66FFFFFF"),
        FocusStroke = C(isGold ? "#CCE8C878" : "#CC90C4F0"),
        SpecularStart = C(isGold ? "#80FBE6B0" : "#80FFFFFF"),
        SpecularMid = C(isGold ? "#28E8C878" : "#28FFFFFF"),
        SpecularEnd = C(isGold ? "#59F2D896" : "#59FFFFFF"),
        InnerShade = C(isGold ? "#18F0D48A" : "#18FFFFFF"),
        InnerShadeWidth = 4,
        TopLight = C("#33FFFFFF"),
        ShadowColor = C("#000000"),
        ShadowOpacity = 0.28,
        ShadowBlur = 10,
        ShadowDepth = 1
    };

    // فوق الأزرق: صافٍ مع حدود بيضاء شفافة وتظليل داخلي خفيف.
    private static LiquidGlassPalette OnBlueLight() => new()
    {
        Tint = C("#12FFFFFF"),
        TintHover = C("#28FFFFFF"),
        TintPressed = C("#1404183A"),
        TintActive = C("#28FFFFFF"),
        OuterStroke = C("#59FFFFFF"),
        OuterStrokeHover = C("#80FFFFFF"),
        FocusStroke = C("#CCFFFFFF"),
        SpecularStart = C("#99FFFFFF"),
        SpecularMid = C("#33FFFFFF"),
        SpecularEnd = C("#73FFFFFF"),
        InnerShade = C("#2804183A"),
        InnerShadeWidth = 4,
        TopLight = C("#40FFFFFF"),
        ShadowColor = C("#04183A"),
        ShadowOpacity = 0.22,
        ShadowBlur = 12,
        ShadowDepth = 2,
        BackdropBlur = 14
    };

    private static LiquidGlassPalette OnBlueDark() => new()
    {
        Tint = C("#12FFFFFF"),
        TintHover = C("#24FFFFFF"),
        TintPressed = C("#08FFFFFF"),
        TintActive = C("#24FFFFFF"),
        OuterStroke = C("#4DFFFFFF"),
        OuterStrokeHover = C("#73FFFFFF"),
        FocusStroke = C("#CCFFFFFF"),
        SpecularStart = C("#80FFFFFF"),
        SpecularMid = C("#28FFFFFF"),
        SpecularEnd = C("#59FFFFFF"),
        InnerShade = C("#2200061A"),
        InnerShadeWidth = 4,
        TopLight = C("#33FFFFFF"),
        ShadowColor = C("#000000"),
        ShadowOpacity = 0.30,
        ShadowBlur = 12,
        ShadowDepth = 2,
        BackdropBlur = 14
    };

    // إشعار عائم: صقيع أخف وظل ناعم وتمويه أوضح للخلفية.
    private static LiquidGlassPalette NotificationLight(bool isGold) => new()
    {
        Tint = C(isGold ? "#73FFFBF2" : "#73FFFFFF"),
        TintHover = C(isGold ? "#73FFFBF2" : "#73FFFFFF"),
        TintPressed = C(isGold ? "#73FFFBF2" : "#73FFFFFF"),
        TintActive = C(isGold ? "#73FFFBF2" : "#73FFFFFF"),
        OuterStroke = C("#66FFFFFF"),
        OuterStrokeHover = C("#66FFFFFF"),
        FocusStroke = C(isGold ? "#CCB08A3A" : "#CC5B8FD4"),
        SpecularStart = C("#80FFFFFF"),
        SpecularMid = C("#26FFFFFF"),
        SpecularEnd = C("#59FFFFFF"),
        InnerShade = C(isGold ? "#1A6A5020" : "#1A3C5068"),
        InnerShadeWidth = 5,
        TopLight = C("#59FFFFFF"),
        ShadowColor = C(isGold ? "#5A4418" : "#2C4058"),
        ShadowOpacity = 0.20,
        ShadowBlur = 22,
        ShadowDepth = 3,
        BackdropBlur = 20
    };

    private static LiquidGlassPalette NotificationDark(bool isGold) => new()
    {
        Tint = C(isGold ? "#661A140A" : "#660C1430"),
        TintHover = C(isGold ? "#661A140A" : "#660C1430"),
        TintPressed = C(isGold ? "#661A140A" : "#660C1430"),
        TintActive = C(isGold ? "#661A140A" : "#660C1430"),
        OuterStroke = C("#40FFFFFF"),
        OuterStrokeHover = C("#40FFFFFF"),
        FocusStroke = C(isGold ? "#CCE8C878" : "#CC90C4F0"),
        SpecularStart = C(isGold ? "#80FBE6B0" : "#80FFFFFF"),
        SpecularMid = C(isGold ? "#28E8C878" : "#28FFFFFF"),
        SpecularEnd = C(isGold ? "#59F2D896" : "#59FFFFFF"),
        InnerShade = C(isGold ? "#18F0D48A" : "#18FFFFFF"),
        InnerShadeWidth = 5,
        TopLight = C("#33FFFFFF"),
        ShadowColor = C("#000000"),
        ShadowOpacity = 0.36,
        ShadowBlur = 22,
        ShadowDepth = 3,
        BackdropBlur = 20
    };

    // شريط رأس الجداول فاتح: صقيع خفيف جداً حتى يظهر الجو الأزرق خلفه شفّافاً مموّهاً.
    private static LiquidGlassPalette HeaderLight(bool isGold) => new()
    {
        Tint = C(isGold ? "#28FFFBF5" : "#22F5FAFF"),
        TintHover = C(isGold ? "#33FFFBF5" : "#33FFFFFF"),
        TintPressed = C("#14FFFFFF"),
        TintActive = C("#28FFFFFF"),
        OuterStroke = C("#B3FFFFFF"),
        OuterStrokeHover = C("#CCFFFFFF"),
        FocusStroke = C(isGold ? "#CCB08A3A" : "#CC5B8FD4"),
        SpecularStart = C("#CCFFFFFF"),
        SpecularMid = C("#40FFFFFF"),
        SpecularEnd = C("#8CFFFFFF"),
        InnerShade = C(isGold ? "#1C6A5020" : "#1C3C5878"),
        InnerShadeWidth = 4.5,
        TopLight = C("#8CFFFFFF"),
        ShadowColor = C(isGold ? "#5A4418" : "#2C4058"),
        ShadowOpacity = 0.20,
        ShadowBlur = 16,
        ShadowDepth = 2,
        BackdropBlur = 20
    };

    private static LiquidGlassPalette HeaderDark(bool isGold) => new()
    {
        Tint = C("#14FFFFFF"),
        TintHover = C("#28FFFFFF"),
        TintPressed = C("#0AFFFFFF"),
        TintActive = C("#22FFFFFF"),
        OuterStroke = C("#66FFFFFF"),
        OuterStrokeHover = C("#99FFFFFF"),
        FocusStroke = C(isGold ? "#CCE8C878" : "#CC90C4F0"),
        SpecularStart = C("#99FFFFFF"),
        SpecularMid = C("#28FFFFFF"),
        SpecularEnd = C("#66FFFFFF"),
        InnerShade = C(isGold ? "#18F0D48A" : "#18FFFFFF"),
        InnerShadeWidth = 5,
        TopLight = C("#40FFFFFF"),
        ShadowColor = C("#000000"),
        ShadowOpacity = 0.32,
        ShadowBlur = 18,
        ShadowDepth = 2,
        BackdropBlur = 22
    };

    private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex)!;
}
