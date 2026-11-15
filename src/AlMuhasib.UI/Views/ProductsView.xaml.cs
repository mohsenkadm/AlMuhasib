using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class ProductsView : UserControl
{
    private ProductsViewModel? _viewModel;
    private readonly PriceLabelPrintOverlay _overlay = new();

    public ProductsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachViewModel();
        Loaded += OnLoaded;
        Loaded += (_, _) => AttachViewModel();
        Unloaded += (_, _) => ClosePrintScreen();
    }

    private void AttachViewModel()
    {
        if (DataContext is not ProductsViewModel viewModel || ReferenceEquals(_viewModel, viewModel))
            return;

        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        if (_viewModel.IsBarcodePrintDialogOpen)
            ShowPrintScreen();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProductsViewModel.IsBarcodePrintDialogOpen))
            return;

        if (_viewModel?.IsBarcodePrintDialogOpen == true)
            ShowPrintScreen();
        else
            ClosePrintScreen();
    }

    private void ShowPrintScreen()
    {
        if (_viewModel is null)
            return;

        _overlay.Show(_viewModel.LabelPrint);
    }

    private void ClosePrintScreen() => _overlay.Hide();

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
