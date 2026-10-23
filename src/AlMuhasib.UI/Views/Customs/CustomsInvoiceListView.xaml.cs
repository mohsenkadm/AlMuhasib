using System.Windows.Controls;
using AlMuhasib.UI.ViewModels.Customs;

namespace AlMuhasib.UI.Views.Customs;

public partial class CustomsInvoiceListView : UserControl
{
    public CustomsInvoiceListView()
    {
        InitializeComponent();
    }

    private void InvoicesGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not CustomsInvoiceListViewModel vm)
            return;

        CustomsServerSortHelper.HandleSorting(InvoicesGrid, e, vm.ApplyServerSortAsync);
    }
}
