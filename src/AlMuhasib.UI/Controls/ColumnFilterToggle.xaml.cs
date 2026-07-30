using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Behaviors;

namespace AlMuhasib.UI.Controls;

public partial class ColumnFilterToggle : UserControl
{
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(ColumnFilterToggle),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ActiveFilterCountProperty =
        DependencyProperty.Register(
            nameof(ActiveFilterCount),
            typeof(int),
            typeof(ColumnFilterToggle),
            new PropertyMetadata(0));

    public static readonly DependencyProperty ClearFiltersCommandProperty =
        DependencyProperty.Register(
            nameof(ClearFiltersCommand),
            typeof(System.Windows.Input.ICommand),
            typeof(ColumnFilterToggle),
            new PropertyMetadata(null));

    public static readonly DependencyProperty TargetDataGridProperty =
        DependencyProperty.Register(
            nameof(TargetDataGrid),
            typeof(DataGrid),
            typeof(ColumnFilterToggle),
            new PropertyMetadata(null));

    public static readonly DependencyProperty OnDarkBackgroundProperty =
        DependencyProperty.Register(
            nameof(OnDarkBackground),
            typeof(bool),
            typeof(ColumnFilterToggle),
            new PropertyMetadata(false));

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public bool OnDarkBackground
    {
        get => (bool)GetValue(OnDarkBackgroundProperty);
        set => SetValue(OnDarkBackgroundProperty, value);
    }

    public int ActiveFilterCount
    {
        get => (int)GetValue(ActiveFilterCountProperty);
        set => SetValue(ActiveFilterCountProperty, value);
    }

    public System.Windows.Input.ICommand? ClearFiltersCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(ClearFiltersCommandProperty);
        set => SetValue(ClearFiltersCommandProperty, value);
    }

    public DataGrid? TargetDataGrid
    {
        get => (DataGrid?)GetValue(TargetDataGridProperty);
        set => SetValue(TargetDataGridProperty, value);
    }

    public ColumnFilterToggle()
    {
        InitializeComponent();
        ClearButton.Click += OnClearClicked;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => ResolveTargetDataGrid();

    private void ResolveTargetDataGrid()
    {
        if (TargetDataGrid is not null)
            return;

        DependencyObject? ancestor = this;
        while (ancestor is not null)
        {
            var dataGrid = FindFilterableDataGridInSubtree(ancestor);
            if (dataGrid is not null)
            {
                TargetDataGrid = dataGrid;
                return;
            }

            ancestor = LogicalTreeHelper.GetParent(ancestor);
        }
    }

    private static DataGrid? FindFilterableDataGridInSubtree(DependencyObject root)
    {
        if (root is DataGrid grid && DataGridColumnFilterBehavior.GetIsEnabled(grid))
            return grid;

        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is DependencyObject dependencyObject)
            {
                var found = FindFilterableDataGridInSubtree(dependencyObject);
                if (found is not null)
                    return found;
            }
        }

        return null;
    }

    private void OnClearClicked(object sender, RoutedEventArgs e)
    {
        if (TargetDataGrid is not null)
            DataGridColumnFilterBehavior.ClearAllFilters(TargetDataGrid);

        ClearFiltersCommand?.Execute(null);
    }
}
