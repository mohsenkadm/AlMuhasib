using System.Windows;
using System.Windows.Threading;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

public sealed class ToastNotificationService : IToastNotificationService
{
    private const int MaxVisible = 6;
    private const int LoadingMs = 380;
    private const int DefaultDisplayMs = 3400;

    private readonly ISoundService _sound;
    private ToastHost? _host;

    public ToastNotificationService(ISoundService sound) => _sound = sound;

    public void AttachHost(ToastHost host) => _host = host;

    public void ShowSuccess(string message, string? title = null) =>
        _ = ShowFlowAsync(ToastDisplayState.Success, message, title);

    public void ShowError(string message, string? title = null) =>
        _ = ShowFlowAsync(ToastDisplayState.Error, message, title);

    public void ShowWarning(string message, string? title = null) =>
        _ = ShowFlowAsync(ToastDisplayState.Warning, message, title);

    public void ShowInfo(string message, string? title = null) =>
        _ = ShowFlowAsync(ToastDisplayState.Info, message, title);

    public async Task RunAsync(
        string loadingMessage,
        Func<Task> operation,
        string? successMessage = null,
        string? title = null)
    {
        var toast = CreateToast(ToastDisplayState.Loading, loadingMessage, title ?? "جاري التنفيذ");
        Push(toast);

        try
        {
            await operation().ConfigureAwait(false);
            await TransitionAsync(toast, ToastDisplayState.Success, successMessage ?? loadingMessage,
                title ?? "تم بنجاح", SoundEffect.Save).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await TransitionAsync(toast, ToastDisplayState.Error, ex.Message, "خطأ", SoundEffect.Error).ConfigureAwait(false);
        }

        await DismissAsync(toast).ConfigureAwait(false);
    }

    private async Task ShowFlowAsync(ToastDisplayState finalState, string message, string? title)
    {
        var toast = CreateToast(ToastDisplayState.Loading, message, title ?? DefaultTitle(finalState));
        Push(toast);
        await Task.Delay(LoadingMs).ConfigureAwait(false);
        await TransitionAsync(toast, finalState, message, title ?? DefaultTitle(finalState),
            SoundForState(finalState)).ConfigureAwait(false);
        await DismissAsync(toast).ConfigureAwait(false);
    }

    private async Task TransitionAsync(
        ToastNotification toast,
        ToastDisplayState state,
        string message,
        string title,
        SoundEffect? sound = null)
    {
        if (sound is SoundEffect effect)
            _sound.Play(effect);

        await RunOnUiAsync(() =>
        {
            toast.State = state;
            toast.Message = message;
            toast.Title = title;
            toast.DismissProgress = 1;
        }).ConfigureAwait(false);
    }

    private static SoundEffect? SoundForState(ToastDisplayState state) => state switch
    {
        ToastDisplayState.Success => SoundEffect.Success,
        ToastDisplayState.Error => SoundEffect.Error,
        ToastDisplayState.Warning => SoundEffect.Warning,
        ToastDisplayState.Info => SoundEffect.Info,
        _ => null
    };

    private async Task DismissAsync(ToastNotification toast, int displayMs = DefaultDisplayMs)
    {
        const int steps = 36;
        var stepDelay = displayMs / steps;

        for (var i = steps; i >= 0; i--)
        {
            var progress = (double)i / steps;
            await RunOnUiAsync(() => toast.DismissProgress = progress).ConfigureAwait(false);
            await Task.Delay(stepDelay).ConfigureAwait(false);
        }

        await RunOnUiAsync(() => toast.IsExiting = true).ConfigureAwait(false);
        await Task.Delay(380).ConfigureAwait(false);
        await RunOnUiAsync(() => _host?.RemoveToast(toast)).ConfigureAwait(false);
    }

    private ToastNotification CreateToast(ToastDisplayState state, string message, string title) =>
        new()
        {
            State = state,
            Message = message,
            Title = title,
            DismissProgress = 1,
            IsEntering = true
        };

    private void Push(ToastNotification toast)
    {
        RunOnUi(() =>
        {
            if (_host is null)
                return;

            _host.PushToast(toast);
            _ = ClearEnteringFlagAsync(toast);
        });
    }

    private static async Task ClearEnteringFlagAsync(ToastNotification toast)
    {
        await Task.Delay(450).ConfigureAwait(false);
        await RunOnUiAsync(() => toast.IsEntering = false).ConfigureAwait(false);
    }

    private static async Task RunOnUiAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        if (dispatcher.CheckAccess())
        {
            action();
            return;
        }

        await dispatcher.InvokeAsync(action, DispatcherPriority.Normal);
    }

    private static void RunOnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        if (dispatcher.CheckAccess())
            action();
        else
            dispatcher.Invoke(action);
    }

    private static string DefaultTitle(ToastDisplayState state) => state switch
    {
        ToastDisplayState.Success => "تم بنجاح",
        ToastDisplayState.Error => "خطأ",
        ToastDisplayState.Warning => "تنبيه",
        ToastDisplayState.Info => "معلومة",
        _ => "جاري التنفيذ"
    };
}
