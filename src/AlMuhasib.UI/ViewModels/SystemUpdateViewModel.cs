using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.Core.Models.Updates;
using AlMuhasib.UI.Helpers;
using AlMuhasib.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.ViewModels;

public partial class SystemUpdateViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _currentVersion = AppInfo.Version;

    [ObservableProperty]
    private bool _isChecking;

    [ObservableProperty]
    private bool _updatesEnabled;

    [ObservableProperty]
    private bool _autoCheckOnStartup;

    public string VersionLabel => AppInfo.VersionLabel;

    public SystemUpdateViewModel(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        PageTitle = "تحديث النظام";

        var options = configuration.GetSection(AppUpdateOptions.SectionName).Get<AppUpdateOptions>()
                      ?? new AppUpdateOptions();
        UpdatesEnabled = options.Enabled;
        AutoCheckOnStartup = options.Enabled && options.CheckOnStartup;
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        if (IsChecking)
            return;

        try
        {
            IsChecking = true;
            await AppUpdateCoordinator.CheckAndApplyManuallyAsync(_serviceProvider);
        }
        finally
        {
            IsChecking = false;
        }
    }
}
