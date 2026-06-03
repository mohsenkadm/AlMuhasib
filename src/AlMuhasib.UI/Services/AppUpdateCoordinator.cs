using System.Windows;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Updates;
using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.Services;

public static class AppUpdateCoordinator
{
    /// <summary>
    /// Checks for updates on startup. Returns true if the app should exit (update being applied).
    /// </summary>
    public static async Task<bool> TryStartupUpdateAsync(
        IServiceProvider services,
        Action<string>? setStatus = null)
    {
        var configuration = services.GetRequiredService<IConfiguration>();
        var options = configuration.GetSection(AppUpdateOptions.SectionName).Get<AppUpdateOptions>()
                      ?? new AppUpdateOptions();

        if (!options.Enabled || !options.CheckOnStartup)
            return false;

        setStatus?.Invoke("جاري التحقق من التحديثات...");
        var updateService = services.GetRequiredService<IAppUpdateService>();

        var check = await updateService.CheckForUpdateAsync();
        if (!check.IsUpdateAvailable || check.Manifest is null)
            return false;

        if (check.SkippedBecauseOffline || check.SkippedBecauseRecentCheck)
            return false;

        var accepted = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dlg = new UpdatePromptWindow(check);
            return dlg.ShowDialog() == true && dlg.UserAcceptedUpdate;
        });

        if (!accepted && !check.Manifest.IsMandatory)
            return false;

        if (!accepted && check.Manifest.IsMandatory)
        {
            BeautifulMessageDialog.ShowWarning(
                "يجب تثبيت التحديث الإلزامي للمتابعة. سيتم إغلاق البرنامج.",
                "تحديث مطلوب");
            return true;
        }

        return await ApplyUpdateWithUiAsync(updateService, check.Manifest);
    }

    public static async Task<bool> CheckAndApplyManuallyAsync(IServiceProvider services)
    {
        var updateService = services.GetRequiredService<IAppUpdateService>();

        if (!await updateService.IsOnlineAsync())
        {
            BeautifulMessageDialog.ShowWarning(
                "لا يوجد اتصال بالإنترنت أو لا يمكن الوصول لخادم التحديثات.",
                "تحديث النظام");
            return false;
        }

        var check = await updateService.CheckForUpdateAsync(ignoreInterval: true);
        if (!string.IsNullOrWhiteSpace(check.ErrorMessage))
        {
            BeautifulMessageDialog.ShowError($"فشل التحقق من التحديث:\n{check.ErrorMessage}", "تحديث النظام");
            return false;
        }

        if (!check.IsUpdateAvailable || check.Manifest is null)
        {
            var current = updateService.GetCurrentVersion();
            BeautifulMessageDialog.ShowInfo(
                $"أنت تستخدم أحدث إصدار ({current}).",
                "تحديث النظام");
            return false;
        }

        var accepted = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dlg = new UpdatePromptWindow(check);
            return dlg.ShowDialog() == true && dlg.UserAcceptedUpdate;
        });

        if (!accepted)
            return false;

        return await ApplyUpdateWithUiAsync(updateService, check.Manifest);
    }

    private static async Task<bool> ApplyUpdateWithUiAsync(IAppUpdateService updateService, AppUpdateManifest manifest)
    {
        UpdateProgressWindow? progressWindow = null;

        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                progressWindow = new UpdateProgressWindow();
                progressWindow.Show();
            });

            var progress = new Progress<string>(msg =>
            {
                Application.Current.Dispatcher.Invoke(() => progressWindow?.SetStatus(msg));
            });

            await updateService.ApplyUpdateAsync(manifest, progress);

            await Application.Current.Dispatcher.InvokeAsync(() => progressWindow?.Close());

            // Release WPF handles before the updater replaces binaries.
            Application.Current.Shutdown();
            await Task.Delay(800);
            Environment.Exit(0);
            return true;
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                progressWindow?.Close();
                BeautifulMessageDialog.ShowError($"فشل التحديث:\n{ex.Message}", "تحديث النظام");
            });
            return false;
        }
    }
}
