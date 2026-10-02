using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IInvoiceService
{
    Task<Invoice> CreateInvoiceAsync(
        Invoice invoice,
        IEnumerable<InvoiceItem> items,
        bool skipStockUpdate = false,
        int loyaltyRedeemPoints = 0,
        bool applyLoyalty = false,
        bool preserveProvidedNumber = false);
    Task<Invoice?> GetByIdAsync(int id);
    Task<Invoice?> GetByIdWithDetailsAsync(int id);
    Task<(IEnumerable<Invoice> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, InvoiceType? type = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<string> GenerateInvoiceNumberAsync(InvoiceType type);

    /// <summary>
    /// Applies IQD cash rounding (step 250): UP for Purchase, DOWN for Sale.
    /// USD invoices skip IQD cash rounding (return 0).
    /// </summary>
    decimal CalculateRounding(decimal netAmount, InvoiceType invoiceType, AccountingCurrency currency = AccountingCurrency.IQD);

    Task DeleteInvoiceAsync(int id);

    /// <summary>
    /// Restores a soft-deleted invoice and re-applies stock/cash effects (opposite of delete).
    /// </summary>
    Task RestoreInvoiceAsync(int id);

    /// <summary>
    /// Search invoices by number or party name for quick lookup in invoice screens.
    /// </summary>
    Task<IReadOnlyList<Invoice>> SearchAsync(
        InvoiceType invoiceType,
        string? searchText,
        bool newestFirst,
        int limit = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces an existing invoice (soft-delete + recreate) while preserving the supplied invoice number.
    /// </summary>
    Task<Invoice> ReplaceInvoiceAsync(
        int existingId,
        Invoice invoice,
        IEnumerable<InvoiceItem> items,
        bool skipStockUpdate = false);

    /// <summary>
    /// Pays a credit invoice (partial or full). Updates CashBox balance.
    /// </summary>
    Task PayCreditInvoiceAsync(int invoiceId, decimal amount, int cashBoxId);

    /// <summary>
    /// متوقف: لا يُعاد تطبيق المرتجعات تلقائياً (التطبيق عند الحفظ فقط). يُرجع 0 دائماً.
    /// </summary>
    Task<int> RepairUnappliedReturnCreditsAsync(CancellationToken cancellationToken = default);
}
