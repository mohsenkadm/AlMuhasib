using System.Windows.Controls;
using System.Windows.Input;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class MaintenanceView : UserControl
{
    public MaintenanceView() => InitializeComponent();

    private void MaintGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MaintenanceViewModel vm)
            return;
        if (MaintGrid.SelectedItem is MaintenanceReportRow row
            && vm.EditRecordCommand.CanExecute(row))
            vm.EditRecordCommand.Execute(row);
    }
}
