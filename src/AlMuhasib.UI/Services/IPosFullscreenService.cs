using AlMuhasib.UI.ViewModels;

namespace AlMuhasib.UI.Services;

public interface IPosFullscreenService
{
    bool IsOpen { get; }

    void Open(PosQuickSaleViewModel viewModel);

    void Close();
}
