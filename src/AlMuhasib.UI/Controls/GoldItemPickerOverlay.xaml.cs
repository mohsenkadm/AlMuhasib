using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AlMuhasib.UI.ViewModels.Gold;

namespace AlMuhasib.UI.Controls;

public partial class GoldItemPickerOverlay : UserControl
{
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(GoldItemPickerOverlay),
            new PropertyMetadata(false, OnIsOpenChanged));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public GoldItemPickerOverlay()
    {
        InitializeComponent();
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not GoldItemPickerOverlay overlay)
            return;

        var open = e.NewValue is true;
        overlay.OverlayRoot.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        overlay.IsHitTestVisible = open;
        overlay.OverlayRoot.IsHitTestVisible = open;
    }

    private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is GoldItemPickerViewModel vm && vm.CancelCommand.CanExecute(null))
            vm.CancelCommand.Execute(null);
    }

    private void ItemRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: GoldItemPickerDisplayItem item })
            item.IsSelected = !item.IsSelected;
    }
}
