using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Modules;

public sealed class AccountingSystemModule : ISystemModule
{
    public ApplicationSystemType SystemType => ApplicationSystemType.Accounting;
    public string DisplayName => "النظام المحاسبي";
    public Type DashboardViewModelType => typeof(DashboardViewModel);
    public Type? SetupWizardViewModelType => typeof(SetupWizardViewModel);

    public IReadOnlyList<(string Name, string Label)> PermissionScreens =>
        ScreenPermissionRegistry.AccountingScreens;

    public IReadOnlyList<NavigationMenuItem> BuildMenuItems() =>
        AccountingMenuBuilder.Build();

    public string GetScreenName(Type viewModelType) =>
        ScreenPermissionRegistry.GetAccountingScreenName(viewModelType);

    public string GetLabel(string screenName) =>
        ScreenPermissionRegistry.GetAccountingLabel(screenName);

    public Type? GetDefaultViewModelType(string screenName) =>
        ScreenPermissionRegistry.GetAccountingDefaultViewModelType(screenName);
}
