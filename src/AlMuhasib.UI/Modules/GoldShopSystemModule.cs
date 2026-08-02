using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Services;
using AlMuhasib.UI.ViewModels.Gold;

namespace AlMuhasib.UI.Modules;

public sealed class GoldShopSystemModule : ISystemModule
{
    public ApplicationSystemType SystemType => ApplicationSystemType.GoldShop;
    public string DisplayName => "نظام الذهب";
    public Type DashboardViewModelType => typeof(GoldDashboardViewModel);
    public Type? SetupWizardViewModelType => null;

    public IReadOnlyList<(string Name, string Label)> PermissionScreens =>
        GoldShopPermissionRegistry.Screens;

    public IReadOnlyList<NavigationMenuItem> BuildMenuItems() =>
        GoldShopMenuBuilder.Build();

    public string GetScreenName(Type viewModelType) =>
        GoldShopPermissionRegistry.GetScreenName(viewModelType);

    public string GetLabel(string screenName) =>
        GoldShopPermissionRegistry.GetLabel(screenName);

    public Type? GetDefaultViewModelType(string screenName) =>
        GoldShopPermissionRegistry.GetDefaultViewModelType(screenName);
}
