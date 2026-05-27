using System.Printing;
using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.Controls;

public partial class PrintSettingsWindow : Window
{
    public PrintSettingsWindow()
    {
        InitializeComponent();
        LoadPrinters();
        LoadCurrentSettings();
    }

    private void LoadPrinters()
    {
        PrinterCombo.Items.Clear();
        try
        {
            using var server = new LocalPrintServer();
            foreach (var queue in server.GetPrintQueues())
            {
                var name = queue.FullName;
                if (!string.IsNullOrWhiteSpace(name))
                    PrinterCombo.Items.Add(name);
            }
        }
        catch
        {
            PrinterCombo.Items.Add("الطابعة الافتراضية للنظام");
        }

        if (PrinterCombo.Items.Count == 0)
            PrinterCombo.Items.Add("الطابعة الافتراضية للنظام");
    }

    private void LoadCurrentSettings()
    {
        PrintPreferences.Load();

        if (!string.IsNullOrWhiteSpace(PrintPreferences.PreferredPrinter))
        {
            foreach (var item in PrinterCombo.Items)
            {
                if (item?.ToString() == PrintPreferences.PreferredPrinter)
                {
                    PrinterCombo.SelectedItem = item;
                    break;
                }
            }
        }

        if (PrinterCombo.SelectedItem is null && PrinterCombo.Items.Count > 0)
            PrinterCombo.SelectedIndex = 0;

        foreach (ComboBoxItem item in PaperSizeCombo.Items)
        {
            if (item.Content?.ToString() == PrintPreferences.PaperSize)
            {
                PaperSizeCombo.SelectedItem = item;
                break;
            }
        }

        PreviewCheckBox.IsChecked = PrintPreferences.ShowPrintPreview;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        PrintPreferences.PreferredPrinter = PrinterCombo.SelectedItem?.ToString();
        PrintPreferences.PaperSize = (PaperSizeCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "A4";
        PrintPreferences.ShowPrintPreview = PreviewCheckBox.IsChecked == true;
        PrintPreferences.Save();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
