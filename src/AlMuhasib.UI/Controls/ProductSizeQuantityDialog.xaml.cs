using System.Windows;
using System.Windows.Input;
using AlMuhasib.Core.Entities;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Controls;

public partial class ProductSizeQuantityDialog : Window
{
    private readonly ProductSizeQuantityDialogViewModel _vm = new();

    public SizeQuantitySelection? Selection { get; private set; }

    public ProductSizeQuantityDialog()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    public static SizeQuantitySelection? ShowForProduct(
        Window? owner,
        Product product,
        IReadOnlyList<ProductSize> sizes,
        IReadOnlyDictionary<int, decimal>? stockBySizeId,
        bool showStock,
        string modeHint,
        decimal unitPrice = 0,
        int? pricingTypeId = null,
        string? pricingTypeName = null,
        IReadOnlyDictionary<int, decimal>? seedQuantities = null)
    {
        if (sizes.Count == 0)
            return null;

        var dialog = new ProductSizeQuantityDialog();
        if (owner is not null)
            dialog.Owner = owner;

        dialog._vm.Load(
            product,
            sizes,
            stockBySizeId,
            showStock,
            modeHint,
            unitPrice,
            pricingTypeId,
            pricingTypeName,
            seedQuantities);

        return dialog.ShowDialog() == true ? dialog.Selection : null;
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var selection = _vm.TryBuildSelection();
        if (selection is null)
            return;

        Selection = selection;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
