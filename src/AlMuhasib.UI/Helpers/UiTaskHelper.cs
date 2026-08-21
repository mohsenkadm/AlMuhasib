using System.Windows.Threading;

namespace AlMuhasib.UI.Helpers;

/// <summary>
/// Waits for a UI-bound async operation without deadlocking the dispatcher.
/// </summary>
public static class UiTaskHelper
{
    public static T WaitWithMessagePump<T>(Task<T> task)
    {
        if (task.IsCompleted)
            return task.GetAwaiter().GetResult();

        var dispatcher = Dispatcher.CurrentDispatcher;
        var frame = new DispatcherFrame();
        task.ContinueWith(
            _ => dispatcher.BeginInvoke(() => frame.Continue = false),
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        if (task.IsCompleted)
            frame.Continue = false;
        else
            Dispatcher.PushFrame(frame);

        return task.GetAwaiter().GetResult();
    }
}
