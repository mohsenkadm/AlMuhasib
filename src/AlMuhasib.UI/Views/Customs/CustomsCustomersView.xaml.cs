using System.Windows.Controls;
using AlMuhasib.UI.ViewModels.Customs;

namespace AlMuhasib.UI.Views.Customs;

public partial class CustomsCustomersView : UserControl
{
    public CustomsCustomersView()
    {
        InitializeComponent();
    }

    private void CustomsCustomersGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not CustomsCustomersViewModel vm)
            return;

        CustomsServerSortHelper.HandleSorting(CustomsCustomersGrid, e, vm.ApplyServerSortAsync);
    }
}
