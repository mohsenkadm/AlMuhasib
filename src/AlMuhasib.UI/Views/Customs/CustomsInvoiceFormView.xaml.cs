using System.Windows.Controls;
using AlMuhasib.UI.ViewModels.Customs;

namespace AlMuhasib.UI.Views.Customs;

public partial class CustomsInvoiceFormView : UserControl
{
    public CustomsInvoiceFormView()
    {
        InitializeComponent();
    }

    private void VehiclesGrid_OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (DataContext is CustomsInvoiceFormViewModel vm)
            vm.VehiclePriceChangedCommand.Execute(null);
    }
}
