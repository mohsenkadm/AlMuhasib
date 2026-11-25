using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace AlMuhasib.UI.Controls;

/// <summary>
/// مادة الزجاج السائل المشتركة: توضع كطبقة سفلية تحت محتوى الزر أو الإشعار.
/// الطبقات من الأسفل: ظل خارج الشكل فقط، الخلفية الحقيقية مموّهة (اختياري)،
/// ثم جسم شبه صافٍ مع تظليل داخلي وضوء علوي وحلقة لامعة وخط خارجي رفيع.
/// </summary>
/// <remarks>
/// WPF لا يوفر تمويه ما خلف العنصر. عند ضبط <see cref="BackdropSourceProperty"/> نرسم نسخة
/// حيّة من العنصر الذي خلف الزجاج بـ <see cref="VisualBrush"/> ونموّهها، وهو المقابل العملي.
/// لا يوجد انكسار/انحناء للصورة عند الحواف؛ السماكة محاكاة بالتظليل الداخلي.
/// </remarks>
public sealed class LiquidGlassChrome : FrameworkElement
{
    public const string StandardPaletteKey = "LiquidGlassPalette";
    public const string OnBluePaletteKey = "LiquidGlassOnBluePalette";
    public const string NotificationPaletteKey = "LiquidGlassNotificationPalette";
    public const string HeaderPaletteKey = "LiquidGlassHeaderPalette";

    private static readonly LiquidGlassPalette FallbackStandard = LiquidGlassPalettes.Create(LiquidGlassTone.Standard, false, false);
    private static readonly LiquidGlassPalette FallbackOnBlue = LiquidGlassPalettes.Create(LiquidGlassTone.OnBlue, false, false);
    private static readonly LiquidGlassPalette FallbackNotification = LiquidGlassPalettes.Create(LiquidGlassTone.Notification, false, false);
    private static readonly LiquidGlassPalette FallbackHeader = LiquidGlassPalettes.Create(LiquidGlassTone.Header, false, false);

    /// <summary>مهلة إبقاء الخلفية المموّهة بعد التعطيل حتى تكتمل حركة الخروج.</summary>
    private static readonly TimeSpan BackdropReleaseDelay = TimeSpan.FromMilliseconds(450);

    /// <summary>مدة متابعة الموضع كل إطار بعد التفعيل (حركة الدخول لا تُطلق تحديث تخطيط).</summary>
    private const long BackdropTrackingBurstMs = 700;

    public static readonly DependencyProperty ToneProperty =
        DependencyProperty.Register(nameof(Tone), typeof(LiquidGlassTone), typeof(LiquidGlassChrome),
            new PropertyMetadata(LiquidGlassTone.Standard, OnToneChanged));

    public static readonly DependencyProperty PaletteProperty =
        DependencyProperty.Register(nameof(Palette), typeof(LiquidGlassPalette), typeof(LiquidGlassChrome),
            new PropertyMetadata(null, OnAppearanceChanged));

    /// <summary>نصف قطر الزوايا. القيمة NaN تعني كبسولة كاملة (نصف الارتفاع).</summary>
    public static readonly DependencyProperty CornerRadiusProperty =
        DependencyProperty.Register(nameof(CornerRadius), typeof(double), typeof(LiquidGlassChrome),
            new PropertyMetadata(double.NaN, OnAppearanceChanged));

    public static readonly DependencyProperty ShadowStrengthProperty =
        DependencyProperty.Register(nameof(ShadowStrength), typeof(double), typeof(LiquidGlassChrome),
            new PropertyMetadata(1.0, OnAppearanceChanged));

    /// <summary>
    /// غسل لوني اختياري خفيف (مثل شدة إشعار الأسفل). الشفافية 0 تعني عدم الاستخدام.
    /// </summary>
    public static readonly DependencyProperty AccentProperty =
        DependencyProperty.Register(nameof(Accent), typeof(Color), typeof(LiquidGlassChrome),
            new PropertyMetadata(Colors.Transparent, OnAppearanceChanged));

    // مقدار الحالة المتحرك (0..1). معرّفان قبل IsHovered/IsPressed لأن دوالهما تشير إليهما.
    private static readonly DependencyProperty HoverAmountProperty =
        DependencyProperty.Register("HoverAmount", typeof(double), typeof(LiquidGlassChrome),
            new PropertyMetadata(0.0, OnStateChanged));

