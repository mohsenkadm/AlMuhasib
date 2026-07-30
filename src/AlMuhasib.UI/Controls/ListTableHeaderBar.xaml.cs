using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AlMuhasib.UI.Controls;

public partial class ListTableHeaderBar : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(ListTableHeaderBar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty TargetDataGridProperty =
        DependencyProperty.Register(nameof(TargetDataGrid), typeof(DataGrid), typeof(ListTableHeaderBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ExportCommandProperty =
        DependencyProperty.Register(nameof(ExportCommand), typeof(ICommand), typeof(ListTableHeaderBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PrintCommandProperty =
        DependencyProperty.Register(nameof(PrintCommand), typeof(ICommand), typeof(ListTableHeaderBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty ClearFiltersCommandProperty =
        DependencyProperty.Register(nameof(ClearFiltersCommand), typeof(ICommand), typeof(ListTableHeaderBar),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsFilterPanelOpenProperty =
        DependencyProperty.Register(nameof(IsFilterPanelOpen), typeof(bool), typeof(ListTableHeaderBar),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ActiveFilterCountProperty =
        DependencyProperty.Register(nameof(ActiveFilterCount), typeof(int), typeof(ListTableHeaderBar),
            new PropertyMetadata(0));

    public static readonly DependencyProperty ShowCardToggleProperty =
        DependencyProperty.Register(nameof(ShowCardToggle), typeof(bool), typeof(ListTableHeaderBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowExportProperty =
        DependencyProperty.Register(nameof(ShowExport), typeof(bool), typeof(ListTableHeaderBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowPrintProperty =
        DependencyProperty.Register(nameof(ShowPrint), typeof(bool), typeof(ListTableHeaderBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ShowFilterProperty =
        DependencyProperty.Register(nameof(ShowFilter), typeof(bool), typeof(ListTableHeaderBar),
            new PropertyMetadata(true));

    public static readonly DependencyProperty ExtraContentProperty =
        DependencyProperty.Register(nameof(ExtraContent), typeof(object), typeof(ListTableHeaderBar),
            new PropertyMetadata(null));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public DataGrid? TargetDataGrid
    {
        get => (DataGrid?)GetValue(TargetDataGridProperty);
        set => SetValue(TargetDataGridProperty, value);
    }

    public ICommand? ExportCommand
    {
        get => (ICommand?)GetValue(ExportCommandProperty);
        set => SetValue(ExportCommandProperty, value);
    }

    public ICommand? PrintCommand
    {
        get => (ICommand?)GetValue(PrintCommandProperty);
        set => SetValue(PrintCommandProperty, value);
    }

    public ICommand? ClearFiltersCommand
    {
        get => (ICommand?)GetValue(ClearFiltersCommandProperty);
        set => SetValue(ClearFiltersCommandProperty, value);
    }

    public bool IsFilterPanelOpen
    {
        get => (bool)GetValue(IsFilterPanelOpenProperty);
        set => SetValue(IsFilterPanelOpenProperty, value);
    }

    public int ActiveFilterCount
    {
        get => (int)GetValue(ActiveFilterCountProperty);
        set => SetValue(ActiveFilterCountProperty, value);
    }

    public bool ShowCardToggle
    {
        get => (bool)GetValue(ShowCardToggleProperty);
        set => SetValue(ShowCardToggleProperty, value);
    }

    public bool ShowExport
    {
        get => (bool)GetValue(ShowExportProperty);
        set => SetValue(ShowExportProperty, value);
    }

    public bool ShowPrint
    {
        get => (bool)GetValue(ShowPrintProperty);
        set => SetValue(ShowPrintProperty, value);
    }

    public bool ShowFilter
    {
        get => (bool)GetValue(ShowFilterProperty);
        set => SetValue(ShowFilterProperty, value);
    }

    public object? ExtraContent
    {
        get => GetValue(ExtraContentProperty);
        set => SetValue(ExtraContentProperty, value);
    }

    public ListTableHeaderBar() => InitializeComponent();
}
