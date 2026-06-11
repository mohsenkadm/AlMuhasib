using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.Behaviors;

public static class DataGridColumnFilterBehavior
{
    private static readonly DependencyProperty DebounceTimerProperty =
        DependencyProperty.RegisterAttached(
            "DebounceTimer",
            typeof(DispatcherTimer),
            typeof(DataGridColumnFilterBehavior));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DataGridColumnFilterBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty IsFilterPanelOpenProperty =
        DependencyProperty.RegisterAttached(
            "IsFilterPanelOpen",
            typeof(bool),
            typeof(DataGridColumnFilterBehavior),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFilterPanelOpenChanged));

    public static readonly DependencyProperty FilterChangedCommandProperty =
        DependencyProperty.RegisterAttached(
            "FilterChangedCommand",
            typeof(ICommand),
            typeof(DataGridColumnFilterBehavior));

    public static readonly DependencyProperty ActiveFilterCountProperty =
        DependencyProperty.RegisterAttached(
            "ActiveFilterCount",
            typeof(int),
            typeof(DataGridColumnFilterBehavior),
            new PropertyMetadata(0));

    public static readonly DependencyProperty FilterPropertyPathProperty =
        DependencyProperty.RegisterAttached(
            "FilterPropertyPath",
            typeof(string),
            typeof(DataGridColumnFilterBehavior),
            new PropertyMetadata(string.Empty, OnColumnFilterMetaChanged));

    public static readonly DependencyProperty IsFilterableProperty =
        DependencyProperty.RegisterAttached(
            "IsFilterable",
            typeof(bool),
            typeof(DataGridColumnFilterBehavior),
            new PropertyMetadata(false));

    public static readonly DependencyProperty FilterTextProperty =
        DependencyProperty.RegisterAttached(
            "FilterText",
            typeof(string),
            typeof(DataGridColumnFilterBehavior),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFilterTextChanged));

    private static readonly DependencyProperty OwnerDataGridProperty =
        DependencyProperty.RegisterAttached(
            "OwnerDataGrid",
            typeof(DataGrid),
            typeof(DataGridColumnFilterBehavior));

    private static DataGrid? GetOwnerDataGrid(DependencyObject obj) => (DataGrid?)obj.GetValue(OwnerDataGridProperty);

    private static void SetOwnerDataGrid(DependencyObject obj, DataGrid? value) =>
        obj.SetValue(OwnerDataGridProperty, value);

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static bool GetIsFilterPanelOpen(DependencyObject obj) => (bool)obj.GetValue(IsFilterPanelOpenProperty);
    public static void SetIsFilterPanelOpen(DependencyObject obj, bool value) => obj.SetValue(IsFilterPanelOpenProperty, value);

    public static ICommand? GetFilterChangedCommand(DependencyObject obj) => (ICommand?)obj.GetValue(FilterChangedCommandProperty);
    public static void SetFilterChangedCommand(DependencyObject obj, ICommand? value) => obj.SetValue(FilterChangedCommandProperty, value);

    public static int GetActiveFilterCount(DependencyObject obj) => (int)obj.GetValue(ActiveFilterCountProperty);
    public static void SetActiveFilterCount(DependencyObject obj, int value) => obj.SetValue(ActiveFilterCountProperty, value);

    public static string GetFilterPropertyPath(DependencyObject obj) => (string)obj.GetValue(FilterPropertyPathProperty);
    public static void SetFilterPropertyPath(DependencyObject obj, string value) => obj.SetValue(FilterPropertyPathProperty, value);

    public static bool GetIsFilterable(DependencyObject obj) => (bool)obj.GetValue(IsFilterableProperty);
    public static void SetIsFilterable(DependencyObject obj, bool value) => obj.SetValue(IsFilterableProperty, value);

    public static string GetFilterText(DependencyObject obj) => (string)obj.GetValue(FilterTextProperty);
    public static void SetFilterText(DependencyObject obj, string value) => obj.SetValue(FilterTextProperty, value);

    public static void ClearAllFilters(DataGrid grid)
    {
        foreach (var column in grid.Columns)
            SetFilterText(column, string.Empty);

        SetActiveFilterCount(grid, 0);
        RaiseFilterChanged(grid, immediate: true);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
            return;

        if ((bool)e.NewValue)
        {
            grid.Loaded -= Grid_Loaded;
            grid.Loaded += Grid_Loaded;
            grid.PreviewKeyDown -= Grid_PreviewKeyDown;
            grid.PreviewKeyDown += Grid_PreviewKeyDown;
            grid.Columns.CollectionChanged -= Grid_ColumnsChanged;
            grid.Columns.CollectionChanged += Grid_ColumnsChanged;

            RegisterColumnOwners(grid);

            if (grid.IsLoaded)
                ApplyFilterHeaderStyle(grid);
        }
        else
        {
            grid.Loaded -= Grid_Loaded;
            grid.PreviewKeyDown -= Grid_PreviewKeyDown;
            grid.Columns.CollectionChanged -= Grid_ColumnsChanged;
            UnregisterColumnOwners(grid);
        }
    }

    private static void Grid_ColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (sender is not DataGrid grid)
            return;

        if (e.OldItems is not null)
        {
            foreach (DataGridColumn column in e.OldItems)
                SetOwnerDataGrid(column, null);
        }

        if (e.NewItems is not null)
        {
            foreach (DataGridColumn column in e.NewItems)
                SetOwnerDataGrid(column, grid);
        }
    }

    private static void RegisterColumnOwners(DataGrid grid)
    {
        foreach (var column in grid.Columns)
            SetOwnerDataGrid(column, grid);
    }

    private static void UnregisterColumnOwners(DataGrid grid)
    {
        foreach (var column in grid.Columns)
            SetOwnerDataGrid(column, null);
    }

    private static void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is DataGrid grid)
        {
            RegisterColumnOwners(grid);
            ApplyFilterHeaderStyle(grid);
        }
    }

    private static void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DataGrid grid || !GetIsEnabled(grid))
            return;

        if (e.Key == Key.Escape && GetIsFilterPanelOpen(grid))
        {
            SetIsFilterPanelOpen(grid, false);
            e.Handled = true;
        }
        else if (e.Key == Key.F && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            SetIsFilterPanelOpen(grid, !GetIsFilterPanelOpen(grid));
            e.Handled = true;
        }
    }

    private static void ApplyFilterHeaderStyle(DataGrid grid)
    {
        var style = grid.TryFindResource("FilterableDataGridColumnHeader") as Style;
        if (style is not null)
            grid.ColumnHeaderStyle = style;
    }

    private static void OnFilterPanelOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGrid grid && !(bool)e.NewValue)
            return;

        // Panel opened — focus first filter box after animation
        if (d is DataGrid g && (bool)e.NewValue)
        {
            g.Dispatcher.BeginInvoke(() => FocusFirstFilterBox(g), DispatcherPriority.Input);
        }
    }

    private static void FocusFirstFilterBox(DataGrid grid)
    {
        grid.UpdateLayout();
        var header = grid.Columns
            .Select(c => GetFilterPropertyPath(c))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .SelectMany(_ => FindVisualChildren<TextBox>(grid))
            .FirstOrDefault(tb => tb.Name == "ColumnFilterBox" && tb.IsVisible);

        header?.Focus();
    }

    private static void OnColumnFilterMetaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DataGridColumn && !string.IsNullOrWhiteSpace(e.NewValue as string))
            SetIsFilterable(d, true);
    }

    private static void OnFilterTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var grid = ResolveDataGrid(d);
        if (grid is null || !GetIsEnabled(grid))
            return;

        UpdateActiveFilterCount(grid);
        ScheduleFilterChanged(grid);
    }

    private static DataGrid? ResolveDataGrid(DependencyObject d) => d switch
    {
        DataGrid grid => grid,
        DataGridColumn column => GetOwnerDataGrid(column),
        _ => FindVisualParent<DataGrid>(d)
    };

    private static void UpdateActiveFilterCount(DataGrid grid)
    {
        var count = grid.Columns.Count(c =>
            GetIsFilterable(c) &&
            !string.IsNullOrWhiteSpace(GetFilterPropertyPath(c)) &&
            !string.IsNullOrWhiteSpace(GetFilterText(c)));

        SetActiveFilterCount(grid, count);
    }

    private static void ScheduleFilterChanged(DataGrid grid)
    {
        if (grid.GetValue(DebounceTimerProperty) is DispatcherTimer existing)
        {
            existing.Stop();
            existing.Tick -= DebounceTimer_Tick;
        }

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        timer.Tick += DebounceTimer_Tick;
        timer.Tag = grid;
        grid.SetValue(DebounceTimerProperty, timer);
        timer.Start();
    }

    private static void DebounceTimer_Tick(object? sender, EventArgs e)
    {
        if (sender is not DispatcherTimer timer)
            return;

        timer.Stop();
        timer.Tick -= DebounceTimer_Tick;

        if (timer.Tag is DataGrid grid)
            RaiseFilterChanged(grid, immediate: true);
    }

    private static void RaiseFilterChanged(DataGrid grid, bool immediate)
    {
        if (!immediate)
        {
            ScheduleFilterChanged(grid);
            return;
        }

        var filters = BuildFilterDictionary(grid);
        UpdateActiveFilterCount(grid);

        var cmd = GetFilterChangedCommand(grid);
        if (cmd?.CanExecute(filters) == true)
        {
            cmd.Execute(filters);
            return;
        }

        ApplyCollectionViewFilter(grid, filters);
    }

    private static void ApplyCollectionViewFilter(DataGrid grid, IReadOnlyDictionary<string, string> filters)
    {
        if (grid.ItemsSource is null)
            return;

        var view = CollectionViewSource.GetDefaultView(grid.ItemsSource);
        if (view is null)
            return;

        var active = filters
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value.Trim(), StringComparer.OrdinalIgnoreCase);

        if (active.Count == 0)
        {
            view.Filter = null;
        }
        else
        {
            view.Filter = item =>
            {
                if (item is null)
                    return false;

                return ColumnFilterEngine.Apply(new[] { item }, active).Count > 0;
            };
        }

        view.Refresh();
    }

    private static Dictionary<string, string> BuildFilterDictionary(DataGrid grid)
    {
        var filters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in grid.Columns)
        {
            if (!GetIsFilterable(column))
                continue;

            var path = GetFilterPropertyPath(column);
            if (string.IsNullOrWhiteSpace(path))
                continue;

            filters[path] = GetFilterText(column);
        }

        return filters;
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T match)
                return match;

            child = child is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(child)
                : null;
        }

        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                yield return match;

            foreach (var nested in FindVisualChildren<T>(child))
                yield return nested;
        }
    }
}
