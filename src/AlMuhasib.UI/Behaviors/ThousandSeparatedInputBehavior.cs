using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AlMuhasib.UI.Behaviors;

/// <summary>
/// تنسيق فواصل الآلاف أثناء الكتابة مع الحفاظ على موضع المؤشر (للحذف والإدخال بسلاسة).
/// </summary>
public static class ThousandSeparatedInputBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ThousandSeparatedInputBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty DecimalPlacesProperty =
        DependencyProperty.RegisterAttached(
            "DecimalPlaces",
            typeof(int),
            typeof(ThousandSeparatedInputBehavior),
            new PropertyMetadata(0));

    private static readonly DependencyProperty IsUpdatingProperty =
        DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool),
            typeof(ThousandSeparatedInputBehavior),
            new PropertyMetadata(false));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static int GetDecimalPlaces(DependencyObject obj) => (int)obj.GetValue(DecimalPlacesProperty);
    public static void SetDecimalPlaces(DependencyObject obj, int value) => obj.SetValue(DecimalPlacesProperty, value);

    private static bool GetIsUpdating(DependencyObject obj) => (bool)obj.GetValue(IsUpdatingProperty);
    private static void SetIsUpdating(DependencyObject obj, bool value) => obj.SetValue(IsUpdatingProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox) return;

        if ((bool)e.NewValue)
        {
            textBox.PreviewTextInput += OnPreviewTextInput;
            textBox.PreviewKeyDown += OnPreviewKeyDown;
            DataObject.AddPastingHandler(textBox, OnPaste);
            textBox.TextChanged += OnTextChanged;
        }
        else
        {
            textBox.PreviewTextInput -= OnPreviewTextInput;
            textBox.PreviewKeyDown -= OnPreviewKeyDown;
            DataObject.RemovePastingHandler(textBox, OnPaste);
            textBox.TextChanged -= OnTextChanged;
        }
    }

    private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        var places = GetDecimalPlaces(textBox);
        foreach (var ch in e.Text)
        {
            if (char.IsDigit(ch)) continue;
            if (places > 0 && (ch == '.' || ch == ',') && !textBox.Text.Contains('.') && !HasDecimalComma(textBox.Text))
                continue;
            e.Handled = true;
            return;
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        // اسمح بـ Backspace/Delete/أسهم دون تدخل
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (!e.SourceDataObject.GetDataPresent(DataFormats.Text)) 
        {
            e.CancelCommand();
            return;
        }

        var paste = e.SourceDataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (!IsAllowedPaste(paste, GetDecimalPlaces(textBox)))
            e.CancelCommand();
    }

    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (GetIsUpdating(textBox)) return;

        var places = Math.Max(0, GetDecimalPlaces(textBox));
        var raw = textBox.Text ?? string.Empty;
        if (raw.Length == 0)
            return;

        var caret = textBox.CaretIndex;
        var digitsBeforeCaret = CountSignificantCharsBefore(raw, caret, places);

        if (!TryNormalize(raw, places, out _, out var formatted))
            return;

        if (string.Equals(raw, formatted, StringComparison.Ordinal))
            return;

        SetIsUpdating(textBox, true);
        try
        {
            textBox.Text = formatted;
            textBox.CaretIndex = CaretIndexFromSignificantCount(formatted, digitsBeforeCaret, places);
        }
        finally
        {
            SetIsUpdating(textBox, false);
        }
    }

    private static bool TryNormalize(string raw, int places, out string normalized, out string formatted)
    {
        normalized = string.Empty;
        formatted = raw;

        var cleaned = raw
            .Replace("٬", string.Empty)
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty);

        // فواصل الآلاف فقط — الفاصلة العشرية إن وُجدت كنقطة
        if (places <= 0)
        {
            cleaned = cleaned.Replace(",", string.Empty).Replace(".", string.Empty);
            if (cleaned.Length == 0)
            {
                formatted = string.Empty;
                normalized = string.Empty;
                return true;
            }

            if (!decimal.TryParse(cleaned, NumberStyles.None, CultureInfo.InvariantCulture, out var whole))
                return false;

            normalized = whole.ToString(CultureInfo.InvariantCulture);
            formatted = whole.ToString("N0", CultureInfo.CurrentCulture);
            return true;
        }

        // أزل فواصل الآلاف وأبقِ فاصل عشري واحد
        cleaned = cleaned.Replace(",", string.Empty);
        var dot = cleaned.IndexOf('.');
        if (dot >= 0)
        {
            var intPart = cleaned[..dot];
            var frac = cleaned[(dot + 1)..].Replace(".", string.Empty);
            if (frac.Length > places)
                frac = frac[..places];
            cleaned = string.IsNullOrEmpty(frac) ? intPart : $"{intPart}.{frac}";
        }

        if (cleaned is "." or "")
        {
            formatted = cleaned;
            normalized = cleaned;
            return true;
        }

        if (!decimal.TryParse(cleaned, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            return false;

        normalized = cleaned;
        var format = "N" + places;
        // أثناء الكتابة إن انتهى بنقطة أبقِ النقطة
        if (raw.TrimEnd().EndsWith('.') || raw.TrimEnd().EndsWith(','))
            formatted = value.ToString("N0", CultureInfo.CurrentCulture) + ".";
        else
            formatted = value.ToString(format, CultureInfo.CurrentCulture);
        return true;
    }

    private static int CountSignificantCharsBefore(string text, int caret, int places)
    {
        caret = Math.Clamp(caret, 0, text.Length);
        var count = 0;
        var seenDot = false;
        for (var i = 0; i < caret; i++)
        {
            var ch = text[i];
            if (char.IsDigit(ch))
            {
                count++;
                continue;
            }

            if (places > 0 && !seenDot && (ch == '.' || ch == ','))
            {
                // اعتبر النقطة العشرية فقط إن وُجدت أرقام بعدها أو كانت آخر حرف منطقي
                seenDot = true;
                count++; // نحسب الفاصل العشري كحرف مهم لموضع المؤشر
            }
        }
        return count;
    }

    private static int CaretIndexFromSignificantCount(string formatted, int significantCount, int places)
    {
        if (significantCount <= 0) return 0;
        var seen = 0;
        var seenDot = false;
        for (var i = 0; i < formatted.Length; i++)
        {
            var ch = formatted[i];
            if (char.IsDigit(ch))
            {
                seen++;
                if (seen >= significantCount)
                    return i + 1;
                continue;
            }

            if (places > 0 && !seenDot && ch == '.')
            {
                seenDot = true;
                seen++;
                if (seen >= significantCount)
                    return i + 1;
            }
        }
        return formatted.Length;
    }

    private static bool HasDecimalComma(string text)
    {
        // لا نستخدم الفاصلة كعشري هنا — الآلاف فقط
        return false;
    }

    private static bool IsAllowedPaste(string paste, int places)
    {
        foreach (var ch in paste)
        {
            if (char.IsDigit(ch) || ch is ',' or '٬' or ' ' or '\u00A0') continue;
            if (places > 0 && ch == '.') continue;
            return false;
        }
        return true;
    }
}
