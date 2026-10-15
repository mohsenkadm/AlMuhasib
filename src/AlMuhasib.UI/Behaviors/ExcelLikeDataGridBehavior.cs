using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace AlMuhasib.UI.Behaviors;

/// <summary>
/// سلوك جدول شبيه بإكسل: Enter للصف التالي، تحديد النص عند التركيز، ولصق عمودي للأرقام.
/// </summary>
public static class ExcelLikeDataGridBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ExcelLikeDataGridBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>رأس أو Tag العمود القابل للصق العمودي (مثل سعر البيع الجديد).</summary>
    public static readonly DependencyProperty PasteTargetHeaderProperty =
        DependencyProperty.RegisterAttached(
            "PasteTargetHeader",
            typeof(string),
            typeof(ExcelLikeDataGridBehavior),
            new PropertyMetadata("سعر البيع الجديد"));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static string GetPasteTargetHeader(DependencyObject obj) => (string)obj.GetValue(PasteTargetHeaderProperty);
    public static void SetPasteTargetHeader(DependencyObject obj, string value) => obj.SetValue(PasteTargetHeaderProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid) return;

        if ((bool)e.NewValue)
        {
            grid.PreviewKeyDown += OnPreviewKeyDown;
            grid.GotFocus += OnGotFocus;
            grid.BeginningEdit += OnBeginningEdit;
            DataObject.AddPastingHandler(grid, OnPaste);
        }
        else
        {
            grid.PreviewKeyDown -= OnPreviewKeyDown;
            grid.GotFocus -= OnGotFocus;
            grid.BeginningEdit -= OnBeginningEdit;
            DataObject.RemovePastingHandler(grid, OnPaste);
        }
    }

    private static void OnBeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
    {
        if (sender is not DataGrid grid) return;
        grid.Dispatcher.BeginInvoke(() =>
        {
            var cell = GetCurrentCell(grid);
            var textBox = cell is null ? null : FindVisualChild<TextBox>(cell);
            if (textBox is null) return;
            textBox.Focus();
            textBox.SelectAll();
        }, DispatcherPriority.Input);
    }

    private static void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TextBox textBox)
        {
            textBox.Dispatcher.BeginInvoke(() =>
            {
                if (textBox.IsFocused)
                    textBox.SelectAll();
            }, DispatcherPriority.Input);
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid) return;

        if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (TryPasteColumn(grid))
            {
                e.Handled = true;
                return;
            }
        }

        if (e.Key != Key.Enter)
            return;

        // اترك Enter الافتراضي داخل محرر متعدد الأسطر إن وُجد
        if (e.OriginalSource is TextBox { AcceptsReturn: true })
            return;

        var currentItem = grid.CurrentItem ?? grid.SelectedItem;
        var currentColumn = grid.CurrentColumn;
        if (currentItem is null || currentColumn is null)
            return;

        var currentIndex = grid.Items.IndexOf(currentItem);
        if (currentIndex < 0 || currentIndex >= grid.Items.Count - 1)
        {
            try { grid.CommitEdit(DataGridEditingUnit.Cell, true); } catch (InvalidOperationException) { }
            try { grid.CommitEdit(DataGridEditingUnit.Row, true); } catch (InvalidOperationException) { }
            e.Handled = true;
            return;
        }

        try { grid.CommitEdit(DataGridEditingUnit.Cell, true); } catch (InvalidOperationException) { }
        try { grid.CommitEdit(DataGridEditingUnit.Row, true); } catch (InvalidOperationException) { }

        var nextItem = grid.Items[currentIndex + 1];
        grid.Dispatcher.BeginInvoke(() =>
        {
            grid.ScrollIntoView(nextItem, currentColumn);
            grid.CurrentCell = new DataGridCellInfo(nextItem, currentColumn);
            if (grid.SelectionUnit == DataGridSelectionUnit.Cell)
            {
                grid.SelectedCells.Clear();
                grid.SelectedCells.Add(grid.CurrentCell);
            }
            else
            {
                grid.SelectedItem = nextItem;
            }

            if (!currentColumn.IsReadOnly)
            {
                try { grid.BeginEdit(); } catch (InvalidOperationException) { }
            }
        }, DispatcherPriority.Background);

        e.Handled = true;
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not DataGrid grid) return;
        if (!e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText)
            && !e.SourceDataObject.GetDataPresent(DataFormats.Text))
            return;

        if (TryPasteColumn(grid))
            e.CancelCommand();
    }

    private static bool TryPasteColumn(DataGrid grid)
    {
        if (!Clipboard.ContainsText())
            return false;

        var text = Clipboard.GetText();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var lines = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0)
            return false;

        var values = new List<decimal>(lines.Length);
        foreach (var line in lines)
        {
            var token = line.Split('\t', StringSplitOptions.TrimEntries)[0]
                .Replace(",", "", StringComparison.Ordinal)
                .Trim();
            if (!decimal.TryParse(token, NumberStyles.Number, CultureInfo.CurrentCulture, out var value)
                && !decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
                return false;
            values.Add(Math.Max(0m, value));
        }

        var targetColumn = ResolvePasteColumn(grid);
        if (targetColumn is null)
            return false;

        var startItem = grid.CurrentItem ?? grid.SelectedItem;
        if (startItem is null)
            return false;

        var startIndex = grid.Items.IndexOf(startItem);
        if (startIndex < 0)
            return false;

        try { grid.CommitEdit(DataGridEditingUnit.Cell, true); } catch (InvalidOperationException) { }
        try { grid.CommitEdit(DataGridEditingUnit.Row, true); } catch (InvalidOperationException) { }

        var bindingPath = GetColumnBindingPath(targetColumn);
        if (string.IsNullOrWhiteSpace(bindingPath))
            return false;

        for (var i = 0; i < values.Count; i++)
        {
            var rowIndex = startIndex + i;
            if (rowIndex >= grid.Items.Count)
                break;

            var item = grid.Items[rowIndex];
            if (item is null || item == CollectionView.NewItemPlaceholder)
                break;

            var prop = item.GetType().GetProperty(bindingPath);
            if (prop is null || !prop.CanWrite)
                continue;

            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            object coerced = values[i];
            if (targetType != typeof(decimal))
                coerced = Convert.ChangeType(values[i], targetType, CultureInfo.InvariantCulture);
            prop.SetValue(item, coerced);
        }

        return true;
    }

    private static DataGridColumn? ResolvePasteColumn(DataGrid grid)
    {
        if (grid.CurrentColumn is { IsReadOnly: false } current)
            return current;

        var header = GetPasteTargetHeader(grid);
        foreach (var column in grid.Columns)
        {
            if (column.IsReadOnly) continue;
            if (string.Equals(column.Header?.ToString(), header, StringComparison.Ordinal))
                return column;
            if (column is DataGridBoundColumn bound
                && bound.Binding is System.Windows.Data.Binding b
                && string.Equals(b.Path?.Path, "NewPrice", StringComparison.Ordinal))
                return column;
        }

        return grid.Columns.FirstOrDefault(c => !c.IsReadOnly && c is DataGridTextColumn);
    }

    private static string? GetColumnBindingPath(DataGridColumn column)
    {
        if (column is DataGridBoundColumn { Binding: System.Windows.Data.Binding binding })
            return binding.Path?.Path;
        return null;
    }

    private static DataGridCell? GetCurrentCell(DataGrid grid)
    {
        if (grid.CurrentCell.Item is null || grid.CurrentCell.Column is null)
            return null;

        var row = (DataGridRow?)grid.ItemContainerGenerator.ContainerFromItem(grid.CurrentCell.Item);
        if (row is null) return null;

        var presenter = FindVisualChild<DataGridCellsPresenter>(row);
        if (presenter is null) return null;

        return (DataGridCell?)presenter.ItemContainerGenerator.ContainerFromIndex(grid.CurrentCell.Column.DisplayIndex);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found) return found;
            var nested = FindVisualChild<T>(child);
            if (nested is not null) return nested;
        }

        return null;
    }
}
