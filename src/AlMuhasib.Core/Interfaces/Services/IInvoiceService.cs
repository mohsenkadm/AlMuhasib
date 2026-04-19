using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IInvoiceService
{
    Task<Invoice> CreateInvoiceAsync(Invoice invoice, IEnumerable<InvoiceItem> items);
    Task<Invoice?> GetByIdAsync(int id);
    Task<Invoice?> GetByIdWithDetailsAsync(int id);
    Task<(IEnumerable<Invoice> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, InvoiceType? type = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<string> GenerateInvoiceNumberAsync(InvoiceType type);

    /// <summary>
    /// Applies rounding: UP for Purchase, DOWN for Sale.
    /// Returns the rounding difference amount.
    /// </summary>
    decimal CalculateRounding(decimal netAmount, InvoiceType invoiceType);

    Task DeleteInvoiceAsync(int id);

    /// <summary>
    /// Pays a credit invoice (partial or full). Updates CashBox balance.
    /// </summary>
    Task PayCreditInvoiceAsync(int invoiceId, decimal amount, int cashBoxId);
}
