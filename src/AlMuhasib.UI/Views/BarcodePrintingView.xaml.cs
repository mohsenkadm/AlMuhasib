using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Views;

public partial class BarcodePrintingView : UserControl
{
    private BarcodePrintingViewModel? _viewModel;
    private PriceLabelPrintDialog? _dialog;

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

    private static Grid? OverlayHost() =>
        Application.Current?.MainWindow is MainWindow window ? window.FullScreenHost : null;

    private void ShowScreen()
    {
        var host = OverlayHost();
        if (host is null || _viewModel is null)
            return;

        if (_dialog is null)
        {
            host.Children.Clear();
            host.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0x99, 0x00, 0x00, 0x00))
            });
            _dialog = new PriceLabelPrintDialog
            {
                DataContext = _viewModel,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            host.Children.Add(_dialog);
        }
        else
        {
            _dialog.DataContext = _viewModel;
        }

        host.Visibility = Visibility.Visible;
    }

    private void CloseScreen()
    {
        var host = OverlayHost();
        if (host is not null)
            host.Visibility = Visibility.Collapsed;
    }
}
