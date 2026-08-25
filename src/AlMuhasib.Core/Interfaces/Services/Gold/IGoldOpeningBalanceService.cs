using AlMuhasib.Core.Entities.Gold;
using AlMuhasib.Core.Models.Gold;

namespace AlMuhasib.Core.Interfaces.Services.Gold;

public interface IGoldOpeningBalanceService
{
    /// <summary>Sets absolute grams on hand for a karat/warehouse (opening stock).</summary>
    Task<GoldStockBalance> SetOpeningStockAsync(
        GoldOpeningStockRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Sets customer money credit balances and gold credit grams (opening balances).</summary>
    Task<GoldCustomer> SetCustomerOpeningBalanceAsync(
        GoldOpeningCustomerBalanceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Clears customer opening credit balances to zero.</summary>
    Task ClearCustomerOpeningBalanceAsync(
        int customerId,
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>Sets supplier money credit balances (opening balances).</summary>
    Task<GoldSupplier> SetSupplierOpeningBalanceAsync(
        GoldOpeningSupplierBalanceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Clears supplier opening credit balances to zero.</summary>
    Task ClearSupplierOpeningBalanceAsync(
        int supplierId,
        string? notes = null,
        CancellationToken cancellationToken = default);
}
