using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views;

public partial class PosQuickSaleView : UserControl
{
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
            ColPosUnit, ColPosBatch, ColPosSerial, ColPosPricing, ColPosDiscount);
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
