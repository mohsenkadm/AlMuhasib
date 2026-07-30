using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Controls;

public partial class HotelListPageShell : UserControl
{
    public HotelListPageShell()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (TargetDataGrid is not null)
            return;

        if (ListContent is DependencyObject contentRoot)
        {
            var grid = FindFilterableDataGrid(contentRoot);
            if (grid is not null)
                TargetDataGrid = grid;
        }
    }

    private static DataGrid? FindFilterableDataGrid(DependencyObject root)
    {
        if (root is DataGrid dataGrid)
            return dataGrid;

        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            var found = FindFilterableDataGrid(child);
            if (found is not null)
                return found;
        }

        foreach (var logical in LogicalTreeHelper.GetChildren(root))
        {
            if (logical is DependencyObject dep)
            {
                var found = FindFilterableDataGrid(dep);
                if (found is not null)
                    return found;
            }
        }

        return null;
    }

    public static readonly DependencyProperty PageTitleProperty =
        DependencyProperty.Register(nameof(PageTitle), typeof(string), typeof(HotelListPageShell), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ListTitleProperty =
        DependencyProperty.Register(nameof(ListTitle), typeof(string), typeof(HotelListPageShell), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ToolbarContentProperty =
        DependencyProperty.Register(nameof(ToolbarContent), typeof(object), typeof(HotelListPageShell));

    public static readonly DependencyProperty ListContentProperty =
        DependencyProperty.Register(nameof(ListContent), typeof(object), typeof(HotelListPageShell));

    public static readonly DependencyProperty ListHeaderExtraProperty =
        DependencyProperty.Register(nameof(ListHeaderExtra), typeof(object), typeof(HotelListPageShell));

    public static readonly DependencyProperty ListFooterContentProperty =
        DependencyProperty.Register(nameof(ListFooterContent), typeof(object), typeof(HotelListPageShell));

    public static readonly DependencyProperty StatsItemsSourceProperty =
        DependencyProperty.Register(nameof(StatsItemsSource), typeof(object), typeof(HotelListPageShell));

    public static readonly DependencyProperty ShowStatsBarProperty =
        DependencyProperty.Register(nameof(ShowStatsBar), typeof(bool), typeof(HotelListPageShell), new PropertyMetadata(false));

    public static readonly DependencyProperty ShowPaginationProperty =
        DependencyProperty.Register(nameof(ShowPagination), typeof(bool), typeof(HotelListPageShell), new PropertyMetadata(true));

    public static readonly DependencyProperty ShowPreviewPanelProperty =
        DependencyProperty.Register(nameof(ShowPreviewPanel), typeof(bool), typeof(HotelListPageShell), new PropertyMetadata(true));

    public static readonly DependencyProperty PreviewPanelWidthProperty =
        DependencyProperty.Register(nameof(PreviewPanelWidth), typeof(GridLength), typeof(HotelListPageShell), new PropertyMetadata(new GridLength(380)));

    public static readonly DependencyProperty HasPreviewSelectionProperty =
        DependencyProperty.Register(nameof(HasPreviewSelection), typeof(bool), typeof(HotelListPageShell), new PropertyMetadata(false));

    public static readonly DependencyProperty PreviewTitleProperty =
        DependencyProperty.Register(nameof(PreviewTitle), typeof(string), typeof(HotelListPageShell), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PreviewSubtitleProperty =
        DependencyProperty.Register(nameof(PreviewSubtitle), typeof(string), typeof(HotelListPageShell), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PreviewIconKindProperty =
        DependencyProperty.Register(nameof(PreviewIconKind), typeof(PackIconKind), typeof(HotelListPageShell), new PropertyMetadata(PackIconKind.InformationOutline));

    public static readonly DependencyProperty PreviewSelectedTabProperty =
        DependencyProperty.Register(nameof(PreviewSelectedTab), typeof(int), typeof(HotelListPageShell), new PropertyMetadata(0));

    public static readonly DependencyProperty PreviewSummaryContentProperty =
        DependencyProperty.Register(nameof(PreviewSummaryContent), typeof(object), typeof(HotelListPageShell));

    public static readonly DependencyProperty PreviewHistoryContentProperty =
        DependencyProperty.Register(nameof(PreviewHistoryContent), typeof(object), typeof(HotelListPageShell));

    public static readonly DependencyProperty PreviewActionsContentProperty =
        DependencyProperty.Register(nameof(PreviewActionsContent), typeof(object), typeof(HotelListPageShell));

    public static readonly DependencyProperty ClosePreviewCommandProperty =
        DependencyProperty.Register(nameof(ClosePreviewCommand), typeof(ICommand), typeof(HotelListPageShell));

    public static readonly DependencyProperty ShowPreviewCloseButtonProperty =
        DependencyProperty.Register(nameof(ShowPreviewCloseButton), typeof(bool), typeof(HotelListPageShell), new PropertyMetadata(true));

    public static readonly DependencyProperty TargetDataGridProperty =
        DependencyProperty.Register(nameof(TargetDataGrid), typeof(DataGrid), typeof(HotelListPageShell), new PropertyMetadata(null));

    public static readonly DependencyProperty ExportCommandProperty =
        DependencyProperty.Register(nameof(ExportCommand), typeof(ICommand), typeof(HotelListPageShell), new PropertyMetadata(null));

    public static readonly DependencyProperty PrintCommandProperty =
        DependencyProperty.Register(nameof(PrintCommand), typeof(ICommand), typeof(HotelListPageShell), new PropertyMetadata(null));

    public static readonly DependencyProperty ClearFiltersCommandProperty =
        DependencyProperty.Register(nameof(ClearFiltersCommand), typeof(ICommand), typeof(HotelListPageShell), new PropertyMetadata(null));

    public static readonly DependencyProperty IsFilterPanelOpenProperty =
        DependencyProperty.Register(nameof(IsFilterPanelOpen), typeof(bool), typeof(HotelListPageShell),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ActiveFilterCountProperty =
        DependencyProperty.Register(nameof(ActiveFilterCount), typeof(int), typeof(HotelListPageShell), new PropertyMetadata(0));

    public static readonly DependencyProperty ShowCardToggleProperty =
        DependencyProperty.Register(nameof(ShowCardToggle), typeof(bool), typeof(HotelListPageShell), new PropertyMetadata(true));

    public static readonly DependencyProperty ShowFilterProperty =
        DependencyProperty.Register(nameof(ShowFilter), typeof(bool), typeof(HotelListPageShell), new PropertyMetadata(true));

    public static readonly DependencyProperty ShowExportProperty =
        DependencyProperty.Register(nameof(ShowExport), typeof(bool), typeof(HotelListPageShell), new PropertyMetadata(true));

    public static readonly DependencyProperty ShowPrintProperty =
        DependencyProperty.Register(nameof(ShowPrint), typeof(bool), typeof(HotelListPageShell), new PropertyMetadata(true));

    public string PageTitle
    {
        get => (string)GetValue(PageTitleProperty);
        set => SetValue(PageTitleProperty, value);
    }

    public string ListTitle
    {
        get => (string)GetValue(ListTitleProperty);
        set => SetValue(ListTitleProperty, value);
    }

    public object? ToolbarContent
    {
        get => GetValue(ToolbarContentProperty);
        set => SetValue(ToolbarContentProperty, value);
    }

    public object? ListContent
    {
        get => GetValue(ListContentProperty);
        set => SetValue(ListContentProperty, value);
    }

    public object? ListHeaderExtra
    {
        get => GetValue(ListHeaderExtraProperty);
        set => SetValue(ListHeaderExtraProperty, value);
    }

    public object? ListFooterContent
    {
        get => GetValue(ListFooterContentProperty);
        set => SetValue(ListFooterContentProperty, value);
    }

    public object? StatsItemsSource
    {
        get => GetValue(StatsItemsSourceProperty);
        set => SetValue(StatsItemsSourceProperty, value);
    }

    public bool ShowStatsBar
    {
        get => (bool)GetValue(ShowStatsBarProperty);
        set => SetValue(ShowStatsBarProperty, value);
    }

    public bool ShowPagination
    {
        get => (bool)GetValue(ShowPaginationProperty);
        set => SetValue(ShowPaginationProperty, value);
    }

    public bool ShowPreviewPanel
    {
        get => (bool)GetValue(ShowPreviewPanelProperty);
        set => SetValue(ShowPreviewPanelProperty, value);
    }

    public GridLength PreviewPanelWidth
    {
        get => (GridLength)GetValue(PreviewPanelWidthProperty);
        set => SetValue(PreviewPanelWidthProperty, value);
    }

    public bool HasPreviewSelection
    {
        get => (bool)GetValue(HasPreviewSelectionProperty);
        set => SetValue(HasPreviewSelectionProperty, value);
    }

    public string PreviewTitle
    {
        get => (string)GetValue(PreviewTitleProperty);
        set => SetValue(PreviewTitleProperty, value);
    }

    public string PreviewSubtitle
    {
        get => (string)GetValue(PreviewSubtitleProperty);
        set => SetValue(PreviewSubtitleProperty, value);
    }

    public PackIconKind PreviewIconKind
    {
        get => (PackIconKind)GetValue(PreviewIconKindProperty);
        set => SetValue(PreviewIconKindProperty, value);
    }

    public int PreviewSelectedTab
    {
        get => (int)GetValue(PreviewSelectedTabProperty);
        set => SetValue(PreviewSelectedTabProperty, value);
    }

    public object? PreviewSummaryContent
    {
        get => GetValue(PreviewSummaryContentProperty);
        set => SetValue(PreviewSummaryContentProperty, value);
    }

    public object? PreviewHistoryContent
    {
        get => GetValue(PreviewHistoryContentProperty);
        set => SetValue(PreviewHistoryContentProperty, value);
    }

    public object? PreviewActionsContent
    {
        get => GetValue(PreviewActionsContentProperty);
        set => SetValue(PreviewActionsContentProperty, value);
    }

    public ICommand? ClosePreviewCommand
    {
        get => (ICommand?)GetValue(ClosePreviewCommandProperty);
        set => SetValue(ClosePreviewCommandProperty, value);
    }

    public bool ShowPreviewCloseButton
    {
        get => (bool)GetValue(ShowPreviewCloseButtonProperty);
        set => SetValue(ShowPreviewCloseButtonProperty, value);
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

    public bool ShowFilter
    {
        get => (bool)GetValue(ShowFilterProperty);
        set => SetValue(ShowFilterProperty, value);
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
}
