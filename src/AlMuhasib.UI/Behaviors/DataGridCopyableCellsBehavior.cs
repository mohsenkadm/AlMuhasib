using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace AlMuhasib.UI.Behaviors;

/// <summary>
/// يحوّل أعمدة DataGridTextColumn إلى خلايا TextBox للقراءة فقط لتمكين تحديد ونسخ النص،
/// مع دعم Ctrl+C لنسخ الصفوف المحددة كـ TSV.
/// </summary>
public static class DataGridCopyableCellsBehavior
{
    private static readonly DependencyProperty IsProcessingProperty =
        DependencyProperty.RegisterAttached(
            "IsProcessing",
            typeof(bool),
            typeof(DataGridCopyableCellsBehavior));

    private static readonly DependencyProperty IsConvertedColumnProperty =
        DependencyProperty.RegisterAttached(
            "IsConvertedColumn",
            typeof(bool),
            typeof(DataGridCopyableCellsBehavior));

    private static readonly DependencyProperty BindingPathProperty =
        DependencyProperty.RegisterAttached(
            "BindingPath",
            typeof(string),
            typeof(DataGridCopyableCellsBehavior));

    private static readonly DependencyProperty BindingStringFormatProperty =
        DependencyProperty.RegisterAttached(
            "BindingStringFormat",
            typeof(string),
            typeof(DataGridCopyableCellsBehavior));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridCopyableCellsBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static bool GetIsProcessing(DependencyObject obj) => (bool)obj.GetValue(IsProcessingProperty);
    private static void SetIsProcessing(DependencyObject obj, bool value) => obj.SetValue(IsProcessingProperty, value);

    private static bool GetIsConvertedColumn(DependencyObject obj) => (bool)obj.GetValue(IsConvertedColumnProperty);
    private static void SetIsConvertedColumn(DependencyObject obj, bool value) => obj.SetValue(IsConvertedColumnProperty, value);

    private static string GetBindingPath(DependencyObject obj) => (string)obj.GetValue(BindingPathProperty);
    private static void SetBindingPath(DependencyObject obj, string value) => obj.SetValue(BindingPathProperty, value);

    private static string? GetBindingStringFormat(DependencyObject obj) => (string?)obj.GetValue(BindingStringFormatProperty);
    private static void SetBindingStringFormat(DependencyObject obj, string? value) => obj.SetValue(BindingStringFormatProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
            return;

        if ((bool)e.NewValue)
            Attach(grid);
        else
            Detach(grid);
    }

    private static void Attach(DataGrid grid)
    {
        grid.Loaded -= Grid_Loaded;
        grid.Loaded += Grid_Loaded;
        grid.Columns.CollectionChanged -= Grid_ColumnsChanged;
        grid.Columns.CollectionChanged += Grid_ColumnsChanged;
        grid.PreviewKeyDown -= Grid_PreviewKeyDown;
        grid.PreviewKeyDown += Grid_PreviewKeyDown;

        if (grid.IsLoaded)
            ConvertColumns(grid);
    }

    private static void Detach(DataGrid grid)
    {
        grid.Loaded -= Grid_Loaded;
        grid.Columns.CollectionChanged -= Grid_ColumnsChanged;
        grid.PreviewKeyDown -= Grid_PreviewKeyDown;
    }

