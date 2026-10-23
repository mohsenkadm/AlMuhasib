using System.Windows.Controls;
using AlMuhasib.UI.ViewModels.Customs;

namespace AlMuhasib.UI.Views.Customs;

public partial class CustomsReceiptListView : UserControl
{
    public CustomsReceiptListView()
    {
        InitializeComponent();
    }

    private void ReceiptsGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not CustomsReceiptListViewModel vm)
            return;

        CustomsServerSortHelper.HandleSorting(ReceiptsGrid, e, vm.ApplyServerSortAsync);
    }
}
