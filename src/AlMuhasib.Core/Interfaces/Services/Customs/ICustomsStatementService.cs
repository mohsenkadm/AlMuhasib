using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Core.Interfaces.Services.Customs;

public interface ICustomsStatementService
{
    Task<CustomsStatementResult> GetStatementAsync(
        int customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CustomsStatementTransactionType transactionType = CustomsStatementTransactionType.All,
        int? invoiceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Open invoices with remaining balance for the official outstanding-balance claim print form.
    /// </summary>
    Task<CustomsOutstandingClaimDetail> GetOutstandingClaimAsync(
        int customerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? invoiceId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomsStatementInvoiceOption>> GetInvoiceOptionsAsync(
        int customerId,
        CancellationToken cancellationToken = default);
}