    private static void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid grid)
            ConvertColumns(grid);
    }

    private static void Grid_ColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid || GetIsProcessing(grid))
            return;

        ConvertColumns(grid);
    }

    private static void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.C || Keyboard.Modifiers != ModifierKeys.Control)
            return;

        if (sender is not DataGrid grid || !GetIsEnabled(grid))
            return;

        if (Keyboard.FocusedElement is TextBox textBox)
        {
            if (textBox.SelectionLength > 0)
                return;

            if (!textBox.IsReadOnly)
                return;
        }

        if (grid.SelectedItems.Count == 0)
            return;

        CopySelectedRowsToClipboard(grid);
        e.Handled = true;
    }

    private static void ConvertColumns(DataGrid grid)
    {
        if (GetIsProcessing(grid))
            return;

        SetIsProcessing(grid, true);
        try
        {
            // Snapshot first — mutating Columns while iterating causes DisplayIndex out-of-range.
            var toConvert = grid.Columns
                .OfType<DataGridTextColumn>()
                .Where(c => !GetIsConvertedColumn(c))
                .ToList();

            foreach (var textColumn in toConvert)
            {
                var index = grid.Columns.IndexOf(textColumn);
                if (index < 0)
                    continue;

                var templateColumn = CreateTemplateColumn(grid, textColumn);
                grid.Columns.RemoveAt(index);
                grid.Columns.Insert(index, templateColumn);
            }
        }
        finally
        {
            SetIsProcessing(grid, false);
        }
    }

    private static DataGridTemplateColumn CreateTemplateColumn(DataGrid grid, DataGridTextColumn textColumn)
    {
        var templateColumn = new DataGridTemplateColumn
        {
            Header = textColumn.Header,
            Width = textColumn.Width,
            MinWidth = textColumn.MinWidth,
            MaxWidth = textColumn.MaxWidth,
            Visibility = textColumn.Visibility,
            SortMemberPath = textColumn.SortMemberPath,
            CanUserSort = textColumn.CanUserSort,
            CanUserReorder = textColumn.CanUserReorder,
            CanUserResize = textColumn.CanUserResize,
            // Do not set DisplayIndex during Insert — WPF throws IndexOutOfRange on tabbed grids.
            IsReadOnly = true,
            CellTemplate = CreateCellTemplate(grid, textColumn),
        };

        CopyFilterProperties(textColumn, templateColumn);
        StoreBindingMetadata(textColumn, templateColumn);
        SetIsConvertedColumn(templateColumn, true);

        return templateColumn;
    }

    private static DataTemplate CreateCellTemplate(DataGrid grid, DataGridTextColumn textColumn)
    {
        var textBoxFactory = new FrameworkElementFactory(typeof(TextBox));
        textBoxFactory.SetValue(TextBox.IsReadOnlyProperty, true);
        textBoxFactory.SetValue(TextBox.IsReadOnlyCaretVisibleProperty, false);
        textBoxFactory.SetValue(TextBox.BackgroundProperty, Brushes.Transparent);
        textBoxFactory.SetValue(TextBox.BorderThicknessProperty, new Thickness(0));
        textBoxFactory.SetValue(TextBox.PaddingProperty, new Thickness(0));
        textBoxFactory.SetValue(TextBox.VerticalAlignmentProperty, VerticalAlignment.Center);
        textBoxFactory.SetValue(TextBox.FocusVisualStyleProperty, null);
        textBoxFactory.SetValue(FrameworkElement.CursorProperty, Cursors.IBeam);

        var style = BuildTextBoxStyle(grid, textColumn.ElementStyle);
        if (style != null)
            textBoxFactory.SetValue(FrameworkElement.StyleProperty, style);

        if (textColumn.Binding != null)
            textBoxFactory.SetBinding(TextBox.TextProperty, CloneBinding(textColumn.Binding));

        return new DataTemplate { VisualTree = textBoxFactory };
    }

    private static Style? BuildTextBoxStyle(DataGrid grid, Style? elementStyle)
    {
        var baseStyle = grid.TryFindResource("CopyableDataGridTextBox") as Style;
        if (baseStyle == null && elementStyle == null)
            return null;

        if (elementStyle == null)
            return baseStyle;

        var merged = baseStyle != null
            ? new Style(typeof(TextBox), baseStyle)
            : new Style(typeof(TextBox));

        foreach (var setter in elementStyle.Setters)
            merged.Setters.Add(setter);

        return merged;
    }

    private static void CopyFilterProperties(DataGridColumn from, DataGridColumn to)
    {
        DataGridColumnFilterBehavior.SetFilterPropertyPath(to,
            DataGridColumnFilterBehavior.GetFilterPropertyPath(from));
        DataGridColumnFilterBehavior.SetIsFilterable(to,
            DataGridColumnFilterBehavior.GetIsFilterable(from));
        DataGridColumnFilterBehavior.SetFilterText(to,
            DataGridColumnFilterBehavior.GetFilterText(from));
    }

    private static void StoreBindingMetadata(DataGridTextColumn from, DataGridColumn to)
    {
        if (from.Binding is Binding binding && binding.Path.Path is { Length: > 0 } path)
        {
            SetBindingPath(to, path);
            if (!string.IsNullOrEmpty(binding.StringFormat))
                SetBindingStringFormat(to, binding.StringFormat);
            return;
        }

        if (!string.IsNullOrWhiteSpace(from.SortMemberPath))
            SetBindingPath(to, from.SortMemberPath);
    }

    private static BindingBase CloneBinding(BindingBase binding)
    {
        if (binding is not Binding source)
            return binding;

        var clone = new Binding
        {
            Path = source.Path,
            Mode = BindingMode.OneWay,
            Converter = source.Converter,
            ConverterParameter = source.ConverterParameter,
            ConverterCulture = source.ConverterCulture,
            StringFormat = source.StringFormat,
            FallbackValue = source.FallbackValue,
            TargetNullValue = source.TargetNullValue,
            ValidatesOnDataErrors = source.ValidatesOnDataErrors,
            ValidatesOnExceptions = source.ValidatesOnExceptions,
            NotifyOnSourceUpdated = source.NotifyOnSourceUpdated,
            NotifyOnTargetUpdated = source.NotifyOnTargetUpdated,
            NotifyOnValidationError = source.NotifyOnValidationError,
        };

        if (source.RelativeSource != null)
            clone.RelativeSource = source.RelativeSource;
        else if (source.Source != null)
            clone.Source = source.Source;
        else if (!string.IsNullOrEmpty(source.ElementName))
            clone.ElementName = source.ElementName;

        return clone;
    }

    private static void CopySelectedRowsToClipboard(DataGrid grid)
    {
        var columns = grid.Columns
            .Where(c => c.Visibility == Visibility.Visible && IsDataColumn(c))
            .ToList();

        if (columns.Count == 0)
            return;

        var sb = new StringBuilder();
        foreach (var item in grid.SelectedItems)
        {
            if (item == null)
                continue;

            var values = columns.Select(col => GetCellDisplayText(item, col));
            sb.AppendLine(string.Join("\t", values));
        }

        var text = sb.ToString().TrimEnd();
        if (text.Length > 0)
            Clipboard.SetText(text);
    }

    private static bool IsDataColumn(DataGridColumn column)
    {
        if (column is DataGridTextColumn)
            return true;

        if (!string.IsNullOrWhiteSpace(GetBindingPath(column)))
            return true;

        if (!string.IsNullOrWhiteSpace(column.SortMemberPath))
            return true;

        return column is DataGridTemplateColumn && GetIsConvertedColumn(column);
    }

    private static string GetCellDisplayText(object item, DataGridColumn column)
    {
        var path = GetBindingPath(column);
        if (string.IsNullOrWhiteSpace(path))
            path = column.SortMemberPath;

        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var value = GetNestedPropertyValue(item, path);
        var format = GetBindingStringFormat(column);

        if (!string.IsNullOrEmpty(format) && value != null)
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, format, value);
            }
            catch (FormatException)
            {
                // fallback below
            }
        }

        return FormatPropertyValue(value);
    }

    private static object? GetNestedPropertyValue(object obj, string path)
    {
        object? current = obj;
        foreach (var part in path.Split('.'))
        {
            if (current == null)
                return null;

            var property = current.GetType().GetProperty(part, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property == null)
                return null;

            current = property.GetValue(current);
        }

        return current;
    }

    private static string FormatPropertyValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dt => dt.ToString("yyyy/MM/dd", CultureInfo.CurrentCulture),
            DateTimeOffset dto => dto.ToString("yyyy/MM/dd", CultureInfo.CurrentCulture),
            bool b => b ? "True" : "False",
            IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty,
        };
    }
}
