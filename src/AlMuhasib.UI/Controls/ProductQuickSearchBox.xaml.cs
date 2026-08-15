using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AlMuhasib.Core.Entities;
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
    private IProductQuickSearchHost? _host;

    public ProductQuickSearchBox()
    {
        Suggestions = [];
        InitializeComponent();
        _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _filterTimer.Tick += (_, _) =>
        {
            _filterTimer.Stop();
            RefreshSuggestions();
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
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextRefresh || _isSelecting)
            return;
        ScheduleRefresh();
    }

    private void ScheduleRefresh()
    {
        _filterTimer.Stop();
        _filterTimer.Start();
    }

    private void SearchBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ResolveHost();
        RefreshSuggestions(forceOpen: true);
    }

    private void SearchBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // تأخير بسيط للسماح بالنقر على الاقتراح (الـ Popup خارج نطاق التركيز)
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (_isSelecting || !SuggestionsPopup.IsOpen)
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

        if (e.Key == Key.Enter && Suggestions.Count > 0)
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
        try
        {
            SelectedProduct = suggestion.Product;
            Text = suggestion.Product.Name;
            ClosePopup();
            SearchBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
        finally
        {
            _isSelecting = false;
        }
    }

    private void RefreshSuggestions(bool forceOpen = false)
    {
        ResolveHost();
        var catalog = _host?.QuickSearchCatalog;
        Suggestions.Clear();

        var term = Text?.Trim() ?? string.Empty;
        if (catalog is null)
        {
            EmptyHint.Text = "تعذر تحميل كتالوج البحث";
            if (forceOpen)
                OpenPopup();
            return;
        }

        // لا تفتح القائمة إن كان النص يطابق المنتج المحدد بالكامل دون تغيير
        if (!forceOpen
            && SelectedProduct is not null
            && string.Equals(SelectedProduct.Name, term, StringComparison.OrdinalIgnoreCase))
        {
            ClosePopup();
            return;
        }

        var results = catalog.Search(term);
        foreach (var item in results)
            Suggestions.Add(item);

        EmptyHint.Text = Suggestions.Count == 0
            ? "لا توجد مواد مطابقة"
            : $"{Suggestions.Count} مادة — انقر للاختيار";

        // لا تفتح الاقتراحات عند التعبئة البرمجية للنص (مثل استعادة قائمة الانتظار)
        if (forceOpen || IsKeyboardFocusWithin)
            OpenPopup();
        else
            ClosePopup();
    }

    private void OpenPopup()
    {
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

    private void ClosePopup()
    {
        SuggestionsPopup.IsOpen = false;
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
