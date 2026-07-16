using System.Windows;

namespace AlMuhasib.UI.Helpers;

/// <summary>يضمن تنفيذ تحديثات واجهة الميزات على خيط الـ UI.</summary>
public static class FeatureUiRefresh
{
    public static void Invoke(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }
}
