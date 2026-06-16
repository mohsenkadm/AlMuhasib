using System.Diagnostics;
using System.Windows;

namespace AlMuhasib.UI.Helpers;

public static class ApplicationRestartHelper
{
    public static void Restart()
    {
        var exePath = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName;

        if (!string.IsNullOrWhiteSpace(exePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory
            });
        }

        if (Application.Current is not null)
            Application.Current.Shutdown();
    }
}
