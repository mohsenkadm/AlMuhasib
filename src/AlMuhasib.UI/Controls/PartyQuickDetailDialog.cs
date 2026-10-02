using System.Windows;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.UI.Controls;

public static class PartyQuickDetailDialog
{
    public static void ShowCustomer(IPartyQuickDetailService service, int customerId, bool showMultiCurrency = false)
        => Show(service, isCustomer: true, customerId, showMultiCurrency);

    public static void ShowSupplier(IPartyQuickDetailService service, int supplierId, bool showMultiCurrency = false)
        => Show(service, isCustomer: false, supplierId, showMultiCurrency);

    private static void Show(IPartyQuickDetailService service, bool isCustomer, int id, bool showMultiCurrency)
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
                        model.Apply(data, showMultiCurrency);
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
