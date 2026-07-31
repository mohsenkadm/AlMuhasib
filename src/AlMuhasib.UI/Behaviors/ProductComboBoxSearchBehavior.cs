using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AlMuhasib.Core.Entities;
using AlMuhasib.UI.Helpers;

namespace AlMuhasib.UI.Behaviors;

/// <summary>
/// يصفّي عناصر ComboBox المنتج حسب الاسم / الباركود / الاسم العلمي أثناء الكتابة،
/// دون تغيير DisplayMemberPath عن الاسم التجاري.
/// </summary>
public static class ProductComboBoxSearchBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ProductComboBoxSearchBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static readonly DependencyProperty ViewProperty =
        DependencyProperty.RegisterAttached(
            "View",
            typeof(ICollectionView),
            typeof(ProductComboBoxSearchBehavior));

    private static readonly DependencyProperty SourceProperty =
        DependencyProperty.RegisterAttached(
            "Source",
            typeof(IEnumerable),
            typeof(ProductComboBoxSearchBehavior));

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ComboBox combo)
            return;

        var descriptor = DependencyPropertyDescriptor.FromProperty(
            ItemsControl.ItemsSourceProperty, typeof(ComboBox));

        if ((bool)e.NewValue)
        {
            combo.IsTextSearchEnabled = false;
            combo.Loaded += OnComboLoaded;
            combo.AddHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(OnTextChanged), true);
            descriptor?.AddValueChanged(combo, OnItemsSourceChanged);
            CaptureAndWrap(combo);
            ApplyFilter(combo);
        }
        else
        {
            combo.Loaded -= OnComboLoaded;
            combo.RemoveHandler(TextBox.TextChangedEvent, new TextChangedEventHandler(OnTextChanged));
            descriptor?.RemoveValueChanged(combo, OnItemsSourceChanged);
            RestoreSource(combo);
        }
    }

    private static void OnComboLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox combo)
        {
            CaptureAndWrap(combo);
            ApplyFilter(combo);
        }
    }

    private static void OnItemsSourceChanged(object? sender, EventArgs e)
    {
        if (sender is not ComboBox combo)
            return;

        // تجاهل التغيير الذي نُحدثه نحن عند لفّ المصدر بـ ListCollectionView
        if (combo.ItemsSource is ICollectionView && combo.GetValue(ViewProperty) is ICollectionView)
            return;

        CaptureAndWrap(combo);
        ApplyFilter(combo);
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is ComboBox combo)
            ApplyFilter(combo);
    }

    private static void CaptureAndWrap(ComboBox combo)
    {
        if (combo.ItemsSource is null)
            return;

        if (combo.ItemsSource is ICollectionView existingView
            && ReferenceEquals(combo.GetValue(ViewProperty), existingView))
            return;

        var raw = combo.GetValue(SourceProperty) as IEnumerable ?? combo.ItemsSource;
        if (combo.ItemsSource is not ICollectionView)
            combo.SetValue(SourceProperty, combo.ItemsSource);

        raw = combo.GetValue(SourceProperty) as IEnumerable ?? raw;
        var list = raw is IList ilist ? ilist : raw.Cast<object>().ToList();
        var view = new ListCollectionView(list);
        combo.SetValue(ViewProperty, view);
        combo.ItemsSource = view;
    }

    private static void RestoreSource(ComboBox combo)
    {
        if (combo.GetValue(SourceProperty) is IEnumerable original)
            combo.ItemsSource = original;
        combo.ClearValue(ViewProperty);
        combo.ClearValue(SourceProperty);
    }

    private static void ApplyFilter(ComboBox combo)
    {
        CaptureAndWrap(combo);
        if (combo.GetValue(ViewProperty) is not ICollectionView view)
            return;

        var term = combo.Text?.Trim() ?? string.Empty;
        view.Filter = item =>
        {
            if (item is not Product product)
                return true;

            if (string.IsNullOrEmpty(term))
                return true;

            if (combo.SelectedItem is Product selected
                && string.Equals(selected.Name, term, StringComparison.OrdinalIgnoreCase))
                return true;

            return ProductSearchHelper.Matches(product, term);
        };
        view.Refresh();
    }
}
