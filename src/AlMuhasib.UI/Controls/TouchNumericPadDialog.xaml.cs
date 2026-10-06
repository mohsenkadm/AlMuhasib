using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace AlMuhasib.UI.Controls;

public partial class TouchNumericPadDialog : Window
{
    private string _buffer = "0";
    private bool _allowDecimal;
    private bool _allowZero;

    public decimal Value { get; private set; }

    public TouchNumericPadDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Shows a touch numeric pad. Returns null on cancel.
    /// </summary>
    public static decimal? Prompt(
        string title,
        string? subtitle = null,
        decimal initialValue = 0m,
        bool allowDecimal = true,
        bool allowZero = true,
        decimal? minValue = null)
    {
        var dialog = new TouchNumericPadDialog
        {
            _allowDecimal = allowDecimal,
            _allowZero = allowZero
        };
        TryAssignOwner(dialog);
        dialog.TitleText.Text = title;
        dialog.SubtitleText.Text = subtitle ?? string.Empty;
        dialog.SubtitleText.Visibility = string.IsNullOrWhiteSpace(subtitle)
            ? Visibility.Collapsed
            : Visibility.Visible;
        dialog.DecimalButton.Visibility = allowDecimal ? Visibility.Visible : Visibility.Hidden;
        dialog.DecimalButton.IsEnabled = allowDecimal;

        var start = initialValue < 0 ? 0m : initialValue;
        dialog._buffer = allowDecimal
            ? start.ToString("0.##", CultureInfo.InvariantCulture)
            : ((int)Math.Round(start, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(dialog._buffer))
            dialog._buffer = "0";
        dialog.RefreshDisplay();

        if (dialog.ShowDialog() != true)
            return null;

        if (minValue.HasValue && dialog.Value < minValue.Value)
            return null;

        if (!allowZero && dialog.Value <= 0)
            return null;

        return dialog.Value;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Digit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string dig })
            return;

        if (_buffer is "0" or "-0")
            _buffer = dig;
        else if (_buffer.Length < 12)
            _buffer += dig;

        RefreshDisplay();
    }

    private void Decimal_Click(object sender, RoutedEventArgs e)
    {
        if (!_allowDecimal || _buffer.Contains('.'))
            return;
        _buffer += ".";
        RefreshDisplay();
    }

    private void Backspace_Click(object sender, RoutedEventArgs e)
    {
        if (_buffer.Length <= 1)
            _buffer = "0";
        else
            _buffer = _buffer[..^1];
        if (_buffer is "." or "-")
            _buffer = "0";
        RefreshDisplay();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _buffer = "0";
        RefreshDisplay();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (!TryAccept())
            return;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool TryAccept()
    {
        var text = _buffer.TrimEnd('.');
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            return false;

        if (!_allowZero && value <= 0)
            return false;

        if (!_allowDecimal)
            value = Math.Round(value, 0, MidpointRounding.AwayFromZero);

        Value = value;
        return true;
    }

    private void RefreshDisplay()
    {
        DisplayText.Text = _buffer;
    }

    private static void TryAssignOwner(Window dialog)
    {
        if (Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) is { } active)
        {
            dialog.Owner = active;
            return;
        }

        if (Application.Current?.MainWindow is { IsLoaded: true } main)
            dialog.Owner = main;
    }
}
