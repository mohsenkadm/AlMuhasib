using System.Windows.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class InstallmentAgingReportView : UserControl
{
    public InstallmentAgingReportView() => InitializeComponent();

    private void AgingGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is InstallmentAgingReportViewModel vm && sender is DataGrid grid)
            vm.UpdateBulkInstallmentSelectionFromGrid(grid);
    }
}
