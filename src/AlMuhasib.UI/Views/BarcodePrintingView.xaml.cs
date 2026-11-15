using System.ComponentModel;
using System.Windows.Controls;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class BarcodePrintingView : UserControl
{
    private BarcodePrintingViewModel? _viewModel;
    private readonly PriceLabelPrintOverlay _overlay = new();

    public BarcodePrintingView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => AttachViewModel();
        Loaded += (_, _) => AttachViewModel();
        Unloaded += (_, _) => CloseScreen();
    }

    private void AttachViewModel()
    {
        if (DataContext is not BarcodePrintingViewModel viewModel || ReferenceEquals(_viewModel, viewModel))
            return;

        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        if (_viewModel.IsPrintDialogOpen)
            ShowScreen();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BarcodePrintingViewModel.IsPrintDialogOpen))
            return;

        if (_viewModel?.IsPrintDialogOpen == true)
            ShowScreen();
        else
            CloseScreen();
    }

    private void ShowScreen()
    {
        if (_viewModel is null)
            return;

        _overlay.Show(_viewModel.LabelPrint);
    }

    private void CloseScreen() => _overlay.Hide();
}
