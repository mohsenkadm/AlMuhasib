using System.ComponentModel;
using System.Windows;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        // If we're already handling a close decision, let it through
        if (_viewModel.IsExitConfirmed)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        _viewModel.IsExitDialogOpen = true;
    }
}