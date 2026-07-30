using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views;

public partial class SalesInvoiceView : UserControl
{
    public SalesInvoiceView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        PreviewKeyDown += Root_PreviewKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        InvoiceFeatureColumnSync.Attach(
            this,
            ColCustomField1,
            ColCustomField2,
            ColUnit,
            ColBatch,
            expiry: null,
            ColSerial,
            ColPricingType);
    }

    private void Root_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F7) return;
        if (DataContext is ViewModels.SalesInvoiceViewModel vm)
            vm.OpenCurrencyChangeCommand.Execute(null);
        e.Handled = true;
    }
}
