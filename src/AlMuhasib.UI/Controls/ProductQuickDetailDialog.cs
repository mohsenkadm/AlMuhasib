using System.Windows;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.UI.Controls;

public static class ProductQuickDetailDialog
{
    public static void Show(IProductQuickDetailService service, int productId)
    {
        var model = new ProductQuickDetailOverlayViewModel();
        var overlay = new ProductQuickDetailOverlay { DataContext = model };

        async void Load()
        {
            try
            {
                var data = await service.GetDetailAsync(productId);
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (data is null)
                        model.SetError("لم يتم العثور على المنتج");
                    else
                        model.Apply(data);
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() => model.SetError(ex.Message));
            }
        }

        Load();
        overlay.ShowCentered();
    }
}
