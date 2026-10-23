using System.ComponentModel;
using System.Windows.Controls;

namespace AlMuhasib.UI.Views.Customs;

/// <summary>
/// M-04: cancel WPF page-local DataGrid sort and drive allowlisted server reload via ViewModel.
/// </summary>
internal static class CustomsServerSortHelper
{
    public static void HandleSorting(
        DataGrid grid,
        DataGridSortingEventArgs e,
        Func<string, bool, Task> applyServerSortAsync)
    {
        e.Handled = true;
        var path = e.Column.SortMemberPath;
        if (string.IsNullOrWhiteSpace(path))
            return;

        var next = e.Column.SortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        foreach (var column in grid.Columns)
        {
            if (!ReferenceEquals(column, e.Column))
                column.SortDirection = null;
        }

        e.Column.SortDirection = next;
        _ = applyServerSortAsync(path, next == ListSortDirection.Descending);
    }
}
