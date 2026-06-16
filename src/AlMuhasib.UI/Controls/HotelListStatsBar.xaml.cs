using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace AlMuhasib.UI.Controls;

public partial class HotelListStatsBar : UserControl
{
    public HotelListStatsBar() => InitializeComponent();

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(HotelListStatsBar),
            new PropertyMetadata(null));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }
}
