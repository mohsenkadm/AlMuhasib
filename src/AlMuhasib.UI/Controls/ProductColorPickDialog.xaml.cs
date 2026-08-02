using System.Windows;
using System.Windows.Input;
using AlMuhasib.Core.Entities;

namespace AlMuhasib.UI.Controls;

public partial class ProductColorPickDialog : Window
{
    public ProductColor? SelectedColor { get; private set; }
    public bool Skipped { get; private set; }

    public ProductColorPickDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// يعيد اللون المختار، أو null عند التخطي، أو يرمي إلغاء عبر DialogResult=false.
    /// </summary>
    public static (ProductColor? Color, bool Cancelled) ShowForProduct(
        Window? owner,
        Product product,
        IReadOnlyList<ProductColor> colors)
    {
        if (colors.Count == 0)
            return (null, false);

        if (colors.Count == 1)
            return (colors[0], false);

        var dialog = new ProductColorPickDialog();
        if (owner is not null)
            dialog.Owner = owner;

        dialog.ProductNameText.Text = product.Name;
        dialog.ColorsList.ItemsSource = colors;
        dialog.ColorsList.SelectedIndex = 0;

        var ok = dialog.ShowDialog() == true;
        if (!ok)
            return (null, true);

        if (dialog.Skipped)
            return (null, false);

        return (dialog.SelectedColor, false);
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        SelectedColor = ColorsList.SelectedItem as ProductColor;
        if (SelectedColor is null)
            return;
        Skipped = false;
        DialogResult = true;
        Close();
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        SelectedColor = null;
        Skipped = true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
