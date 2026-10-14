using System.Windows;
using System.Windows.Input;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Windows;

public partial class PosFullscreenWindow : Window
{
    private readonly ISessionActivityService _sessionActivity;
    private readonly IToastNotificationService _toast;

    public PosFullscreenWindow(
        ISessionActivityService sessionActivity,
        IToastNotificationService toast)
    {
        _sessionActivity = sessionActivity;
        _toast = toast;
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
        PreviewMouseDown += (_, _) => TouchActivity();
        PreviewMouseMove += (_, _) => TouchActivity();
        PreviewMouseWheel += (_, _) => TouchActivity();
        Closed += (_, _) => _toast.DetachOverlayHost();
    }

    public void Initialize(PosQuickSaleViewModel viewModel)
    {
        DataContext = viewModel;
        PosView.DataContext = viewModel;
        Owner = Application.Current.MainWindow;
        WindowState = WindowState.Maximized;
        _toast.AttachOverlayHost(PosToastHost);
        TouchActivity();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        TouchActivity();
        if (e.Key == Key.Escape && DataContext is PosQuickSaleViewModel vm)
        {
            vm.ToggleFullscreenCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void TouchActivity() => _sessionActivity.RecordActivity();
}
