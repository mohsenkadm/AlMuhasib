using AlMuhasib.Core.Entities.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldPrintService
{
    /// <summary>Builds and prints a gold invoice document.</summary>
    Task PrintInvoiceAsync(GoldInvoice invoice, CancellationToken cancellationToken = default);

    /// <summary>Prints a barcode/weight label for a gold item.</summary>
    Task PrintItemLabelAsync(GoldItem item, CancellationToken cancellationToken = default);
}
