using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Controls;

public partial class ProductPickerOverlay : UserControl
{
    private const double MaxDialogWidth = 980;
    private const double ScreenMargin = 12;

    private ScrollViewer? _pageScrollViewer;
    private ScrollBarVisibility _savedVScroll = ScrollBarVisibility.Auto;
    private ScrollBarVisibility _savedHScroll = ScrollBarVisibility.Auto;

    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(ProductPickerOverlay),
            new PropertyMetadata(false, OnIsOpenChanged));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public ProductPickerOverlay()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyOpenState(IsOpen, instant: true);
        SizeChanged += (_, _) =>
        {
            if (IsOpen)
                UpdateDialogLayout();
        };
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ProductPickerOverlay overlay)
            return;

        if (!overlay.IsLoaded)
            return;

        overlay.ApplyOpenState(e.NewValue is true);
    }

    private void ApplyOpenState(bool open, bool instant = false)
    {
        if (open)
            PlayOpenAnimation(instant);
        else
            PlayCloseAnimation(instant);
    }

    private void Backdrop_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ProductPickerViewModel vm && vm.CancelCommand.CanExecute(null))
            vm.CancelCommand.Execute(null);
    }

    private void PinToViewport()
    {
        _pageScrollViewer = FindPageScrollViewer(this);
        if (_pageScrollViewer is null)
            return;

        _savedVScroll = _pageScrollViewer.VerticalScrollBarVisibility;
        _savedHScroll = _pageScrollViewer.HorizontalScrollBarVisibility;
        _pageScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        _pageScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        _pageScrollViewer.ScrollChanged += PageScrollViewer_ScrollChanged;

        UpdateDialogLayout();
    }

    private void UnpinFromViewport()
    {
        if (_pageScrollViewer is not null)
        {
            _pageScrollViewer.ScrollChanged -= PageScrollViewer_ScrollChanged;
            _pageScrollViewer.VerticalScrollBarVisibility = _savedVScroll;
            _pageScrollViewer.HorizontalScrollBarVisibility = _savedHScroll;
            _pageScrollViewer = null;
        }

        ClearValue(WidthProperty);
        ClearValue(HeightProperty);
        RenderTransform = null;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    private void PageScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) =>
        UpdateDialogLayout();

    private static ScrollViewer? FindPageScrollViewer(DependencyObject element)
    {
        var parent = VisualTreeHelper.GetParent(element);
        while (parent is not null)
        {
            if (parent is ScrollViewer scrollViewer)
                return scrollViewer;

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    private void UpdateDialogLayout()
    {
        var viewportWidth = _pageScrollViewer?.ViewportWidth ?? OverlayRoot.ActualWidth;
        var viewportHeight = _pageScrollViewer?.ViewportHeight ?? OverlayRoot.ActualHeight;

        if (viewportWidth <= 0 || viewportHeight <= 0)
            return;

        if (_pageScrollViewer is not null)
        {
            Width = viewportWidth;
            Height = viewportHeight;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
            RenderTransform = new TranslateTransform(0, _pageScrollViewer.VerticalOffset);
        }

        var margin = ScreenMargin * 2;
        DialogCard.Width = Math.Min(MaxDialogWidth, Math.Max(320, viewportWidth - margin));
        DialogCard.MaxHeight = Math.Max(360, viewportHeight - margin);
    }

    private void PlayOpenAnimation(bool instant = false)
    {
        IsHitTestVisible = true;
        OverlayRoot.Visibility = Visibility.Visible;
        OverlayRoot.IsHitTestVisible = true;
        PinToViewport();

        if (instant)
        {
            Backdrop.Opacity = 1;
            return;
        }

        Backdrop.Opacity = 0;
        Backdrop.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });

        var scale = new DoubleAnimation(0.96, 1, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        DialogCard.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
        DialogCard.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
    }

    private void PlayCloseAnimation(bool instant = false)
    {
        if (OverlayRoot.Visibility != Visibility.Visible)
        {
            IsHitTestVisible = false;
            UnpinFromViewport();
            return;
        }

        void FinishClose()
        {
            OverlayRoot.Visibility = Visibility.Collapsed;
            OverlayRoot.IsHitTestVisible = false;
            IsHitTestVisible = false;
            UnpinFromViewport();
        }

        if (instant)
        {
            Backdrop.Opacity = 0;
            FinishClose();
            return;
        }

        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, _) => FinishClose();
        Backdrop.BeginAnimation(OpacityProperty, fade);
    }

    private void ProductCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsInsideButton(e.OriginalSource as DependencyObject))
            return;

        if (sender is FrameworkElement { DataContext: ProductPickerDisplayItem item }
            && DataContext is ProductPickerViewModel vm)
            vm.AddProductCommand.Execute(item);
    }

    private static bool IsInsideButton(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is Button)
                return true;
            source = GetVisualOrLogicalParent(source);
        }

        return false;
    }

    /// <summary>Run and other inlines are not Visuals — VisualTreeHelper.GetParent throws on them.</summary>
    private static DependencyObject? GetVisualOrLogicalParent(DependencyObject current) => current switch
    {
        Visual => VisualTreeHelper.GetParent(current),
        System.Windows.Media.Media3D.Visual3D => VisualTreeHelper.GetParent(current),
        FrameworkContentElement fce => fce.Parent,
        _ => null
    };
}
