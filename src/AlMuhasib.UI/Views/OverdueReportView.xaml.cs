using System.Windows.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class OverdueReportView : UserControl
{
    public OverdueReportView() => InitializeComponent();

    private void OverdueGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is OverdueReportViewModel vm && sender is DataGrid grid)
            vm.UpdateBulkInstallmentSelectionFromGrid(grid);
    }
}
