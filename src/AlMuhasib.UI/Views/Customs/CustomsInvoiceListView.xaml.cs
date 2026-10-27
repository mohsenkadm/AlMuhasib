using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.ViewModels.Customs;

namespace AlMuhasib.UI.Views.Customs;

public partial class CustomsInvoiceListView : UserControl
{
    public CustomsInvoiceListView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SyncListProxy();
        Loaded += (_, _) => SyncListProxy();
    }

    private void SyncListProxy()
    {
        if (Resources["ListProxy"] is BindingProxy proxy)
            proxy.Data = DataContext;
    }

    private void InvoicesGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not CustomsInvoiceListViewModel vm)
            return;

        CustomsServerSortHelper.HandleSorting(InvoicesGrid, e, vm.ApplyServerSortAsync);
    }
}
