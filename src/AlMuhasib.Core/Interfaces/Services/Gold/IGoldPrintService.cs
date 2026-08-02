using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Interfaces.Services;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldPrintService
{
    /// <summary>Builds the print model used for A4/thermal/PDF/WhatsApp.</summary>
    InvoicePrintModel BuildInvoicePrintModel(GoldInvoice invoice);

    /// <summary>Builds and prints a gold invoice document.</summary>
    Task PrintInvoiceAsync(GoldInvoice invoice, CancellationToken cancellationToken = default);

    /// <summary>Prints a barcode/weight label for a gold item.</summary>
    Task PrintItemLabelAsync(GoldItem item, CancellationToken cancellationToken = default);
}
