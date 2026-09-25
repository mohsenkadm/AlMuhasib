using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace AlMuhasib.UI.Behaviors;

/// <summary>
/// Opens a page-level ContextMenu on right-click anywhere inside the view
/// (including DataGrid cells), using the view's DataContext for commands.
/// </summary>
public static class PageContextMenuBehavior
{
    public static readonly DependencyProperty MenuProperty =
        DependencyProperty.RegisterAttached(
            "Menu",
            typeof(ContextMenu),
            typeof(PageContextMenuBehavior),
            new PropertyMetadata(null, OnMenuChanged));

    public static void SetMenu(DependencyObject element, ContextMenu? value) =>
        element.SetValue(MenuProperty, value);

    public static ContextMenu? GetMenu(DependencyObject element) =>
        (ContextMenu?)element.GetValue(MenuProperty);

    private static void OnMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement root)
            return;

        root.PreviewMouseRightButtonUp -= OnPreviewMouseRightButtonUp;
        if (e.NewValue is ContextMenu)
            root.PreviewMouseRightButtonUp += OnPreviewMouseRightButtonUp;
    }

    private static void OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement root)
            return;

        var menu = GetMenu(root);
        if (menu is null)
            return;

        // Text boxes / editable fields keep their own context menus.
        if (e.OriginalSource is DependencyObject source
            && (FindAncestor<TextBoxBase>(source) is not null
                || FindAncestor<PasswordBox>(source) is not null))
            return;

        menu.DataContext = root.DataContext;
        menu.PlacementTarget = root;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
        e.Handled = true;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
                return match;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current)
                      ?? (current as FrameworkElement)?.Parent as DependencyObject;
        }

        return null;
    }
}
