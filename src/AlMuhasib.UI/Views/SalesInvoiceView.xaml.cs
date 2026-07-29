using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views;

public partial class SalesInvoiceView : UserControl
{
    public SalesInvoiceView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
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
}
