using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AlMuhasib.UI.Controls;

public partial class HotelStatusBadge : UserControl
{
    public HotelStatusBadge()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(nameof(Label), typeof(string), typeof(HotelStatusBadge), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register(nameof(Color), typeof(string), typeof(HotelStatusBadge),
            new PropertyMetadata("#616161", OnColorChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Color
    {
        get => (string)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HotelStatusBadge badge)
            badge.UpdateBackground();
    }

    private void UpdateBackground()
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(Color)!;
            BadgeBorder.Background = new SolidColorBrush(color);
        }
        catch
        {
            BadgeBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#616161")!);
        }
    }
}
