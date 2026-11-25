using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Controls;

public partial class ChromeTabStrip : UserControl
{
    private const double DragThreshold = 8;
    private const double DetachSlop = 36;

    private ITabHost? _host;
    private DocumentTab? _dragTab;
    private Point _dragStartScreen;
    private bool _dragging;
    private bool _reorderBusy;
    private Popup? _ghost;
    private ScrollViewer? _scroll;

    public ChromeTabStrip()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(ChromeTabStrip));

    public System.Collections.IEnumerable? ItemsSource
    {
        get => (System.Collections.IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty SelectedTabProperty =
        DependencyProperty.Register(
            nameof(SelectedTab),
            typeof(DocumentTab),
            typeof(ChromeTabStrip),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public DocumentTab? SelectedTab
    {
        get => (DocumentTab?)GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }

    public ITabHost? Host => _host;

    public void AttachHost(ITabHost host) => _host = host;

    public bool IsRightToLeft => FlowDirection == FlowDirection.RightToLeft;

    public bool ContainsScreenPoint(Point screenPixels, double verticalSlop)
    {
        if (ActualWidth <= 1 || ActualHeight <= 1)
            return false;

        var bounds = GetScreenBounds(verticalSlop);
        return bounds.Contains(screenPixels);
    }

    public Rect GetScreenBounds(double verticalSlop)
    {
        var p1 = PointToScreen(new Point(0, 0));
        var p2 = PointToScreen(new Point(ActualWidth, ActualHeight));
        var left = Math.Min(p1.X, p2.X);
        var right = Math.Max(p1.X, p2.X);
        var top = Math.Min(p1.Y, p2.Y) - verticalSlop;
        var bottom = Math.Max(p1.Y, p2.Y) + verticalSlop;
        return new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }

    public IReadOnlyList<TabDragMath.Slot> GetSlots()
    {
        var slots = new List<TabDragMath.Slot>(TabsList.Items.Count);
        for (var i = 0; i < TabsList.Items.Count; i++)
        {
            if (TabsList.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement element)
                continue;
            if (element.ActualWidth <= 1)
                continue;

            var x1 = element.PointToScreen(new Point(0, 0)).X;
            var x2 = element.PointToScreen(new Point(element.ActualWidth, 0)).X;
            slots.Add(new TabDragMath.Slot(i, Math.Min(x1, x2), Math.Max(x1, x2)));
        }

        return slots;
    }

    public int GetExternalInsertIndex(Point screenPixels)
    {
        var slots = GetSlots();
        if (slots.Count == 0)
            return 0;
        return TabDragMath.ResolveExternalInsertIndex(slots, screenPixels.X, IsRightToLeft);
    }

    public void ShowInsertionMarker(double screenX)
    {
        var local = MarkerCanvas.PointFromScreen(new Point(screenX, 0));
        InsertMarker.Visibility = Visibility.Visible;
        Canvas.SetLeft(InsertMarker, local.X - 1);
    }

    public void HideInsertionMarker() => InsertMarker.Visibility = Visibility.Collapsed;

    public void PreserveScroll(Action action)
    {
        _scroll ??= FindScrollViewer(TabsList);
        var offset = _scroll?.HorizontalOffset ?? 0;
        action();
        UpdateLayout();
        _scroll?.ScrollToHorizontalOffset(offset);
    }

    private void TabsList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsChromeButton(e.OriginalSource as DependencyObject))
            return;

        _dragTab = FindTab(e.OriginalSource as DependencyObject);
        _dragStartScreen = PointToScreen(e.GetPosition(this));
        _dragging = false;
    }

    private void TabsList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragTab is null || e.LeftButton != MouseButtonState.Pressed || _host is null)
            return;

        var screen = PointToScreen(e.GetPosition(this));
        if (!_dragging)
        {
            if (Math.Abs(screen.X - _dragStartScreen.X) < DragThreshold
                && Math.Abs(screen.Y - _dragStartScreen.Y) < DragThreshold)
                return;

            BeginDrag();
        }

        UpdateDrag(screen);
        e.Handled = true;
    }

    private void TabsList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging || _dragTab is null || _host is null)
        {
            _dragTab = null;
            return;
        }

        var screen = PointToScreen(e.GetPosition(this));
        var tab = _dragTab;
        var host = _host;
        EndDrag();
        FinishDrag(tab, host, screen);
        e.Handled = true;
    }

    private void TabsList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;

        if (FindTab(e.OriginalSource as DependencyObject) is DocumentTab tab)
        {
            _host?.TryClose(tab);
            e.Handled = true;
        }
    }

    private void TabsList_LostMouseCapture(object sender, MouseEventArgs e)
    {
        if (!_dragging)
            return;

        EndDrag();
    }

    private void TabsList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_dragging || e.Key != Key.Escape)
            return;

        EndDrag();
        e.Handled = true;
    }

    private void BeginDrag()
    {
        if (_dragTab is null)
            return;

        _dragging = true;
        _dragTab.IsBeingDragged = true;
        Mouse.OverrideCursor = Cursors.SizeAll;
        TabsList.CaptureMouse();
        ShowGhost(_dragTab, _dragStartScreen);
    }

    private void UpdateDrag(Point screen)
    {
        if (_dragTab is null || _host is null || _reorderBusy)
            return;

        MoveGhost(screen);
        AutoScroll(screen);

        var target = TabWindowManager.Instance.HostAt(screen, DetachSlop);
        TabWindowManager.Instance.HideInsertionMarkers();

        if (target is null)
        {
        if (_ghost?.Child is Border border)
            border.Opacity = 0.82;
        return;
    }

    if (_ghost?.Child is Border overStrip)
        overStrip.Opacity = 0.96;

        var slots = target.TabStrip.GetSlots();
        if (ReferenceEquals(target, _host))
        {
            var current = _host.Tabs.IndexOf(_dragTab);
            var next = TabDragMath.ResolveMoveTarget(slots, current, screen.X, IsRightToLeft);
            if (current >= 0 && next != current)
            {
                _reorderBusy = true;
                try
                {
                    PreserveScroll(() => _host.Reorder(_dragTab, next));
                }
                finally
                {
                    _reorderBusy = false;
                }

                slots = GetSlots();
            }
        }

        var markerX = MarkerX(slots, screen.X, target.TabStrip.IsRightToLeft);
        target.TabStrip.ShowInsertionMarker(markerX);
    }

    private void FinishDrag(DocumentTab tab, ITabHost host, Point screen)
    {
        var target = TabWindowManager.Instance.HostAt(screen, DetachSlop);
        if (target is null)
        {
            TabWindowManager.Instance.TryDetachToNewWindow(tab, host, screen);
            return;
        }

        if (ReferenceEquals(target, host))
            return;

        var index = target.TabStrip.GetExternalInsertIndex(screen);
        TabWindowManager.Instance.TryMove(tab, host, target, index);
    }

    private void EndDrag()
    {
        _dragging = false;
        if (_dragTab is not null)
            _dragTab.IsBeingDragged = false;
        _dragTab = null;
        if (TabsList.IsMouseCaptured)
            TabsList.ReleaseMouseCapture();
        Mouse.OverrideCursor = null;
        CloseGhost();
        TabWindowManager.Instance.HideInsertionMarkers();
    }

    private void ShowGhost(DocumentTab tab, Point screen)
    {
        CloseGhost();
        var border = new Border
        {
            MinWidth = 140,
            MaxWidth = 220,
            Height = 36,
            Padding = new Thickness(12, 6, 12, 6),
            CornerRadius = new CornerRadius(10, 10, 0, 0),
            BorderThickness = new Thickness(1),
            Opacity = 0.96,
            IsHitTestVisible = false,
            Child = new TextBlock
            {
                Text = tab.Title,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                FlowDirection = FlowDirection.RightToLeft
            }
        };
        border.SetResourceReference(Border.BackgroundProperty, "ChromeTabSelectedBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "ChromeTabSelectedBorderBrush");
        if (border.Child is TextBlock text)
            text.SetResourceReference(TextBlock.ForegroundProperty, "ChromeTabForegroundBrush");

        _ghost = new Popup
        {
            AllowsTransparency = true,
            IsHitTestVisible = false,
            Placement = PlacementMode.Absolute,
            PlacementTarget = this,
            StaysOpen = true,
            PopupAnimation = PopupAnimation.None,
            Child = border
        };
        MoveGhost(screen);
        _ghost.IsOpen = true;
        if (!TabsList.IsMouseCaptured)
            TabsList.CaptureMouse();
    }

    private void MoveGhost(Point screenPixels)
    {
        if (_ghost is null)
            return;

        var dip = ToDip(screenPixels);
        var lifted = TabWindowManager.Instance.HostAt(screenPixels, DetachSlop) is null ? 18 : 10;
        _ghost.HorizontalOffset = dip.X - 48;
        _ghost.VerticalOffset = dip.Y - lifted;
    }

    private void CloseGhost()
    {
        if (_ghost is null)
            return;

        _ghost.IsOpen = false;
        _ghost.Child = null;
        _ghost = null;
    }

    private Point ToDip(Point screenPixels)
    {
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null)
            return screenPixels;
        return source.CompositionTarget.TransformFromDevice.Transform(screenPixels);
    }

    private void AutoScroll(Point screen)
    {
        _scroll ??= FindScrollViewer(TabsList);
        if (_scroll is null)
            return;

        var bounds = GetScreenBounds(0);
        const double edge = 28;
        if (screen.X < bounds.Left + edge)
            _scroll.ScrollToHorizontalOffset(_scroll.HorizontalOffset - 18);
        else if (screen.X > bounds.Right - edge)
            _scroll.ScrollToHorizontalOffset(_scroll.HorizontalOffset + 18);
    }

    private static double MarkerX(IReadOnlyList<TabDragMath.Slot> slots, double pointerX, bool rightToLeft)
    {
        if (slots.Count == 0)
            return pointerX;

        var index = TabDragMath.ResolveExternalInsertIndex(slots, pointerX, rightToLeft);
        if (index <= 0)
            return rightToLeft ? slots.Max(s => s.Right) : slots.Min(s => s.Left);
        if (index >= slots.Count)
            return rightToLeft ? slots.Min(s => s.Left) : slots.Max(s => s.Right);

        var slot = slots.FirstOrDefault(s => s.Index == index);
        if (slot.Right <= slot.Left && slot.Index == 0 && index > 0)
            return pointerX;
        return rightToLeft ? slot.Right : slot.Left;
    }

    private void Pin_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DocumentTab tab)
            _host?.TogglePin(tab);
        e.Handled = true;
    }

    private void Close_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DocumentTab tab)
            _host?.TryClose(tab);
        e.Handled = true;
    }

    private void TabContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu)
            return;

        var tab = (menu.PlacementTarget as FrameworkElement)?.DataContext as DocumentTab;
        var isMain = _host?.IsMainWindow == true;
        if (isMain && Window.GetWindow(this)?.DataContext is MainWindowViewModel main)
            main.PrepareTabContextMenu(tab);

        foreach (var entry in menu.Items)
        {
            if (entry is not FrameworkElement element)
                continue;

            switch (element.Tag as string)
            {
                case "main-only":
                    element.Visibility = isMain ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case "detached-only":
                    element.Visibility = isMain ? Visibility.Collapsed : Visibility.Visible;
                    break;
                case "refresh":
                    element.Visibility = tab?.ViewModel.CanRefresh == true
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    break;
                case "pin" when element is MenuItem pin && tab is not null:
                    pin.Header = tab.IsPinned ? "إلغاء تثبيت التبويب" : "تثبيت التبويب";
                    break;
            }
        }
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e) =>
        WithMenuTab(sender, tab => _host?.TryClose(tab));

    private void CloseOtherTabs_Click(object sender, RoutedEventArgs e) =>
        WithMenuTab(sender, tab => _host?.CloseOthers(tab));

    private void CloseTabsToTheRight_Click(object sender, RoutedEventArgs e) =>
        WithMenuTab(sender, tab => _host?.CloseToTheVisualRight(tab));

    private void CloseAllTabs_Click(object sender, RoutedEventArgs e) => _host?.CloseAll();

    private void PinTab_Click(object sender, RoutedEventArgs e) =>
        WithMenuTab(sender, tab => _host?.TogglePin(tab));

    private void DetachTab_Click(object sender, RoutedEventArgs e)
    {
        WithMenuTab(sender, tab =>
        {
            if (_host is null)
                return;
            var screen = tab == SelectedTab
                ? PointToScreen(new Point(ActualWidth / 2, ActualHeight / 2))
                : PointToScreen(new Point(24, 24));
            TabWindowManager.Instance.TryDetachToNewWindow(tab, _host, screen);
        });
    }

    private void ReturnToMain_Click(object sender, RoutedEventArgs e) =>
        WithMenuTab(sender, tab => TabWindowManager.Instance.TryMoveToMain(tab));

    private async void RefreshTab_Click(object sender, RoutedEventArgs e)
    {
        var tab = MenuTab(sender);
        if (tab is null || _host is null)
            return;

        try
        {
            await _host.RefreshAsync(tab);
        }
        catch (Exception ex)
        {
            BeautifulMessageDialog.ShowError(
                $"تعذّر تحديث الصفحة:\n\n{ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static void WithMenuTab(object sender, Action<DocumentTab> action)
    {
        var tab = MenuTab(sender);
        if (tab is not null)
            action(tab);
    }

    private static DocumentTab? MenuTab(object sender)
    {
        if (sender is not MenuItem item)
            return null;

        var menu = item.Parent as ContextMenu;
        return (menu?.PlacementTarget as FrameworkElement)?.DataContext as DocumentTab;
    }

    private static DocumentTab? FindTab(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is ListBoxItem item && item.DataContext is DocumentTab fromItem)
                return fromItem;
            if (source is FrameworkElement element && element.DataContext is DocumentTab tab)
                return tab;
            source = VisualTreeHelper.GetParent(source);
        }

        return null;
    }

    private static bool IsChromeButton(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is Button)
                return true;
            if (source is ListBoxItem)
                return false;
            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer viewer)
                return viewer;
            var nested = FindScrollViewer(child);
            if (nested is not null)
                return nested;
        }

        return null;
    }
}
