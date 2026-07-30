using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views;

public partial class InstallmentInvoiceView : UserControl
{
    public InstallmentInvoiceView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        InvoiceFeatureColumnSync.Attach(
            this,
            custom1: null,
            custom2: null,
            ColUnit,
            batch: null,
            expiry: null,
            serial: null,
            pricing: null);
    }
}
