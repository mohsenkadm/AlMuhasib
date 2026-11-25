using AlMuhasib.UI.Controls;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Services;

/// <summary>Refreshes a tab in place only when the view model opted in.</summary>
public static class TabRefresh
{
    public static async Task RunAsync(DocumentTab tab)
    {
        if (!tab.ViewModel.CanRefresh)
            return;

        if (tab.ViewModel.HasUnsavedChanges
            && !BeautifulMessageDialog.ShowConfirm(
                "تحديث الصفحة قد يتجاهل التغييرات غير المحفوظة. هل تريد المتابعة؟",
                "تحديث الصفحة"))
            return;

        await tab.ViewModel.RefreshAsync();
    }
}
