using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Controls;

/// <summary>
/// زر إجراء سريع: كبسولة زجاجية مع أيقونة داخل عدسة ملوّنة.
/// </summary>
public class GlassActionButton : Button
{
    private static readonly SolidColorBrush DefaultAccent = Freeze(Color.FromRgb(0x15, 0x65, 0xC0));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(PackIconKind),
            typeof(GlassActionButton),
            new PropertyMetadata(PackIconKind.CircleSmall));

    public static readonly DependencyProperty AccentBrushProperty =
        DependencyProperty.Register(
            nameof(AccentBrush),
            typeof(Brush),
            typeof(GlassActionButton),
            new PropertyMetadata(DefaultAccent));

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(GlassActionButton),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconContentProperty =
        DependencyProperty.Register(
            nameof(IconContent),
            typeof(object),
            typeof(GlassActionButton),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(GlassActionButton),
            new PropertyMetadata(false));

    public PackIconKind Icon
    {
        get => (PackIconKind)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>يستبدل أيقونة المكتبة برسم مخصص داخل العدسة.</summary>
    public object? IconContent
    {
        get => GetValue(IconContentProperty);
        set => SetValue(IconContentProperty, value);
    }

    /// <summary>تمييز بصري للزر النشط دون تغيير الأمر.</summary>
    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    private static SolidColorBrush Freeze(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
