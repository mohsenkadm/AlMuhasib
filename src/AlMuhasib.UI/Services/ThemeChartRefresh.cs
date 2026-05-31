using System.Windows;

namespace AlMuhasib.UI.Services;

/// <summary>
/// Rebuilds chart series when the app theme toggles (LiveCharts paints are created at build time).
/// </summary>
public static class ThemeChartRefresh
{
    private static readonly List<Func<Task>> _reloadActions = [];
    private static bool _subscribed;

    public static void Register(Func<Task> reload)
    {
        if (!_reloadActions.Contains(reload))
            _reloadActions.Add(reload);

        EnsureSubscribed();
    }

    private static void EnsureSubscribed()
    {
        if (_subscribed) return;
        _subscribed = true;
        ThemeService.ThemeChanged += OnThemeChanged;
    }

    private static async void OnThemeChanged(object? sender, EventArgs e)
    {
        var copy = _reloadActions.ToArray();
        foreach (var reload in copy)
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(reload);
            }
            catch
            {
                // ignore per-handler failures during theme switch
            }
        }
    }
}
