using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Models.SalesRep;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ISalesRepService
{
    /// <summary>يحسب ويحفظ عمولة الفاتورة للمندوب بعد حفظ فاتورة بيع.</summary>
    Task<SalesRepCommissionEntry?> CalculateAndSaveCommissionAsync(int invoiceId, CancellationToken ct = default);

    Task<SalesRepStatement> GetStatementAsync(int salesRepresentativeId, DateTime? from, DateTime? to, CancellationToken ct = default);

    Task<IReadOnlyList<SalesRepPerformanceRow>> GetPerformanceComparisonAsync(DateTime? from, DateTime? to, CancellationToken ct = default);

    Task<IReadOnlyList<SalesRepTargetProgress>> GetTargetProgressAsync(int? salesRepresentativeId, DateTime? asOf, CancellationToken ct = default);

    Task<IReadOnlyList<SalesRepCustomerRow>> GetCustomersByRepAsync(int salesRepresentativeId, DateTime? from, DateTime? to, CancellationToken ct = default);

    Task MarkCommissionPaidAsync(int commissionEntryId, decimal amount, CancellationToken ct = default);

    Task MarkCollectionHandedOverAsync(int collectionId, decimal amount, CancellationToken ct = default);
}
