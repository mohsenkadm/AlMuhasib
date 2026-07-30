using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views;

public partial class PurchaseInvoiceView : UserControl
{
    public PurchaseInvoiceView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        InvoiceFeatureColumnSync.Attach(
            this,
            custom1: ColSize,
            custom2: null,
            ColUnit,
            ColBatch,
            ColExpiry,
            ColSerial);
    }
}
