using AlMuhasib.Core.Models;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IOpeningInstallmentExcelService
{
    byte[] GenerateTemplate();
    IReadOnlyList<OpeningInstallmentImportRow> ParseImportFile(string filePath);
}
