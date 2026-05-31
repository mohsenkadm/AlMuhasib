using System.Windows.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class UnpaidInstallmentsReportView : UserControl
{
    public UnpaidInstallmentsReportView() => InitializeComponent();

    private void InstallmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is UnpaidInstallmentsReportViewModel vm && sender is DataGrid grid)
            vm.UpdateBulkInstallmentSelectionFromGrid(grid);
    }
}
