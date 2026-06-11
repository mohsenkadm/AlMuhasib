using System.Windows;
using System.Windows.Controls;

namespace AlMuhasib.UI.Controls;

public partial class ListViewModeToggle
{
    public static readonly DependencyProperty OnDarkBackgroundProperty =
        DependencyProperty.Register(
            nameof(OnDarkBackground),
            typeof(bool),
            typeof(ListViewModeToggle),
            new PropertyMetadata(true));

    public bool OnDarkBackground
    {
        get => (bool)GetValue(OnDarkBackgroundProperty);
        set => SetValue(OnDarkBackgroundProperty, value);
    }

    public ListViewModeToggle() => InitializeComponent();
}
