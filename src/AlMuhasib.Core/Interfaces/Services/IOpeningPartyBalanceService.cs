using AlMuhasib.Core.Entities;
using AlMuhasib.Core.Models;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IOpeningPartyBalanceService
{
    Task<Invoice> CreateCustomerOpeningBalanceAsync(OpeningPartyBalanceRequest request);
    Task<OpeningPartyBalanceBatchResult> CreateCustomerOpeningBalancesBatchAsync(
        IReadOnlyList<OpeningPartyBalanceRequest> requests);

    Task<Invoice> CreateSupplierOpeningBalanceAsync(OpeningPartyBalanceRequest request);
    Task<OpeningPartyBalanceBatchResult> CreateSupplierOpeningBalancesBatchAsync(
        IReadOnlyList<OpeningPartyBalanceRequest> requests);
}

public interface IOpeningCustomerBalanceExcelService
{
    byte[] GenerateTemplate();
    IReadOnlyList<OpeningPartyBalanceImportRow> ParseImportFile(string filePath);
}

public interface IOpeningSupplierBalanceExcelService
{
    byte[] GenerateTemplate();
    IReadOnlyList<OpeningPartyBalanceImportRow> ParseImportFile(string filePath);
}
