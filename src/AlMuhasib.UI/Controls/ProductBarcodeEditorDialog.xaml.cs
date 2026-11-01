using System.Windows;
using System.Windows.Input;
using AlMuhasib.Core.Helpers;

namespace AlMuhasib.UI.Controls;

public partial class ProductBarcodeEditorDialog : Window
{
    public string? ResultBarcode { get; private set; }

    public ProductBarcodeEditorDialog()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            NewBarcodeBox.Focus();
            UpdateCount();
        };
    }

    public static string? Edit(Window? owner, string? currentBarcode)
    {
        var dialog = new ProductBarcodeEditorDialog();
        if (owner is not null)
            dialog.Owner = owner;

        dialog.Load(currentBarcode);
        return dialog.ShowDialog() == true ? dialog.ResultBarcode : null;
    }

    private void Load(string? currentBarcode)
    {
        var tokens = ProductBarcodeHelper.Split(currentBarcode);
        BarcodesBox.Text = tokens.Count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, tokens);
        UpdateCount();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void NewBarcodeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            AddBarcode_Click(sender, e);
        }
    }

    private void AddBarcode_Click(object sender, RoutedEventArgs e)
    {
        var code = ProductBarcodeHelper.UnifySeparators(NewBarcodeBox.Text ?? string.Empty).Trim();
        if (code.Length == 0)
            return;

        // إن وُجد فاصل داخل المدخل، أضف كل توكن
        var toAdd = ProductBarcodeHelper.Split(code);
        if (toAdd.Count == 0)
            return;

        var existing = GetLines();
        foreach (var token in toAdd)
        {
            if (existing.Any(x => string.Equals(x, token, StringComparison.OrdinalIgnoreCase)))
                continue;
            existing.Add(token);
        }

        BarcodesBox.Text = string.Join(Environment.NewLine, existing);
        NewBarcodeBox.Clear();
        NewBarcodeBox.Focus();
        ErrorText.Text = string.Empty;
        UpdateCount();
        BarcodesBox.CaretIndex = BarcodesBox.Text.Length;
        BarcodesBox.ScrollToEnd();
    }

    private void BarcodesBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        ErrorText.Text = string.Empty;
        UpdateCount();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var lines = GetLines();
        var joined = lines.Count == 0 ? null : string.Join(ProductBarcodeHelper.Separator, lines);
        var normalized = ProductBarcodeHelper.Normalize(joined);

        if (normalized is not null && !ProductBarcodeHelper.IsWithinLength(normalized))
        {
            ErrorText.Text =
                $"تجاوز الحد الأقصى ({ProductBarcodeHelper.MaxStoredLength} حرفاً) — احذف بعض الباركودات";
            return;
        }

        ResultBarcode = normalized ?? string.Empty;
        DialogResult = true;
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private List<string> GetLines()
    {
        var text = ProductBarcodeHelper.UnifySeparators(BarcodesBox.Text ?? string.Empty);
        if (string.IsNullOrWhiteSpace(text))
            return [];

        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace(ProductBarcodeHelper.Separator, '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void UpdateCount()
    {
        var n = GetLines().Count;
        CountText.Text = n == 0 ? "لا توجد باركودات" : (n == 1 ? "باركود واحد" : $"{n} باركودات");
    }
}
