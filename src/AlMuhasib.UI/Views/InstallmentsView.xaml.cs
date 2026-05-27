using System.Windows;
using System.Windows.Controls;
using AlMuhasib.Core.Entities;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class InstallmentsView : UserControl
{
    public InstallmentsView()
    {
        InitializeComponent();
    }

    private void PayableInstallmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not InstallmentsViewModel vm || sender is not DataGrid grid)
            return;

        var selected = grid.SelectedItems.Cast<Installment>().ToList();
        vm.SetBulkSelection(selected);

        if (grid == PlanInstallmentsGrid)
            vm.PaymentSelectedInstallment = selected.Count == 1 ? selected[0] : null;
    }

    private void ClearBulkSelection_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is InstallmentsViewModel vm)
            vm.ClearBulkSelectionCommand.Execute(null);

        foreach (var grid in new DataGrid?[]
                 {
                     OverdueInstallmentsGrid, PlanInstallmentsGrid,
                     DetailedInstallmentsGrid, UnpaidInstallmentsGrid
                 })
        {
            grid?.SelectedItems.Clear();
        }
    }
}
