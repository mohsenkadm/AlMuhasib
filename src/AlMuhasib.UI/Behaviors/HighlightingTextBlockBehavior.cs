using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Behaviors;

/// <summary>يربط TextBlock بتمييز أصفر حسب عبارة البحث.</summary>
public static class HighlightingTextBlockBehavior
{
    public static readonly DependencyProperty HighlightTextProperty =
        DependencyProperty.RegisterAttached(
            "HighlightText",
            typeof(string),
            typeof(HighlightingTextBlockBehavior),
            new PropertyMetadata(null, OnChanged));

    public static readonly DependencyProperty SearchTermProperty =
        DependencyProperty.RegisterAttached(
            "SearchTerm",
            typeof(string),
            typeof(HighlightingTextBlockBehavior),
            new PropertyMetadata(null, OnChanged));

    public static string? GetHighlightText(DependencyObject obj) => (string?)obj.GetValue(HighlightTextProperty);
    public static void SetHighlightText(DependencyObject obj, string? value) => obj.SetValue(HighlightTextProperty, value);

    public static string? GetSearchTerm(DependencyObject obj) => (string?)obj.GetValue(SearchTermProperty);
    public static void SetSearchTerm(DependencyObject obj, string? value) => obj.SetValue(SearchTermProperty, value);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock block)
            HighlightTextHelper.ApplyHighlight(block, GetHighlightText(block), GetSearchTerm(block));
    }
}
