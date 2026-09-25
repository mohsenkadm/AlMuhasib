using System.Windows;
using System.Windows.Threading;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Services;

/// <summary>
/// Rebuilds chart series when the app theme toggles (LiveCharts paints are created at build time).
/// Uses weak refs + debounce + open-tab filtering so theme toggles stay responsive.
/// </summary>
public static class ThemeChartRefresh
{
    private static readonly object Gate = new();
    private static readonly List<WeakReference<Func<Task>>> ReloadActions = [];
    private static bool _subscribed;
    private static CancellationTokenSource? _debounceCts;
    private static int _generation;

    public static void Register(Func<Task> reload)
    {
        ArgumentNullException.ThrowIfNull(reload);

        lock (Gate)
        {
            PruneDead_NoLock();
            foreach (var weak in ReloadActions)
            {
                if (weak.TryGetTarget(out var existing) && SameHandler(existing, reload))
                    return;
            }

            ReloadActions.Add(new WeakReference<Func<Task>>(reload));
            EnsureSubscribed_NoLock();
        }
    }

    public static void Unregister(Func<Task> reload)
    {
        if (reload is null) return;

        lock (Gate)
        {
            ReloadActions.RemoveAll(w =>
                !w.TryGetTarget(out var existing) || SameHandler(existing, reload));
        }
    }

    private static bool SameHandler(Func<Task> a, Func<Task> b)
        => ReferenceEquals(a.Target, b.Target) && a.Method == b.Method;

    private static void EnsureSubscribed_NoLock()
    {
        if (_subscribed) return;
        _subscribed = true;
        ThemeService.ThemeChanged += OnThemeChanged;
    }

    private static void PruneDead_NoLock()
        => ReloadActions.RemoveAll(w => !w.TryGetTarget(out _));

    private static async void OnThemeChanged(object? sender, EventArgs e)
    {
        var cts = new CancellationTokenSource();
        lock (Gate)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = cts;
            _generation++;
        }

        var generation = _generation;
        var token = cts.Token;

        try
        {
            // Let MaterialDesign/palette brushes settle first.
            await Task.Delay(120, token).ConfigureAwait(true);
            if (token.IsCancellationRequested || generation != _generation)
                return;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher is null) return;

            Func<Task>[] copy;
            lock (Gate)
            {
                PruneDead_NoLock();
                copy = ReloadActions
                    .Select(w => w.TryGetTarget(out var t) ? t : null)
                    .Where(t => t is not null && IsHandlerRelevant(t!))
                    .Cast<Func<Task>>()
                    .ToArray();
            }

            foreach (var reload in copy)
            {
                if (token.IsCancellationRequested || generation != _generation)
                    return;

                try
                {
                    // Run one handler at a time so the UI can paint between them.
                    var operation = dispatcher.InvokeAsync(
                        () => reload(),
                        DispatcherPriority.Background);
                    await operation.Task.Unwrap().ConfigureAwait(true);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
                catch
                {
                    // ignore per-handler failures during theme switch
                }

                // Yield so input/theme rendering stays responsive.
                await dispatcher.InvokeAsync(static () => { }, DispatcherPriority.Background);
            }
        }
        catch (TaskCanceledException)
        {
            // newer toggle won the debounce
        }
    }

    /// <summary>
    /// Only refresh charts for the active view / open tabs — never every report VM ever constructed.
    /// </summary>
    private static bool IsHandlerRelevant(Func<Task> reload)
    {
        var target = reload.Target;
        if (target is null)
            return true; // static handlers

        if (Application.Current?.MainWindow?.DataContext is not MainWindowViewModel main)
            return true; // fail open if shell not ready

        if (ReferenceEquals(main.CurrentViewModel, target))
            return true;

        foreach (var tab in main.OpenTabs)
        {
            if (ReferenceEquals(tab.ViewModel, target))
                return true;
        }

        return false;
    }
}
