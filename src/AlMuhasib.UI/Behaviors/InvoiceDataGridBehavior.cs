using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace AlMuhasib.UI.Behaviors;

/// <summary>
/// Attached behavior for invoice DataGrids:
/// - Tab/focus into a TextBox auto-selects all text for immediate overwrite
/// - Enter on the last row triggers the AddRow command
/// </summary>
public static class InvoiceDataGridBehavior
{
    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached("Enable", typeof(bool), typeof(InvoiceDataGridBehavior),
            new PropertyMetadata(false, OnEnableChanged));

    public static bool GetEnable(DependencyObject obj) => (bool)obj.GetValue(EnableProperty);
    public static void SetEnable(DependencyObject obj, bool value) => obj.SetValue(EnableProperty, value);

    public static readonly DependencyProperty AddRowCommandProperty =
        DependencyProperty.RegisterAttached("AddRowCommand", typeof(ICommand), typeof(InvoiceDataGridBehavior));

    public static ICommand? GetAddRowCommand(DependencyObject obj) => (ICommand?)obj.GetValue(AddRowCommandProperty);
    public static void SetAddRowCommand(DependencyObject obj, ICommand? value) => obj.SetValue(AddRowCommandProperty, value);

    private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid) return;

        if ((bool)e.NewValue)
        {
            grid.PreviewKeyDown += OnPreviewKeyDown;
            grid.GotFocus += OnGotFocus;
        }
        else
        {
            grid.PreviewKeyDown -= OnPreviewKeyDown;
            grid.GotFocus -= OnGotFocus;
        }
    }

    private static void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TextBox textBox)
        {
            textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                textBox.SelectAll();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid) return;

        if (e.Key == Key.Enter)
        {
            var currentIndex = grid.Items.IndexOf(grid.CurrentItem);
            if (currentIndex >= 0 && currentIndex == grid.Items.Count - 1)
            {
                var cmd = GetAddRowCommand(grid);
                if (cmd is not null && cmd.CanExecute(null))
                {
                    cmd.Execute(null);
                    grid.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var newItem = grid.Items[grid.Items.Count - 1];
                        grid.ScrollIntoView(newItem);
                        grid.CurrentCell = new DataGridCellInfo(newItem, grid.Columns[1]);
                        grid.BeginEdit();
                        FocusFirstTextBoxInCurrentCell(grid);
                    }), System.Windows.Threading.DispatcherPriority.Background);
                    e.Handled = true;
                }
            }
            else if (currentIndex >= 0 && currentIndex < grid.Items.Count - 1)
            {
                var col = grid.CurrentColumn;
                grid.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var nextItem = grid.Items[currentIndex + 1];
                    grid.CurrentCell = new DataGridCellInfo(nextItem, col ?? grid.Columns[1]);
                    grid.BeginEdit();
                    FocusFirstTextBoxInCurrentCell(grid);
                }), System.Windows.Threading.DispatcherPriority.Background);
                e.Handled = true;
            }
        }
    }

    private static void FocusFirstTextBoxInCurrentCell(DataGrid grid)
    {
        grid.Dispatcher.BeginInvoke(new Action(() =>
        {
            var cell = GetCurrentCell(grid);
            if (cell is null) return;
            var textBox = FindVisualChild<TextBox>(cell);
            if (textBox is not null)
            {
                textBox.Focus();
                textBox.SelectAll();
            }
        }), System.Windows.Threading.DispatcherPriority.Input);
    }

    private static DataGridCell? GetCurrentCell(DataGrid grid)
    {
        if (grid.CurrentCell.Item == null || grid.CurrentCell.Column == null) return null;
        var row = (DataGridRow?)grid.ItemContainerGenerator.ContainerFromItem(grid.CurrentCell.Item);
        if (row is null) return null;
        var presenter = FindVisualChild<DataGridCellsPresenter>(row);
        if (presenter is null) return null;
        var colIndex = grid.CurrentCell.Column.DisplayIndex;
        return (DataGridCell?)presenter.ItemContainerGenerator.ContainerFromIndex(colIndex);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found) return found;
            var result = FindVisualChild<T>(child);
            if (result is not null) return result;
        }
        return null;
    }
}
