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
        PreviewKeyDown += (_, _) => TouchActivity();
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
        _toast.AttachOverlayHost(PosToastHost);
        TouchActivity();
    }

    private void TouchActivity() => _sessionActivity.RecordActivity();
}
