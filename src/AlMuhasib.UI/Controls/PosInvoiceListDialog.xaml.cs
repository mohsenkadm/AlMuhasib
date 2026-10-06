using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Helpers;

namespace AlMuhasib.UI.Controls;

public sealed class PosInvoiceListItem
{
    public int InvoiceId { get; init; }
    public string Number { get; init; } = string.Empty;
    public string Meta { get; init; } = string.Empty;
    public string AmountText { get; init; } = string.Empty;
    public AccountingCurrency Currency { get; init; }
    public decimal Amount { get; init; }
    public bool ShowPrimaryAction { get; init; }
    public bool ShowPrint { get; init; }
    public string PrimaryActionLabel { get; init; } = "استئناف";
}

public partial class PosInvoiceListDialog : Window
{
    public PosInvoiceListItem? SelectedItem { get; private set; }
    public bool PrintRequested { get; private set; }

    public PosInvoiceListDialog()
    {
        InitializeComponent();
    }

    public static PosInvoiceListItem? ShowList(
        string title,
        string subtitle,
        IReadOnlyList<PosInvoiceListItem> items,
        out bool printRequested)
    {
        printRequested = false;
        var dialog = new PosInvoiceListDialog();
        TryAssignOwner(dialog);
        dialog.TitleText.Text = title;
        dialog.SubtitleText.Text = subtitle;
        dialog.InvoiceList.ItemsSource = items;
        dialog.EmptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        dialog.InvoiceList.Visibility = items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        if (dialog.ShowDialog() != true || dialog.SelectedItem is null)
            return null;

        printRequested = dialog.PrintRequested;
        return dialog.SelectedItem;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void PrimaryAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PosInvoiceListItem item })
        {
            SelectedItem = item;
            PrintRequested = false;
            DialogResult = true;
            Close();
        }
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PosInvoiceListItem item })
        {
            SelectedItem = item;
            PrintRequested = true;
            DialogResult = true;
            Close();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
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

    public static string FormatAmount(decimal amount, AccountingCurrency currency) =>
        AccountingCurrencyHelper.Format(amount, currency);
}
