using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AlMuhasib.UI.Behaviors;

/// <summary>
/// Selects all text on focus and clears default zero values when the user starts typing.
/// </summary>
public static class NumericTextBoxBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(NumericTextBoxBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox) return;

        if ((bool)e.NewValue)
        {
            textBox.GotFocus += OnGotFocus;
            textBox.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            textBox.PreviewKeyDown += OnPreviewKeyDown;
        }
        else
        {
            textBox.GotFocus -= OnGotFocus;
            textBox.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            textBox.PreviewKeyDown -= OnPreviewKeyDown;
        }
    }

    private static void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        textBox.Dispatcher.BeginInvoke(() =>
        {
            if (textBox.IsFocused)
                textBox.SelectAll();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox textBox && !textBox.IsKeyboardFocusWithin)
        {
            textBox.Focus();
            e.Handled = true;
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (!IsDigitKey(e.Key)) return;
        if (textBox.SelectionLength > 0) return;
        if (!IsDefaultZeroText(textBox.Text)) return;

        textBox.Clear();
    }

    private static bool IsDigitKey(Key key) =>
        key is >= Key.D0 and <= Key.D9 or >= Key.NumPad0 and <= Key.NumPad9;

    private static bool IsDefaultZeroText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var normalized = text.Trim()
            .Replace(",", "", StringComparison.Ordinal)
            .Replace("٬", "", StringComparison.Ordinal)
            .Replace(" ", "", StringComparison.Ordinal);

        if (normalized is "0" or "0.0" or "0.00") return true;

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.CurrentCulture, out var value)
               && value == 0;
    }
}
