using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Views.Customs;

public partial class CustomsInvoiceFormView : UserControl
{
    public CustomsInvoiceFormView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => SyncFormProxy();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        SyncFormProxy();

    private void SyncFormProxy()
    {
        if (Resources["FormProxy"] is BindingProxy proxy)
            proxy.Data = DataContext;
    }
}
