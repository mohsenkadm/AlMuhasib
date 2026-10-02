using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.UI.Controls;

public static class WarehouseTransferDetailDialog
{
    public static void Show(WarehouseTransferDetailResult detail)
    {
        var model = WarehouseTransferDetailMapper.FromDetail(detail);
        var overlay = new WarehouseTransferDetailOverlay { DataContext = model };
        overlay.ShowCentered();
    }
}
