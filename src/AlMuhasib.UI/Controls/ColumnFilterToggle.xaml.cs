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

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
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
    }

    private void OnClearClicked(object sender, RoutedEventArgs e)
    {
        if (TargetDataGrid is not null)
            DataGridColumnFilterBehavior.ClearAllFilters(TargetDataGrid);

        ClearFiltersCommand?.Execute(null);
    }
}
