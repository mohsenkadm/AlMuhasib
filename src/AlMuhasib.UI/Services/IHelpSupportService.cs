using System.Windows;
using AlMuhasib.UI.Models;

namespace AlMuhasib.UI.Services;

public interface IHelpSupportService
{
    HelpVideosManifest GetManifest();
    IReadOnlyList<HelpVideoItemVm> GetAllVideos();
    void OpenWhatsAppSupport();
    void ShowVideosWindow(Window? owner);
}
