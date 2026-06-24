using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AlMuhasib.UI.Behaviors;

/// <summary>
/// Ensures mouse-wheel scrolling works for nested ScrollViewers (page scroll + DataGrid scroll).
/// Scrollbar drag works by default; wheel often fails when children (TabControl, Grid, etc.) do not forward it.
/// </summary>
public static class ScrollViewerMouseWheel
{
    private static readonly MouseWheelEventHandler MouseWheelHandler = OnMouseWheel;

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ScrollViewerMouseWheel),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        if ((bool)e.NewValue)
            element.AddHandler(UIElement.MouseWheelEvent, MouseWheelHandler, true);
        else
            element.RemoveHandler(UIElement.MouseWheelEvent, MouseWheelHandler);
    }

    /// <summary>
    /// Bubble phase (handledEventsToo): try inner ScrollViewers first, then parents.
    /// </summary>
    private static void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        for (var dep = e.OriginalSource as DependencyObject; dep != null; dep = GetParent(dep))
        {
            if (dep is not ScrollViewer scrollViewer)
                continue;

            if (!TryScroll(scrollViewer, e.Delta))
                continue;

            e.Handled = true;
            return;
        }
    }

    /// <summary>
    /// FlowDocument and other content elements are not Visuals — VisualTreeHelper.GetParent throws on them.
    /// </summary>
    private static DependencyObject? GetParent(DependencyObject current) => current switch
    {
        Visual => VisualTreeHelper.GetParent(current),
        System.Windows.Media.Media3D.Visual3D => VisualTreeHelper.GetParent(current),
        FrameworkContentElement fce => fce.Parent,
        _ => null
    };

    private static bool TryScroll(ScrollViewer scrollViewer, int delta)
    {
        if (scrollViewer.ScrollableHeight <= 0)
            return false;

        var offset = scrollViewer.VerticalOffset - delta;
        if (offset < 0)
            offset = 0;
        else if (offset > scrollViewer.ScrollableHeight)
            offset = scrollViewer.ScrollableHeight;

        if (System.Math.Abs(offset - scrollViewer.VerticalOffset) < 0.01)
            return false;

        scrollViewer.ScrollToVerticalOffset(offset);
        return true;
    }
}
