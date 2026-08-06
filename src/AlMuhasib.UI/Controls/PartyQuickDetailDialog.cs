using System.Windows;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.UI.Controls;

public static class PartyQuickDetailDialog
{
    public static void ShowCustomer(IPartyQuickDetailService service, int customerId)
        => Show(service, isCustomer: true, customerId);

    public static void ShowSupplier(IPartyQuickDetailService service, int supplierId)
        => Show(service, isCustomer: false, supplierId);

    private static void Show(IPartyQuickDetailService service, bool isCustomer, int id)
    {
        var model = new PartyQuickDetailOverlayViewModel
        {
            TypeLabel = isCustomer ? "عميل" : "مورد",
            Name = "جاري التحميل…"
        };
        var overlay = new PartyQuickDetailOverlay { DataContext = model };

        async void Load()
        {
            try
            {
                var data = isCustomer
                    ? await service.GetCustomerDetailAsync(id)
                    : await service.GetSupplierDetailAsync(id);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (data is null)
                        model.SetError(isCustomer ? "لم يتم العثور على العميل" : "لم يتم العثور على المورد");
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
