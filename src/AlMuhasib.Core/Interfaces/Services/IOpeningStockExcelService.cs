using AlMuhasib.Core.Models;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IOpeningStockExcelService
{
    byte[] GenerateTemplate(bool includeProductSalePrice);
    IReadOnlyList<OpeningStockImportRow> ParseImportFile(string filePath, bool includeProductSalePrice);
}
