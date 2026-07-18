using System.Windows;

namespace AlMuhasib.UI.Helpers;

/// <summary>يضمن تنفيذ تحديثات واجهة الميزات على خيط الـ UI دون حظر المتصل.</summary>
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

        // BeginInvoke يمنع الجمود عند استدعاء التحديث من خلفية بينما خيط الواجهة ينتظر.
        dispatcher.BeginInvoke(action);
    }
}
