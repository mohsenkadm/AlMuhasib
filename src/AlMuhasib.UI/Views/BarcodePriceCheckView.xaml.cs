using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AlMuhasib.UI.Views;

public partial class BarcodePriceCheckView : UserControl
{
    public BarcodePriceCheckView() => InitializeComponent();

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            BarcodeBox.Focus();
            Keyboard.Focus(BarcodeBox);
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private void Root_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2)
        {
            FocusBarcodeBox();
            e.Handled = true;
        }
    }

    private void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is ViewModels.BarcodePriceCheckViewModel vm)
            vm.LookupBarcodeCommand.Execute(null);
        FocusBarcodeBox();
        e.Handled = true;
    }

    private void FocusBarcodeBox()
    {
        BarcodeBox.Focus();
        Keyboard.Focus(BarcodeBox);
        BarcodeBox.SelectAll();
    }
}
