using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace AlMuhasib.UI.Controls;

public partial class QuickQuantityDialog : Window
{
    public decimal Quantity { get; private set; } = 1m;

    public QuickQuantityDialog()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            QuantityBox.Text = "1";
            QuantityBox.Focus();
            QuantityBox.SelectAll();
        };
    }

    /// <summary>
    /// يطلب الكمية (افتراضي 1 ومحدّد). يعيد null عند الإلغاء.
    /// </summary>
    public static decimal? Prompt(string? productName, decimal defaultQuantity = 1m)
    {
        var dialog = new QuickQuantityDialog();
        TryAssignOwner(dialog);
        dialog.ProductNameText.Text = string.IsNullOrWhiteSpace(productName)
            ? "أدخل الكمية ثم اضغط Enter"
            : productName.Trim();
        dialog.Quantity = defaultQuantity > 0 ? defaultQuantity : 1m;
        dialog.QuantityBox.Text = dialog.Quantity.ToString("0.##", CultureInfo.CurrentCulture);

        return dialog.ShowDialog() == true ? dialog.Quantity : null;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void QuantityBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (TryAccept())
            {
                e.Handled = true;
                DialogResult = true;
                Close();
            }
            else
            {
                e.Handled = true;
                QuantityBox.SelectAll();
            }
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (TryAccept())
        {
            DialogResult = true;
            Close();
        }
        else
        {
            QuantityBox.Focus();
            QuantityBox.SelectAll();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool TryAccept()
    {
        var text = (QuantityBox.Text ?? string.Empty).Trim();
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var qty)
            && !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out qty))
            return false;

        if (qty <= 0)
            return false;

        Quantity = qty;
        return true;
    }

    private static void TryAssignOwner(Window dialog)
    {
        if (Application.Current?.MainWindow is { IsLoaded: true } main)
            dialog.Owner = main;
    }
}
