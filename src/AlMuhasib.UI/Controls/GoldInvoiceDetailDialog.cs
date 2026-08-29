using AlMuhasib.Core.Enums.Gold;
using AlMuhasib.Core.Interfaces.Services.Gold;
using AlMuhasib.UI.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AlMuhasib.UI.Controls;

public static class GoldInvoiceDetailDialog
{
    public static async Task ShowAsync(
        int invoiceId,
        GoldInvoiceType invoiceType,
        IGoldSaleService saleService,
        IGoldPurchaseService purchaseService)
    {
        var invoice = invoiceType == GoldInvoiceType.Purchase
            ? await purchaseService.GetByIdAsync(invoiceId)
            : await saleService.GetByIdAsync(invoiceId);

        if (invoice is null)
        {
            BeautifulMessageDialog.ShowError("لم يتم العثور على الفاتورة.");
            return;
        }

        Show(invoice);
    }

    public static void Show(AlMuhasib.Core.Entities.Gold.GoldInvoice invoice)
    {
        var model = GoldInvoiceDetailMapper.FromInvoice(invoice);
        var overlay = new GoldInvoiceDetailOverlay { DataContext = model };
        overlay.ShowCentered();
    }

    public static async Task ShowFromListItemAsync(
        int invoiceId,
        GoldInvoiceType invoiceType,
        IServiceProvider services)
    {
        var saleService = services.GetService(typeof(IGoldSaleService)) as IGoldSaleService
            ?? throw new InvalidOperationException("IGoldSaleService not registered.");
        var purchaseService = services.GetService(typeof(IGoldPurchaseService)) as IGoldPurchaseService
            ?? throw new InvalidOperationException("IGoldPurchaseService not registered.");
        await ShowAsync(invoiceId, invoiceType, saleService, purchaseService);
    }
}
