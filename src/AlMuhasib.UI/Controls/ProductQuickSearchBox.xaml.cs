using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AlMuhasib.Core.Entities;
using AlMuhasib.UI.Behaviors;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Controls;

public partial class ProductQuickSearchBox : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(ProductQuickSearchBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    public static readonly DependencyProperty SelectedProductProperty =
        DependencyProperty.Register(
            nameof(SelectedProduct),
            typeof(Product),
            typeof(ProductQuickSearchBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedProductChanged));

    public static readonly DependencyProperty SuggestionsProperty =
        DependencyProperty.Register(
            nameof(Suggestions),
            typeof(ObservableCollection<ProductSearchSuggestion>),
            typeof(ProductQuickSearchBox),
            new PropertyMetadata(null));

    private readonly DispatcherTimer _filterTimer;
    private bool _suppressTextRefresh;
    private bool _isSelecting;
    private bool _suppressPopup;
    private int _refreshGeneration;
    private IProductQuickSearchHost? _host;

    public ProductQuickSearchBox()
    {
        Suggestions = [];
        InitializeComponent();
        _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _filterTimer.Tick += (_, _) =>
        {
            _filterTimer.Stop();
            RunRefreshAsync();
        };
        Loaded += OnLoaded;
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Product? SelectedProduct
    {
        get => (Product?)GetValue(SelectedProductProperty);
        set => SetValue(SelectedProductProperty, value);
    }

    public ObservableCollection<ProductSearchSuggestion> Suggestions
    {
        get => (ObservableCollection<ProductSearchSuggestion>)GetValue(SuggestionsProperty);
        set => SetValue(SuggestionsProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e) => ResolveHost();

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ProductQuickSearchBox box && !box._suppressTextRefresh && !box._isSelecting)
            box.ScheduleRefresh();
    }

    private static void OnSelectedProductChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ProductQuickSearchBox box || box._isSelecting)
            return;

        if (e.NewValue is Product product && !string.Equals(box.Text, product.Name, StringComparison.Ordinal))
        {
            box._suppressTextRefresh = true;
            box.Text = product.Name;
            box._suppressTextRefresh = false;
        }

        if (e.NewValue is not null)
            box.ClosePopup();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextRefresh || _isSelecting || _suppressPopup)
            return;
        ScheduleRefresh();
    }

    private void ScheduleRefresh()
    {
        _filterTimer.Stop();
        _filterTimer.Start();
    }

    private async void RunRefreshAsync(bool forceOpen = false)
    {
        await RefreshSuggestionsAsync(forceOpen);
    }

    private void SearchBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ResolveHost();
        if (_suppressPopup || _isSelecting)
            return;

        // لا تفتح الاقتراحات لصف فيه منتج مختار مسبقاً — فقط عند البحث في صف فارغ
        if (SelectedProduct is not null
            && string.Equals(SelectedProduct.Name, Text?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            ClosePopup();
            return;
        }

        RunRefreshAsync(forceOpen: true);
    }

    private void SearchBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (_isSelecting)
                return;

            if (SuggestionsPopup.Child is FrameworkElement popupChild && popupChild.IsMouseOver)
                return;

            if (!IsKeyboardFocusWithin)
                ClosePopup();
        });
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            ClosePopup();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter && Suggestions.Count > 0 && SuggestionsPopup.IsOpen)
        {
            SelectSuggestion(Suggestions[0]);
            e.Handled = true;
        }
    }

    private void Suggestion_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ProductSearchSuggestion suggestion })
        {
            SelectSuggestion(suggestion);
            e.Handled = true;
        }
    }

    private void SelectSuggestion(ProductSearchSuggestion suggestion)
    {
        _isSelecting = true;
        _suppressPopup = true;
        _filterTimer.Stop();
        _refreshGeneration++;
        try
        {
            SelectedProduct = suggestion.Product;
            Text = suggestion.Product.Name;
            ClosePopup();
            CloseAllPopupsInGrid(FindAncestor<DataGrid>(this));

            if (DataContext is InvoiceItemRow row)
            {
                var qty = QuickQuantityDialog.Prompt(suggestion.Product.Name, defaultQuantity: 1m);
                if (qty is > 0)
                    row.Quantity = qty.Value;
            }

            ClosePopup();
            CloseAllPopupsInGrid(FindAncestor<DataGrid>(this));

            // امنع Enter المتبقي من سلوك الجدول بعد إغلاق الحوار
            InvoiceDataGridBehavior.SuppressEnterOnce();
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                try
                {
                    FocusNextInvoiceProductRow();
                }
                finally
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () =>
                    {
                        _suppressPopup = false;
                        _isSelecting = false;
                    });
                }
            });
        }
        catch
        {
            _suppressPopup = false;
            _isSelecting = false;
            throw;
        }
    }

    private void FocusNextInvoiceProductRow()
    {
        var grid = FindAncestor<DataGrid>(this);
        if (grid is null)
            return;

        CloseAllPopupsInGrid(grid);

        var currentIndex = grid.Items.IndexOf(DataContext);
        if (currentIndex < 0)
            return;

        if (currentIndex == grid.Items.Count - 1)
        {
            var addCmd = InvoiceDataGridBehavior.GetAddRowCommand(grid);
            if (addCmd is not null && addCmd.CanExecute(null))
                addCmd.Execute(null);
        }

        var nextIndex = Math.Min(currentIndex + 1, grid.Items.Count - 1);
        if (nextIndex < 0)
            return;

        var nextItem = grid.Items[nextIndex];
        var productColumn = grid.Columns.Count > 1 ? grid.Columns[1] : grid.Columns.FirstOrDefault();
        if (productColumn is null)
            return;

        grid.ScrollIntoView(nextItem);
        if (grid.SelectionUnit == DataGridSelectionUnit.FullRow)
            grid.SelectedItem = nextItem;
        grid.CurrentCell = new DataGridCellInfo(nextItem, productColumn);
        try { grid.BeginEdit(); } catch (InvalidOperationException) { }

        Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            CloseAllPopupsInGrid(grid);

            var rowContainer = grid.ItemContainerGenerator.ContainerFromItem(nextItem) as DependencyObject;
            var searchBox = FindVisualChild<ProductQuickSearchBox>(rowContainer);
            if (searchBox is null)
                return;

            // الصف التالي فارغ — افتح بحثه فقط
            searchBox._suppressPopup = false;
            var inner = FindVisualChild<TextBox>(searchBox);
            if (inner is null)
                return;

            inner.Focus();
            inner.SelectAll();
            if (searchBox.SelectedProduct is null && string.IsNullOrWhiteSpace(searchBox.Text))
                searchBox.RunRefreshAsync(forceOpen: true);
        });
    }

    public void ClosePopup()
    {
        _filterTimer.Stop();
        SuggestionsPopup.IsOpen = false;
    }

    private static void CloseAllPopupsInGrid(DataGrid? grid)
    {
        if (grid is null)
            return;

        for (var i = 0; i < grid.Items.Count; i++)
        {
            if (grid.ItemContainerGenerator.ContainerFromIndex(i) is not DependencyObject row)
                continue;
            FindVisualChild<ProductQuickSearchBox>(row)?.ClosePopup();
        }
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
                return match;
            current = VisualTreeHelper.GetParent(current)
                      ?? (current as FrameworkElement)?.Parent as DependencyObject;
        }

        return null;
    }

    private static T? FindVisualChild<T>(DependencyObject? parent) where T : DependencyObject
    {
        if (parent is null)
            return null;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found)
                return found;
            var nested = FindVisualChild<T>(child);
            if (nested is not null)
                return nested;
        }

        return null;
    }

    private async Task RefreshSuggestionsAsync(bool forceOpen = false)
    {
        if (_suppressPopup || _isSelecting)
        {
            ClosePopup();
            return;
        }

        var generation = ++_refreshGeneration;
        ResolveHost();
        var catalog = _host?.QuickSearchCatalog;
        Suggestions.Clear();

        var term = Text?.Trim() ?? string.Empty;
        if (catalog is null)
        {
            EmptyHint.Text = "تعذر تحميل كتالوج البحث";
            if (forceOpen && !_suppressPopup)
                OpenPopup();
            return;
        }

        if (SelectedProduct is not null
            && string.Equals(SelectedProduct.Name, term, StringComparison.OrdinalIgnoreCase))
        {
            ClosePopup();
            return;
        }

        var results = await catalog.SearchAsync(
            term,
            string.IsNullOrWhiteSpace(term)
                ? ProductQuickSearchCatalog.DefaultPreviewCount
                : ProductQuickSearchCatalog.DefaultSearchCount);

        if (generation != _refreshGeneration || _suppressPopup || _isSelecting)
        {
            ClosePopup();
            return;
        }

        foreach (var item in results)
            Suggestions.Add(item);

        EmptyHint.Text = Suggestions.Count == 0
            ? (string.IsNullOrWhiteSpace(term)
                ? "اكتب للبحث في جميع المنتجات"
                : "لا توجد مواد مطابقة")
            : string.IsNullOrWhiteSpace(term)
                ? $"أول {Suggestions.Count} مادة — اكتب للبحث في الكل"
                : $"{Suggestions.Count} مادة — انقر للاختيار";

        if (!_suppressPopup && !_isSelecting && (forceOpen || IsKeyboardFocusWithin))
            OpenPopup();
        else
            ClosePopup();
    }

    private void OpenPopup()
    {
        if (_suppressPopup || _isSelecting)
            return;

        if (!SuggestionsPopup.IsOpen)
        {
            SuggestionsPopup.IsOpen = true;
            PlayOpenAnimation();
        }
        else
        {
            PopupCard.Opacity = 1;
        }
    }

    private void SuggestionsPopup_Opened(object sender, EventArgs e) => PlayOpenAnimation();

    private void PlayOpenAnimation()
    {
        PopupCard.Opacity = 0;
        if (PopupCard.RenderTransform is not TranslateTransform transform)
        {
            transform = new TranslateTransform();
            PopupCard.RenderTransform = transform;
        }

        transform.Y = -10;
        PopupCard.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
        transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(-10, 0, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 }
        });
    }

    private void ResolveHost()
    {
        if (_host is not null)
            return;

        DependencyObject? current = this;
        while (current is not null)
        {
            if (current is FrameworkElement { DataContext: IProductQuickSearchHost host })
            {
                _host = host;
                return;
            }

            current = VisualTreeHelper.GetParent(current)
                      ?? (current as FrameworkElement)?.Parent;
        }
    }
}
