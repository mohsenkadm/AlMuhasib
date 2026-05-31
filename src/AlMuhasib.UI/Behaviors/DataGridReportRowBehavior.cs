using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AlMuhasib.UI.Behaviors;

public static class DataGridReportRowBehavior
{
    public static readonly DependencyProperty DoubleClickCommandProperty =
        DependencyProperty.RegisterAttached(
            "DoubleClickCommand",
            typeof(ICommand),
            typeof(DataGridReportRowBehavior),
            new PropertyMetadata(null, OnHandlersChanged));

    public static readonly DependencyProperty SelectionChangedCommandProperty =
        DependencyProperty.RegisterAttached(
            "SelectionChangedCommand",
            typeof(ICommand),
            typeof(DataGridReportRowBehavior),
            new PropertyMetadata(null, OnHandlersChanged));

    public static readonly DependencyProperty EnableExtendedSelectionProperty =
        DependencyProperty.RegisterAttached(
            "EnableExtendedSelection",
            typeof(bool),
            typeof(DataGridReportRowBehavior),
            new PropertyMetadata(false, OnEnableExtendedSelectionChanged));

    public static ICommand? GetDoubleClickCommand(DependencyObject obj) =>
        (ICommand?)obj.GetValue(DoubleClickCommandProperty);

    public static void SetDoubleClickCommand(DependencyObject obj, ICommand? value) =>
        obj.SetValue(DoubleClickCommandProperty, value);

    public static ICommand? GetSelectionChangedCommand(DependencyObject obj) =>
        (ICommand?)obj.GetValue(SelectionChangedCommandProperty);

    public static void SetSelectionChangedCommand(DependencyObject obj, ICommand? value) =>
        obj.SetValue(SelectionChangedCommandProperty, value);

    public static bool GetEnableExtendedSelection(DependencyObject obj) =>
        (bool)obj.GetValue(EnableExtendedSelectionProperty);

    public static void SetEnableExtendedSelection(DependencyObject obj, bool value) =>
        obj.SetValue(EnableExtendedSelectionProperty, value);

    private static void OnEnableExtendedSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGrid grid && (bool)e.NewValue)
            grid.SelectionMode = DataGridSelectionMode.Extended;
    }

    private static void OnHandlersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
            return;

        grid.MouseDoubleClick -= Grid_MouseDoubleClick;
        grid.SelectionChanged -= Grid_SelectionChanged;

        if (GetDoubleClickCommand(grid) is not null)
            grid.MouseDoubleClick += Grid_MouseDoubleClick;

        if (GetSelectionChangedCommand(grid) is not null)
            grid.SelectionChanged += Grid_SelectionChanged;
    }

    private static void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;

        if (e.OriginalSource is not DependencyObject source)
            return;

        var row = FindParent<DataGridRow>(source);
        if (row?.Item is null)
            return;

        var cmd = GetDoubleClickCommand(grid);
        if (cmd?.CanExecute(row.Item) == true)
            cmd.Execute(row.Item);
    }

    private static void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;

        var cmd = GetSelectionChangedCommand(grid);
        if (cmd?.CanExecute(grid) == true)
            cmd.Execute(grid);
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T match)
                return match;
            child = System.Windows.Media.VisualTreeHelper.GetParent(child);
        }

        return null;
    }
}
