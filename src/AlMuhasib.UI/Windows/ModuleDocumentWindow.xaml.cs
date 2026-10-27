using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.Windows;

/// <summary>
/// نافذة مستند منفصلة بشاشة كاملة مع شريط ويندوز الافتراضي (تصغير/تكبير/إغلاق).
/// </summary>
public partial class ModuleDocumentWindow : Window
{
    private readonly ISessionActivityService _sessionActivity;
    private readonly IToastNotificationService _toast;
    private bool _forceClose;
    private bool _closePromptHandled;

    public ModuleDocumentWindow(
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
        Closing += OnClosing;
        Closed += OnClosed;
    }

    public Guid DocumentId { get; } = Guid.NewGuid();
    public Type ViewModelType { get; private set; } = typeof(object);
    public ViewModelBase? ViewModel { get; private set; }
    public IServiceScope? Scope { get; private set; }
    public string? PermissionScreenName { get; private set; }

    public void Initialize(
        Type viewModelType,
        ViewModelBase viewModel,
        IServiceScope scope,
        string title,
        string? permissionScreenName)
    {
        ViewModelType = viewModelType;
        ViewModel = viewModel;
        Scope = scope;
        PermissionScreenName = permissionScreenName;
        Title = string.IsNullOrWhiteSpace(title) ? AppInfo.AppNameAr : $"{title} — {AppInfo.AppNameAr}";
        DataContext = viewModel;
        DocumentHost.Content = viewModel;
        Owner = Application.Current?.MainWindow;
        WindowState = WindowState.Maximized;
        _toast.AttachOverlayHost(DocumentToastHost);
        TouchActivity();
    }

    /// <summary>يغلق النافذة بعد اجتياز فالديشن التغييرات غير المحفوظة (أو تجاهلها بعد موافقة المستخدم).</summary>
    public void RequestClose()
    {
        Close();
    }

    /// <summary>إغلاق قسري بدون فالديشن (عند إغلاق التطبيق).</summary>
    public void ForceClose()
    {
        _forceClose = true;
        Close();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_forceClose || _closePromptHandled)
            return;

        if (ViewModel is null || !ViewModel.HasUnsavedChanges)
            return;

        if (ViewModel.SupportsSaveBeforeLeave)
        {
            var choice = BeautifulMessageDialog.ShowSaveDiscardCancel(
                "لديك معلومات مدخلة وغير محفوظة.\nهل تريد حفظها قبل إغلاق النافذة، أم المتابعة دون حفظ؟");

            if (choice == UnsavedChangesDialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (choice == UnsavedChangesDialogResult.Save)
            {
                var saved = UiTaskHelper.WaitWithMessagePump(ViewModel.SavePendingChangesAsync());
                if (!saved)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }
        else if (!BeautifulMessageDialog.ShowConfirm(
                     "لديك معلومات مدخلة وغير محفوظة.\nهل تريد متابعة الإغلاق بدون حفظ؟",
                     "إغلاق النافذة"))
        {
            e.Cancel = true;
            return;
        }

        _closePromptHandled = true;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _toast.DetachOverlayHost();
        try
        {
            Scope?.Dispose();
        }
        catch
        {
            // ignore dispose races on shutdown
        }

        Scope = null;
        ViewModel = null;
        DocumentHost.Content = null;
        DataContext = null;
    }

    private void TouchActivity() => _sessionActivity.RecordActivity();
}
