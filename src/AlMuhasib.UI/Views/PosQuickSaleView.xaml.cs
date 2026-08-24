using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views;

public partial class PosQuickSaleView : UserControl
{
    public static readonly DependencyProperty IsFullscreenHostProperty =
        DependencyProperty.Register(
            nameof(IsFullscreenHost),
            typeof(bool),
            typeof(PosQuickSaleView),
            new PropertyMetadata(false));

    /// <summary>
    /// True when this view is hosted in the dedicated fullscreen window (not the main tab).
    /// </summary>
    public bool IsFullscreenHost
    {
        get => (bool)GetValue(IsFullscreenHostProperty);
        set => SetValue(IsFullscreenHostProperty, value);
    }

    public PosQuickSaleView()
    {
        InitializeComponent();
        Loaded += OnLoadedAttachColumns;
    }

    private void OnLoadedAttachColumns(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoadedAttachColumns;
        PosFeatureColumnSync.Attach(
            this,
            ColPosSize, ColPosColor, ColPosCustom1, ColPosCustom2,
            ColPosBatch, ColPosSerial, pricing: null, ColPosDiscount);
    }

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            SearchBox.Focus();
            Keyboard.Focus(SearchBox);
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private void Root_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Keep barcode scanner input flowing to search when focus drifts.
        if (e.Key == Key.F2)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F7)
        {
            if (DataContext is ViewModels.PosQuickSaleViewModel vm)
                vm.OpenCurrencyChangeCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is ViewModels.PosQuickSaleViewModel vm)
            vm.AddProductFromSearchCommand.Execute(null);
        e.Handled = true;
    }
}
