using AlMuhasib.Core.Enums;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.ViewModels;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Modules;

public interface ISystemModule
{
    ApplicationSystemType SystemType { get; }
    string DisplayName { get; }
    IReadOnlyList<(string Name, string Label)> PermissionScreens { get; }
    IReadOnlyList<NavigationMenuItem> BuildMenuItems();
    Type DashboardViewModelType { get; }
    Type? SetupWizardViewModelType { get; }
    string GetScreenName(Type viewModelType);
    string GetLabel(string screenName);
    Type? GetDefaultViewModelType(string screenName);
}
