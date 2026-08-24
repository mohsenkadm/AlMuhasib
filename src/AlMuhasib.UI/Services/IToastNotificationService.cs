using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

public interface IToastNotificationService
{
    void AttachHost(Controls.ToastHost host);

    /// <summary>When set, toasts are shown on this host instead of the main window host.</summary>
    void AttachOverlayHost(Controls.ToastHost host);

    void DetachOverlayHost();

    void ShowSuccess(string message, string? title = null);
    void ShowError(string message, string? title = null);
    void ShowWarning(string message, string? title = null);
    void ShowInfo(string message, string? title = null);

    Task RunAsync(
        string loadingMessage,
        Func<Task> operation,
        string? successMessage = null,
        string? title = null);
}
