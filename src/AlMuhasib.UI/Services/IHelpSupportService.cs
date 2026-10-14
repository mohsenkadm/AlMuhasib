using System.Windows;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

public interface IHelpSupportService
{
    HelpVideosManifest GetManifest();
    IReadOnlyList<HelpVideoItemVm> GetAllVideos();
    void OpenWhatsAppSupport();
    void OpenTelegramBot();
    void OpenWhatsAppTelegramSubscribe();
    void OpenAppStore();
    void OpenPlayStore();
    void OpenWhatsAppMobileSubscribe();
    void ShowVideosWindow(Window? owner);
}
