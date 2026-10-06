using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class ProductsView : UserControl
{
    public ProductsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        ProductFeatureColumnSync.Attach(
            this,
            ColScientificName,
            ColUsageInstructions,
            ColVehicleType,
            ColChassisNumber,
            ColCarModel,
            ColVehicleColor,
            ColPassengerCount,
            ColPlateNumber,
            ColPlateType);
        CustomFieldColumnSync.Attach(
            this,
            ProductsGrid,
            [ColCf1, ColCf2, ColCf3, ColCf4, ColCf5, ColCf6, ColCf7, ColCf8],
            vm => vm is ProductsViewModel p ? p.GetCustomFieldColumnStates() : null,
            nameof(ProductsViewModel.CustomFieldColumnsVersion));
    }

    private void ProductsGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ProductsViewModel vm && sender is DataGrid grid)
            vm.UpdateBulkDiscountSelectionFromGrid(grid);
    }
}
