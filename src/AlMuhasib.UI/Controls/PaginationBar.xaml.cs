using System.Windows;
using System.Windows.Controls;

namespace AlMuhasib.UI.Controls;

public partial class PaginationBar
{
    public static readonly DependencyProperty ShowFirstLastButtonsProperty =
        DependencyProperty.Register(
            nameof(ShowFirstLastButtons),
            typeof(bool),
            typeof(PaginationBar),
            new PropertyMetadata(true));

    public bool ShowFirstLastButtons
    {
        get => (bool)GetValue(ShowFirstLastButtonsProperty);
        set => SetValue(ShowFirstLastButtonsProperty, value);
    }

    public PaginationBar() => InitializeComponent();
}