    private static readonly DependencyProperty PressAmountProperty =
        DependencyProperty.Register("PressAmount", typeof(double), typeof(LiquidGlassChrome),
            new PropertyMetadata(0.0, OnStateChanged));

    public static readonly DependencyProperty IsHoveredProperty =
        DependencyProperty.Register(nameof(IsHovered), typeof(bool), typeof(LiquidGlassChrome),
            new PropertyMetadata(false, (d, e) => ((LiquidGlassChrome)d).AnimateState(HoverAmountProperty, (bool)e.NewValue, 180, 220)));

    public static readonly DependencyProperty IsPressedProperty =
        DependencyProperty.Register(nameof(IsPressed), typeof(bool), typeof(LiquidGlassChrome),
            new PropertyMetadata(false, (d, e) => ((LiquidGlassChrome)d).AnimateState(PressAmountProperty, (bool)e.NewValue, 90, 170)));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(LiquidGlassChrome),
            new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty IsFocusedGlassProperty =
        DependencyProperty.Register(nameof(IsFocusedGlass), typeof(bool), typeof(LiquidGlassChrome),
            new PropertyMetadata(false, OnStateChanged));

    /// <summary>
    /// العنصر الذي يقع خلف الزجاج. خاصية موروثة: تُضبط على حاوية الأزرار فتصل إلى الزجاج داخل قوالبها.
    /// يجب ألا يكون المصدر سلفاً للزجاج نفسه.
    /// </summary>
    public static readonly DependencyProperty BackdropSourceProperty =
        DependencyProperty.RegisterAttached("BackdropSource", typeof(Visual), typeof(LiquidGlassChrome),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits, OnBackdropSourceChanged));

    public static readonly DependencyProperty IsBackdropEnabledProperty =
        DependencyProperty.Register(nameof(IsBackdropEnabled), typeof(bool), typeof(LiquidGlassChrome),
            new PropertyMetadata(true, (d, e) => ((LiquidGlassChrome)d).OnBackdropEnabledChanged((bool)e.NewValue)));

    private readonly DrawingVisual _shadow = new();
    private readonly DrawingVisual _backdrop = new();
    private readonly DrawingVisual _surface = new();
    private readonly Visual[] _layers;

    private DropShadowEffect? _shadowEffect;
    private double _shadowBaseOpacity;

    private VisualBrush? _backdropBrush;
    private Rect _backdropViewbox = Rect.Empty;
    private bool _backdropMirrored;
    private Size _backdropSize;
    private double _backdropBlur = -1;
    private bool _backdropHeld = true;
    private bool _layoutHooked;
    private bool _renderingHooked;
    private bool _updatingBackdrop;
    private bool _effectsRetryPending;
    private int _effectsRetryCount;
    private long _trackingUntil;
    private DispatcherTimer? _releaseTimer;

    public LiquidGlassChrome()
    {
        _layers = [_shadow, _backdrop, _surface];
        foreach (var layer in _layers)
            AddVisualChild(layer);

        SnapsToDevicePixels = true;
        SetResourceReference(PaletteProperty, StandardPaletteKey);

        // بعد اكتمال الربط بنافذة ذات HWND صالح؛ تطبيق Effect مبكراً يرمي "hwnd of zero is not valid".
        Loaded += (_, _) =>
        {
            _effectsRetryCount = 0;
            RedrawAll();
            ScheduleEffectsRetry();
        };
        Unloaded += (_, _) =>
        {
            _effectsRetryPending = false;
            _effectsRetryCount = 0;
            ReleaseBackdrop();
            ClearEffects();
        };
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible)
                UpdateBackdrop();
            else
                ReleaseBackdrop();
        };
    }

    public LiquidGlassTone Tone
    {
        get => (LiquidGlassTone)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    public LiquidGlassPalette? Palette
    {
        get => (LiquidGlassPalette?)GetValue(PaletteProperty);
        set => SetValue(PaletteProperty, value);
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>مضاعف لشدة الظل (1 = كما في اللوحة). يُخفَّض للأزرار الصغيرة داخل سطح زجاجي آخر.</summary>
    public double ShadowStrength
    {
        get => (double)GetValue(ShadowStrengthProperty);
        set => SetValue(ShadowStrengthProperty, value);
    }

    public Color Accent
    {
        get => (Color)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public bool IsHovered
    {
        get => (bool)GetValue(IsHoveredProperty);
        set => SetValue(IsHoveredProperty, value);
    }

    public bool IsPressed
    {
        get => (bool)GetValue(IsPressedProperty);
        set => SetValue(IsPressedProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>تركيز لوحة المفاتيح على الزر المالك (اسم مختلف عن UIElement.IsFocused).</summary>
    public bool IsFocusedGlass
    {
        get => (bool)GetValue(IsFocusedGlassProperty);
        set => SetValue(IsFocusedGlassProperty, value);
    }

    /// <summary>يوقف الخلفية المموّهة عندما لا يكون السطح ظاهراً (مثلاً إشعار مخفي) حتى لا تُرسم بلا فائدة.</summary>
    public bool IsBackdropEnabled
    {
        get => (bool)GetValue(IsBackdropEnabledProperty);
        set => SetValue(IsBackdropEnabledProperty, value);
    }

    public static Visual? GetBackdropSource(DependencyObject element) => (Visual?)element.GetValue(BackdropSourceProperty);

    public static void SetBackdropSource(DependencyObject element, Visual? value) => element.SetValue(BackdropSourceProperty, value);

    protected override int VisualChildrenCount => _layers.Length;

    protected override Visual GetVisualChild(int index) => _layers[index];

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        RedrawAll();
    }

    /// <summary>الجسم شبه شفاف، لذلك نعلن كامل الشكل (بزواياه المستديرة) قابلاً للنقر، لا ما حوله.</summary>
    protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters)
    {
        var point = hitTestParameters.HitPoint;
        var width = RenderSize.Width;
        var height = RenderSize.Height;
        if (point.X < 0 || point.Y < 0 || point.X > width || point.Y > height)
            return null;

        // خارج الزاوية المستديرة: المسافة إلى مركز أقرب قوس أكبر من نصف القطر.
        var radius = ResolveRadius(width, height);
        var dx = Math.Max(0, Math.Max(radius - point.X, point.X - (width - radius)));
        var dy = Math.Max(0, Math.Max(radius - point.Y, point.Y - (height - radius)));
        return dx * dx + dy * dy <= radius * radius ? new PointHitTestResult(this, point) : null;
    }

    private LiquidGlassPalette EffectivePalette => Palette ?? Tone switch
    {
        LiquidGlassTone.OnBlue => FallbackOnBlue,
        LiquidGlassTone.Notification => FallbackNotification,
        LiquidGlassTone.Header => FallbackHeader,
        _ => FallbackStandard
    };

    private static void OnToneChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chrome = (LiquidGlassChrome)d;
        chrome.SetResourceReference(PaletteProperty, (LiquidGlassTone)e.NewValue switch
        {
            LiquidGlassTone.OnBlue => OnBluePaletteKey,
            LiquidGlassTone.Notification => NotificationPaletteKey,
            LiquidGlassTone.Header => HeaderPaletteKey,
            _ => StandardPaletteKey
        });
        chrome.RedrawAll();
    }

    private static void OnAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((LiquidGlassChrome)d).RedrawAll();

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((LiquidGlassChrome)d).DrawSurface();

    private static void OnBackdropSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LiquidGlassChrome chrome)
            chrome.UpdateBackdrop();
    }

    private void AnimateState(DependencyProperty property, bool on, int enterMs, int exitMs)
    {
        var animation = new DoubleAnimation(on ? 1 : 0, TimeSpan.FromMilliseconds(on ? enterMs : exitMs))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(property, animation);
    }

    private void RedrawAll()
    {
        DrawShadow();
        DrawSurface();
        UpdateBackdrop(rebuild: true);
    }

    private double ResolveRadius(double width, double height)
    {
        var max = Math.Min(width, height) / 2;
        var radius = CornerRadius;
        return double.IsNaN(radius) ? max : Math.Clamp(radius, 0, max);
    }

    // ── الظل: خارج الشكل فقط، حتى لا يعكّر الجسم الصافي ──────────────────────────────

    private void DrawShadow()
    {
        var width = RenderSize.Width;
        var height = RenderSize.Height;
        var palette = EffectivePalette;
        _shadowBaseOpacity = palette.ShadowOpacity * Math.Max(0, ShadowStrength);

        using (var dc = _shadow.RenderOpen())
        {
            if (width < 4 || height < 4 || _shadowBaseOpacity <= 0.001)
            {
                _shadow.Effect = null;
                _shadowEffect = null;
                return;
            }

            // الشكل المُلقي للظل أصغر ببكسل من فتحة القص حتى لا تتسرب حافته المنعّمة.
            var radius = ResolveRadius(width, height);
            var casterRadius = Math.Max(0, radius - 1);
            dc.DrawRoundedRectangle(Brushes.Black, null, new Rect(1, 1, width - 2, height - 2), casterRadius, casterRadius);

            var reach = palette.ShadowBlur + palette.ShadowDepth + 4;
            var clip = new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new RectangleGeometry(new Rect(-reach, -reach, width + 2 * reach, height + 2 * reach)),
                new RectangleGeometry(new Rect(0, 0, width, height), radius, radius));
            clip.Freeze();
            _shadow.Clip = clip;
        }

        _shadowEffect = new DropShadowEffect
        {
            Color = palette.ShadowColor,
            BlurRadius = palette.ShadowBlur,
            ShadowDepth = palette.ShadowDepth,
            Direction = 270,
            Opacity = Math.Clamp(_shadowBaseOpacity, 0, 1),
            RenderingBias = RenderingBias.Performance
        };
        if (!TryApplyEffect(_shadow, _shadowEffect))
            ScheduleEffectsRetry();
    }

    // ── السطح: تلوين، تظليل داخلي، ضوء علوي، حلقة لامعة، خط خارجي ─────────────────────

    private void DrawSurface()
    {
        var width = RenderSize.Width;
        var height = RenderSize.Height;

        using var dc = _surface.RenderOpen();
        if (width < 4 || height < 4)
            return;

        var palette = EffectivePalette;
        var bounds = new Rect(0, 0, width, height);
        var radius = ResolveRadius(width, height);
        var hover = Math.Clamp((double)GetValue(HoverAmountProperty), 0, 1);
        var press = Math.Clamp((double)GetValue(PressAmountProperty), 0, 1);
        var active = IsActive;

        if (_shadowEffect != null)
            _shadowEffect.Opacity = Math.Clamp(_shadowBaseOpacity * (1 + 0.12 * hover - 0.28 * press), 0, 1);

        var accent = Accent;
        var hasAccent = accent.A > 0;

        // 1) جسم شبه صافٍ (+ غسل لوني خفيف إن وُجد Accent).
        var tint = active ? palette.TintActive : palette.Tint;
        tint = Lerp(tint, palette.TintHover, hover * (active ? 0.45 : 1));
        tint = Lerp(tint, palette.TintPressed, press);
        if (hasAccent)
            tint = SoftAccentWash(tint, accent, 0.22 + 0.08 * hover);
        DrawRing(dc, Solid(tint), null, bounds, radius, 0.5);

        // 2) تظليل داخلي خفيف يعطي سماكة زجاج دون bevel معدني.
        var shade = palette.InnerShade;
        var shadeWidth = Math.Min(palette.InnerShadeWidth, Math.Min(width, height) / 2 - 2.5);
        if (shade.A > 0 && shadeWidth > 0.5)
        {
            const double step = 0.9;
            var steps = Math.Max(1, (int)Math.Ceiling(shadeWidth / step));
            var strength = 1 + 0.28 * press;
            for (var i = 0; i < steps; i++)
            {
                var falloff = 1 - i / (double)steps;
                var alpha = shade.A * falloff * falloff * strength;
                var pen = new Pen(Solid(WithAlpha(shade, alpha)), step);
                pen.Freeze();
                DrawRing(dc, null, pen, bounds, radius, 2 + (i + 0.5) * step);
            }
        }

        // 3) ضوء ناعم رقيق عند الحافة العلوية (حلقتان فقط).
        var topLight = palette.TopLight;
        if (topLight.A > 0)
        {
            var lightStrength = (1 + 0.22 * hover) * (1 - 0.55 * press);
            for (var i = 0; i < 2; i++)
            {
                var falloff = 1 - i / 2.0;
                var color = WithAlpha(topLight, topLight.A * falloff * falloff * lightStrength);
                var brush = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(color, 0),
                        new GradientStop(WithAlpha(color, color.A * 0.28), 0.32),
                        new GradientStop(WithAlpha(color, 0), 0.62)
                    },
                    new Point(0, 0), new Point(0, 1));
                brush.Freeze();
                var pen = new Pen(brush, 0.9);
                pen.Freeze();
                DrawRing(dc, null, pen, bounds, radius, 2.4 + i * 0.9);
            }
        }

        // 4) انعكاس ضوئي خفيف على الحافة (ليس حلقة معدنية ساطعة).
        var boost = 1 + 0.1 * hover;
        var specular = new LinearGradientBrush(
            new GradientStopCollection
            {
                new GradientStop(WithAlpha(palette.SpecularStart, palette.SpecularStart.A * boost), 0),
                new GradientStop(WithAlpha(palette.SpecularMid, palette.SpecularMid.A * boost), 0.5),
                new GradientStop(WithAlpha(palette.SpecularEnd, palette.SpecularEnd.A * boost), 1)
            },
            new Point(0, 0), new Point(1, 1));
        specular.Freeze();
        var specularPen = new Pen(specular, 0.85);
        specularPen.Freeze();
        DrawRing(dc, null, specularPen, bounds, radius, 1.5);

        // 5) الخط الخارجي الرفيع الشفاف.
        var outer = IsFocusedGlass ? palette.FocusStroke : Lerp(palette.OuterStroke, palette.OuterStrokeHover, hover);
        if (hasAccent && !IsFocusedGlass)
            outer = SoftAccentWash(outer, accent, 0.35);
        var outerPen = new Pen(Solid(outer), IsFocusedGlass ? 1.35 : 1);
        outerPen.Freeze();
        DrawRing(dc, null, outerPen, bounds, radius, IsFocusedGlass ? 0.75 : 0.5);
    }

    /// <summary>يمزج لون Accent بشفافية منخفضة فوق اللون الأساسي دون إخفاء الزجاج.</summary>
    private static Color SoftAccentWash(Color baseColor, Color accent, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        var wash = Color.FromArgb(
            (byte)Math.Clamp(Math.Round(accent.A * amount), 0, 255),
            accent.R, accent.G, accent.B);
        return Lerp(baseColor, wash, amount);
    }

    private static void DrawRing(DrawingContext dc, Brush? fill, Pen? pen, Rect bounds, double radius, double inset)
    {
        var width = bounds.Width - 2 * inset;
        var height = bounds.Height - 2 * inset;
        if (width <= 0 || height <= 0)
            return;

        var r = Math.Max(0, radius - inset);
        dc.DrawRoundedRectangle(fill, pen, new Rect(bounds.X + inset, bounds.Y + inset, width, height), r, r);
    }

    // ── الخلفية الحقيقية المموّهة ────────────────────────────────────────────────────

    private void OnBackdropEnabledChanged(bool enabled)
    {
        if (enabled)
        {
            _releaseTimer?.Stop();
            _backdropHeld = true;
            UpdateBackdrop();
            StartTrackingBurst();
            return;
        }

        // نُبقي الخلفية قليلاً حتى لا تختفي فجأة أثناء حركة الخروج.
        _releaseTimer ??= CreateReleaseTimer();
        _releaseTimer.Stop();
        _releaseTimer.Start();
    }

    private DispatcherTimer CreateReleaseTimer()
    {
        var timer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = BackdropReleaseDelay };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (IsBackdropEnabled)
                return;
            _backdropHeld = false;
            ReleaseBackdrop();
        };
        return timer;
    }

    private void UpdateBackdrop(bool rebuild = false)
    {
        if (_updatingBackdrop)
            return;

        var source = GetBackdropSource(this);
        var width = RenderSize.Width;
        var height = RenderSize.Height;

        if (source == null || !_backdropHeld || !IsVisible || width < 4 || height < 4)
        {
            ReleaseBackdrop();
            return;
        }

        // بدون HWND صالح لا يمكن تطبيق BlurEffect/VisualBrush بأمان (خطأ hwnd is zero).
        if (!HasValidHwnd())
        {
            HookLayout(true);
            ScheduleEffectsRetry();
            return;
        }

        var palette = EffectivePalette;
        var blur = Math.Max(0, palette.BackdropBlur);

        // نرسم أوسع من الشكل بمقدار التمويه ثم نقص، حتى لا تبهت الأطراف نحو الشفافية.
        var drawRect = new Rect(-blur, -blur, width + 2 * blur, height + 2 * blur);
        if (!TryMapToSource(source, drawRect, out var viewbox, out var mirrored))
        {
            ReleaseBackdrop();
            HookLayout(true);
            ScheduleEffectsRetry();
            return;
        }

        var needsRebuild = rebuild
            || _backdropBrush == null
            || !ReferenceEquals(_backdropBrush.Visual, source)
            || _backdropSize != RenderSize
            || _backdropMirrored != mirrored
            || Math.Abs(_backdropBlur - blur) > 0.01;

        _updatingBackdrop = true;
        try
        {
            if (needsRebuild)
            {
                var brush = new VisualBrush(source)
                {
                    ViewboxUnits = BrushMappingMode.Absolute,
                    Viewbox = viewbox,
                    Stretch = Stretch.Fill,
                    TileMode = TileMode.None
                };
                if (mirrored)
                    brush.RelativeTransform = new ScaleTransform(-1, 1, 0.5, 0.5);

                using (var dc = _backdrop.RenderOpen())
                    dc.DrawRectangle(brush, null, drawRect);

                var radius = Math.Max(0, ResolveRadius(width, height) - 1);
                var clip = new RectangleGeometry(new Rect(1, 1, width - 2, height - 2), radius, radius);
                clip.Freeze();
                _backdrop.Clip = clip;

                Effect? blurEffect = blur > 0.01
                    ? new BlurEffect { Radius = blur, KernelType = KernelType.Gaussian, RenderingBias = RenderingBias.Performance }
                    : null;
                if (!TryApplyEffect(_backdrop, blurEffect))
                {
                    // نبقي الخلفية بدون تمويه حتى لا نكسر الصفحة، ونعيد المحاولة بعد جاهزية التركيب.
                    _backdropBrush = brush;
                    _backdropSize = RenderSize;
                    _backdropMirrored = mirrored;
                    _backdropBlur = 0;
                    _backdropViewbox = viewbox;
                    ScheduleEffectsRetry();
                }
                else
                {
                    _backdropBrush = brush;
                    _backdropSize = RenderSize;
                    _backdropMirrored = mirrored;
                    _backdropBlur = blur;
                    _backdropViewbox = viewbox;
                }
            }
            else if (!AreClose(_backdropViewbox, viewbox))
            {
                _backdropBrush!.Viewbox = viewbox;
                _backdropViewbox = viewbox;
            }

            HookLayout(true);
        }
        catch (InvalidOperationException)
        {
            // غالباً "An HWND of zero is not valid" أثناء أول إطار للصفحة الرئيسية.
            SafeClearBackdropVisual();
            ScheduleEffectsRetry();
            HookLayout(true);
        }
        finally
        {
            _updatingBackdrop = false;
        }
    }

    private bool TryMapToSource(Visual source, Rect drawRect, out Rect viewbox, out bool mirrored)
    {
        viewbox = Rect.Empty;
        mirrored = false;

        // مصدر يحتوي الزجاج نفسه يعني رسم العنصر داخل نفسه.
        if (source.IsAncestorOf(this))
            return false;

        try
        {
            var transform = TransformToVisual(source);
            viewbox = transform.TransformBounds(drawRect);
            if (viewbox.IsEmpty || viewbox.Width < 1 || viewbox.Height < 1)
                return false;

            mirrored = transform.TryTransform(new Point(0, 0), out var origin)
                && transform.TryTransform(new Point(1, 0), out var unitX)
                && unitX.X < origin.X;
            return true;
        }
        catch (InvalidOperationException)
        {
            // لا يوجد سلف مشترك بعد (العنصر خارج الشجرة البصرية).
            return false;
        }
    }

    private void ReleaseBackdrop()
    {
        HookLayout(false);
        HookRendering(false);
        SafeClearBackdropVisual();
    }

    private void SafeClearBackdropVisual()
    {
        _backdropBrush = null;
        _backdropViewbox = Rect.Empty;
        _backdropSize = default;
        _backdropBlur = -1;
        TryApplyEffect(_backdrop, null);
        _backdrop.Clip = null;
        try
        {
            using (_backdrop.RenderOpen())
            {
            }
        }
        catch (InvalidOperationException)
        {
            // تجاهل أثناء التفكيك.
        }
    }

    private void ClearEffects()
    {
        TryApplyEffect(_shadow, null);
        TryApplyEffect(_backdrop, null);
        _shadowEffect = null;
    }

    /// <summary>تطبيق Effect فقط عند وجود نافذة ذات HWND؛ وإلا يتجنب كسر الصفحة الرئيسية.</summary>
    private bool TryApplyEffect(DrawingVisual visual, Effect? effect)
    {
        try
        {
            if (effect != null && !HasValidHwnd())
            {
                visual.Effect = null;
                return false;
            }

            visual.Effect = effect;
            return true;
        }
        catch (InvalidOperationException)
        {
            try { visual.Effect = null; } catch { /* ignore */ }
            return false;
        }
    }

    private bool HasValidHwnd()
    {
        try
        {
            if (PresentationSource.FromVisual(this) is not HwndSource hwnd)
                return false;
            return hwnd.Handle != IntPtr.Zero && !hwnd.IsDisposed;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void ScheduleEffectsRetry()
    {
        if (_effectsRetryPending || !IsLoaded || _effectsRetryCount >= 12)
            return;

        _effectsRetryPending = true;
        _effectsRetryCount++;
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
        {
            _effectsRetryPending = false;
            if (!IsLoaded || !IsVisible)
                return;

            try
            {
                DrawShadow();
                UpdateBackdrop(rebuild: true);
                if (HasValidHwnd() && (_shadow.Effect != null || GetBackdropSource(this) == null || _backdrop.Effect != null || _backdropBrush != null))
                    _effectsRetryCount = 0;
            }
            catch (InvalidOperationException)
            {
                // ما زال التركيب غير جاهز؛ نعيد المحاولة في الإطار التالي.
                Dispatcher.BeginInvoke(DispatcherPriority.Background, () => ScheduleEffectsRetry());
            }
        });
    }

    private void HookLayout(bool hook)
    {
        if (hook == _layoutHooked)
            return;

        _layoutHooked = hook;
        if (hook)
            LayoutUpdated += OnLayoutUpdated;
        else
            LayoutUpdated -= OnLayoutUpdated;
    }

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        try
        {
            UpdateBackdrop();
        }
        catch (InvalidOperationException)
        {
            ScheduleEffectsRetry();
        }
    }

    private void StartTrackingBurst()
    {
        _trackingUntil = Environment.TickCount64 + BackdropTrackingBurstMs;
        HookRendering(true);
    }

    private void HookRendering(bool hook)
    {
        if (hook == _renderingHooked)
            return;

        _renderingHooked = hook;
        if (hook)
            CompositionTarget.Rendering += OnRendering;
        else
            CompositionTarget.Rendering -= OnRendering;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (Environment.TickCount64 > _trackingUntil)
        {
            HookRendering(false);
            return;
        }

        UpdateBackdrop();
    }

    // ── أدوات ────────────────────────────────────────────────────────────────────────

    private static bool AreClose(Rect a, Rect b) =>
        Math.Abs(a.X - b.X) < 0.25 && Math.Abs(a.Y - b.Y) < 0.25 &&
        Math.Abs(a.Width - b.Width) < 0.25 && Math.Abs(a.Height - b.Height) < 0.25;

    private static SolidColorBrush Solid(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Color WithAlpha(Color color, double alpha) =>
        Color.FromArgb((byte)Math.Clamp(Math.Round(alpha), 0, 255), color.R, color.G, color.B);

    private static Color Lerp(Color from, Color to, double amount)
    {
        if (amount <= 0)
            return from;
        if (amount >= 1)
            return to;

        // مزج بألوان مضروبة بالشفافية حتى لا يظهر لون الطرف الشفاف أثناء الانتقال.
        var fromA = from.A / 255.0;
        var toA = to.A / 255.0;
        var a = fromA + (toA - fromA) * amount;
        if (a <= 0.0001)
            return Color.FromArgb(0, to.R, to.G, to.B);

        byte Channel(byte f, byte t) =>
            (byte)Math.Clamp(Math.Round((f * fromA * (1 - amount) + t * toA * amount) / a), 0, 255);

        return Color.FromArgb((byte)Math.Round(a * 255), Channel(from.R, to.R), Channel(from.G, to.G), Channel(from.B, to.B));
    }
}
